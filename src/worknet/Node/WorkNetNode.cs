using System.Buffers.Binary;
using System.Net;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Neo;
using Neo.BlockchainToolkit.Models;
using Neo.BlockchainToolkit.Persistence;
using Neo.BlockchainToolkit.Plugins;
using Neo.Cryptography;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.Wallets;
using NeoArray = Neo.VM.Types.Array;
using NeoStruct = Neo.VM.Types.Struct;

namespace NeoWorkNet.Node;

class WorkNetNode
{
    readonly WorknetChain chain;
    readonly string dataDirectory;

    public WorkNetNode(WorknetChain chain, string filename)
    {
        this.chain = chain;
        dataDirectory = Path.GetDirectoryName(filename) ?? throw new Exception();
    }

    string NodePath => Path.Combine(dataDirectory, "data", ConsensusAccount.Address);
    WalletAccount ConsensusAccount => chain.ConsensusWallet.GetDefaultAccount() ?? throw new Exception();

    public static async Task<WorknetChain> CreateAsync(Uri uri, uint index)
    {
        using var rpcClient = new RpcClient(uri);
        if (index == 0)
        {
            var stateApi = new StateAPI(rpcClient);
            var (localRootIndex, validatedRootIndex) = await stateApi.GetStateHeightAsync().ConfigureAwait(false);
            index = validatedRootIndex ?? localRootIndex ?? throw new Exception("No valid root index available");
        }
        var branchInfo = await StateServiceStore.GetBranchInfoAsync(rpcClient, index).ConfigureAwait(false);

        var consensusWallet = new ToolkitWallet("node1", branchInfo.ProtocolSettings);
        var consensusAccount = consensusWallet.CreateAccount();
        consensusAccount.IsDefault = true;

        var tcpPort = GetPortNumber(7, 3);
        var webSocketPort = GetPortNumber(7, 4);
        var rpcPort = GetPortNumber(7, 2);
        var consensusNode = new ToolkitConsensusNode(consensusWallet, tcpPort, webSocketPort, rpcPort);

        return new WorknetChain(uri, branchInfo, consensusNode);

        static ushort GetPortNumber(int index, ushort portNumber) => (ushort)(50000 + ((index + 1) * 10) + portNumber);
    }

    public void InitializeStore()
    {
        if (!Directory.Exists(NodePath)) Directory.CreateDirectory(NodePath);

        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db);
        using var trackStore = new PersistentTrackingStore(db, stateStore);

        InitializeStore(trackStore, ConsensusAccount);
    }

    internal static void InitializeStore(IStore store, params WalletAccount[] consensusAccounts)
        => InitializeStore(store, (IEnumerable<WalletAccount>)consensusAccounts);

    internal static void InitializeStore(IStore store, IEnumerable<WalletAccount> consensusAccounts)
    {
        const byte Prefix_Block = 5;
        const byte Prefix_BlockHash = 9;
        const byte Prefix_Candidate = 33;
        const byte Prefix_Committee = 14;
        const byte Prefix_CurrentBlock = 12;

        var keys = consensusAccounts.Select(a => a.GetKey().PublicKey).ToArray();
        var signerCount = (keys.Length * 2 / 3) + 1;
        var consensusContract = Contract.CreateMultiSigContract(signerCount, keys);

        using var snapshot = new SnapshotCache(store.GetSnapshot());

        // replace the Neo Committee with worknet consensus nodes
        // Prefix_Committee stores array of structs containing PublicKey / vote count 
        var members = consensusAccounts.Select(a => new NeoStruct { a.GetKey().PublicKey.ToArray(), 0 });
        var committee = new NeoArray(members);
        var committeeKeyBuilder = new KeyBuilder(NativeContract.NEO.Id, Prefix_Committee);
        var committeeItem = snapshot.GetAndChange(committeeKeyBuilder);
        committeeItem.Value = BinarySerializer.Serialize(committee, 1024 * 1024);

        // remove existing candidates (Prefix_Candidate) to ensure that 
        // worknet node account doesn't get outvoted
        var candidateKeyBuilder = new KeyBuilder(NativeContract.NEO.Id, Prefix_Candidate);
        foreach (var (key, value) in snapshot.Find(candidateKeyBuilder.ToArray()))
        {
            snapshot.Delete(key);
        }

        // create an *UNSIGNED* block that will be appended to the chain 
        // with updated NextConsensus field.
        var prevHash = NativeContract.Ledger.CurrentHash(snapshot);
        var prevBlock = NativeContract.Ledger.GetHeader(snapshot, prevHash);

        var trimmedBlock = new TrimmedBlock
        {
            Header = new Header
            {
                Version = 0,
                PrevHash = prevBlock.Hash,
                MerkleRoot = MerkleTree.ComputeRoot(Array.Empty<UInt256>()),
                Timestamp = Math.Max(Neo.Helper.ToTimestampMS(DateTime.UtcNow), prevBlock.Timestamp + 1),
                Index = prevBlock.Index + 1,
                PrimaryIndex = 0,
                NextConsensus = consensusContract.ScriptHash,
                Witness = new Witness()
                {
                    InvocationScript = Array.Empty<byte>(),
                    VerificationScript = Array.Empty<byte>()
                }
            },
            Hashes = Array.Empty<UInt256>(),
        };

        // update Prefix_BlockHash (mapping index -> hash)
        var blockHashKey = new KeyBuilder(NativeContract.Ledger.Id, Prefix_BlockHash).AddBigEndian(trimmedBlock.Index);
        snapshot.Add(blockHashKey, new StorageItem(trimmedBlock.Hash.ToArray()));

        // update Prefix_Block (store block indexed by hash)
        var blockKey = new KeyBuilder(NativeContract.Ledger.Id, Prefix_Block).Add(trimmedBlock.Hash);
        snapshot.Add(blockKey, new StorageItem(trimmedBlock.ToArray()));

        // update Prefix_CurrentBlock (struct containing current block hash + index)
        var curBlockKey = new KeyBuilder(NativeContract.Ledger.Id, Prefix_CurrentBlock);
        var currentBlock = new Neo.VM.Types.Struct() { trimmedBlock.Hash.ToArray(), trimmedBlock.Index };
        var currentBlockItem = snapshot.GetAndChange(curBlockKey);
        currentBlockItem.Value = BinarySerializer.Serialize(currentBlock, 1024 * 1024);

        snapshot.Commit();
    }

    public void ResetStore()
    {
        if (!Directory.Exists(NodePath)) return;

        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db, true);
        using var trackStore = new PersistentTrackingStore(db, stateStore, true);

        trackStore.Reset();
        InitializeStore(trackStore, ConsensusAccount);
    }

    public async Task PrefetchAsync(string contract, IConsole console, CancellationToken token)
    {
        if (!Directory.Exists(NodePath)) InitializeStore();

        var contracts = chain.BranchInfo.Contracts;
        if (!UInt160.TryParse(contract, out var contractHash))
        {
            var info = contracts.SingleOrDefault(c => c.Name.Equals(contract, StringComparison.OrdinalIgnoreCase));
            contractHash = info?.Hash ?? UInt160.Zero;
        }

        if (contractHash == UInt160.Zero) throw new Exception("Invalid Contract argument");

        var contractName = contracts.SingleOrDefault(c => c.Hash == contractHash)?.Name;
        if (string.IsNullOrEmpty(contractName)) throw new Exception("Invalid Contract argument");

        console.WriteLine($"Prefetching {contractName} ({contractHash}) records");

        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db);
        var result = await stateStore.PrefetchAsync(contractHash, token).ConfigureAwait(false);
        if (result.TryPickT1(out var error, out _))
        {
            throw new Exception(error.Value);
        }
    }

    public async Task RunAsync(uint secondsPerBlock, IConsole console, CancellationToken token)
    {
        if (!Directory.Exists(NodePath)) InitializeStore();

        var key = ConsensusAccount.GetKey() ?? throw new Exception();
        var protocolSettings = chain.ProtocolSettings with
        {
            MillisecondsPerBlock = secondsPerBlock == 0 ? 15000 : secondsPerBlock * 1000,
            ValidatorsCount = 1,
            StandbyCommittee = new[] { key.PublicKey },
            SeedList = new string[] { $"{IPAddress.Loopback}:{chain.ConsensusNode.TcpPort}" }
        };

        var tcs = new TaskCompletionSource<bool>();
        _ = Task.Run(() =>
        {
            try
            {
                using var db = RocksDbUtility.OpenDb(NodePath);
                using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db, true);
                using var trackStore = new PersistentTrackingStore(db, stateStore, true);

                var storeProvider = new WorknetStorageProvider(trackStore);
                StoreFactory.RegisterProvider(storeProvider);

                using var persistencePlugin = new ToolkitPersistencePlugin(db);
                using var logPlugin = new WorkNetLogPlugin(console, Utility.GetDiagnosticWriter(console));
                using var dbftPlugin = new Neo.Consensus.DBFTPlugin(GetConsensusSettings(chain));
                using var rpcServerPlugin = new WorknetRpcServerPlugin(GetRpcServerSettings(chain), persistencePlugin, chain.Uri);
                using var neoSystem = new Neo.NeoSystem(protocolSettings, storeProvider.Name);
                PluginHandler.LoadPlugins(Path.Combine(AppContext.BaseDirectory, "plugins"), console.Out);
                PluginHandler.LoadPlugins(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neo", "plugins"), console.Out);
                neoSystem.StartNode(new Neo.Network.P2P.ChannelsConfig
                {
                    Tcp = new IPEndPoint(IPAddress.Loopback, chain.ConsensusNode.TcpPort),
                    WebSocket = new IPEndPoint(IPAddress.Loopback, chain.ConsensusNode.WebSocketPort),
                });
                dbftPlugin.Start(chain.ConsensusWallet);

                // DevTracker looks for a string that starts with "Neo express is running" to confirm that the instance has started
                // Do not remove or re-word this console output:
                console.Out.WriteLine($"Neo worknet is running");

                var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(token, rpcServerPlugin.CancellationToken);
                linkedToken.Token.WaitHandle.WaitOne();
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
            finally
            {
                tcs.TrySetResult(true);
            }
        }, token);
        await tcs.Task.ConfigureAwait(false);

        static Neo.Consensus.Settings GetConsensusSettings(WorknetChain worknet)
        {
            var settings = new Dictionary<string, string>()
            {
                { "PluginConfiguration:Network", $"{worknet.BranchInfo.Network}" },
                { "IgnoreRecoveryLogs", "true" },
                { "RecoveryLogs", "ConsensusState" }
            };

            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return new Neo.Consensus.Settings(config.GetSection("PluginConfiguration"));
        }

        static RpcServerSettings GetRpcServerSettings(WorknetChain worknet)
        {
            var settings = new Dictionary<string, string>()
                {
                    { "PluginConfiguration:Network", $"{worknet.BranchInfo.Network}" },
                    // TODO: bind address setting
                    { "PluginConfiguration:BindAddress", $"{IPAddress.Loopback}" },
                    { "PluginConfiguration:Port", $"{worknet.ConsensusNode.RpcPort}" },
                    { "PluginConfiguration:SessionEnabled", $"{true}"}
                };

            var config = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
            return RpcServerSettings.Load(config.GetSection("PluginConfiguration"));
        }
    }

    public void UpdateValue(ContractInfo contract, byte[] key, byte[] value)
    {
        if (!Directory.Exists(NodePath)) InitializeStore();
        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db, true);
        using var trackStore = new PersistentTrackingStore(db, stateStore, true);
        var searchKey = StorageKey.CreateSearchPrefix(contract.Id, key);
        trackStore.Put(searchKey, value);
    }

    public IReadOnlyList<(byte[] key, byte[] value)> ListStorage(ContractInfo contract)
    {
        if (!Directory.Exists(NodePath)) InitializeStore();
        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db, true);
        using var trackStore = new PersistentTrackingStore(db, stateStore, true);
        var seekPrefix = StorageKey.CreateSearchPrefix(contract.Id, default);
        IEnumerable<(byte[] Key, byte[] Value)> result = TryFind(trackStore, seekPrefix);
        return result.ToList();
    }

    public byte[]? GetStorage(ContractInfo contract, byte[] prefix)
    {
        if (!Directory.Exists(NodePath)) InitializeStore();
        using var db = RocksDbUtility.OpenDb(NodePath);
        using var stateStore = new StateServiceStore(chain.Uri, chain.BranchInfo, db, true);
        using var trackStore = new PersistentTrackingStore(db, stateStore, true);
        byte[]? value = trackStore.TryGet(StorageKey.CreateSearchPrefix(contract.Id, prefix));
        return value;
    }

    private IEnumerable<(byte[] Key, byte[] Value)> TryFind(IReadOnlyStore store, byte[] key_prefix)
    {
        IEnumerable<(byte[] Key, byte[] Value)> result = Enumerable.Empty<(byte[] Key, byte[] Value)>();
        try
        {
            result = store.Seek(key_prefix, SeekDirection.Forward);
        }
        catch (System.IndexOutOfRangeException)
        {
            yield break;
        }
        foreach (var (key, value) in result)
            if (key.ToArray().AsSpan().StartsWith(key_prefix))
                yield return (key, value);
            else
                yield break;
    }


}
