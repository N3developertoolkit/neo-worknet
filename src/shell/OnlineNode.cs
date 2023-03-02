using System.Numerics;
using Neo;
using Neo.BlockchainToolkit.Models;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC;
using Neo.Network.RPC.Models;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.Wallets;
using NeoShell.Commands;
using NeoShell.Models;

namespace NeoShell.Node
{
  class OnlineNode : INode
  {
    readonly ExpressChain chain;
    readonly RpcClient rpcClient;
    // readonly Lazy<KeyPair[]> consensusNodesKeys;

    public ProtocolSettings ProtocolSettings { get; }

    public OnlineNode(ProtocolSettings settings, ExpressChain chain, ExpressConsensusNode node)
    {
      this.ProtocolSettings = settings;
      this.chain = chain;
      rpcClient = new RpcClient(new Uri($"http://localhost:{node.RpcPort}"), protocolSettings: settings);
      // consensusNodesKeys = new Lazy<KeyPair[]>(() => chain.GetConsensusNodeKeys());
    }

    public void Dispose()
    {
    }

    public Task<RpcInvokeResult> InvokeAsync(Script script, Signer? signer = null)
    {
      return signer is null
          ? rpcClient.InvokeScriptAsync(script)
          : rpcClient.InvokeScriptAsync(script, signer);
    }


    public async Task<UInt256> ExecuteAsync(Wallet wallet, UInt160 accountHash, WitnessScope witnessScope, Script script, decimal additionalGas = 0)
    {
      var signers = new[] { new Signer { Account = accountHash, Scopes = witnessScope } };
      var tm = await TransactionManager.MakeTransactionAsync(rpcClient, script, signers).ConfigureAwait(false);

      if (additionalGas > 0.0m)
      {
        tm.Tx.SystemFee += (long)additionalGas.ToBigInteger(NativeContract.GAS.Decimals);
      }

      var account = wallet.GetAccount(accountHash) ?? throw new Exception();
      if (account.IsMultiSigContract())
      {
        var signatureCount = account.Contract.ParameterList.Length;
        var multiSigWallets = chain.GetMultiSigWallets(ProtocolSettings, accountHash);
        if (multiSigWallets.Count < signatureCount) throw new InvalidOperationException();

        var publicKeys = multiSigWallets
            .Select(w => (w.GetAccount(accountHash)?.GetKey() ?? throw new Exception()).PublicKey)
            .ToArray();

        for (var i = 0; i < signatureCount; i++)
        {
          var key = multiSigWallets[i].GetAccount(accountHash)?.GetKey() ?? throw new Exception();
          tm.AddMultiSig(key, signatureCount, publicKeys);
        }
      }
      else
      {
        tm.AddSignature(account.GetKey() ?? throw new Exception());
      }

      var tx = await tm.SignAsync().ConfigureAwait(false);

      return await rpcClient.SendRawTransactionAsync(tx).ConfigureAwait(false);
    }


    public async Task<IReadOnlyList<(UInt160 hash, ContractManifest manifest)>> ListContractsAsync()
    {
      var json = await rpcClient.RpcSendAsync("expresslistcontracts").ConfigureAwait(false);

      if (json is not null && json is JArray array)
      {
        return array
            .Select(j => (
                UInt160.Parse(j!["hash"]!.AsString()),
                ContractManifest.FromJson((JObject)j["manifest"]!)))
            .ToList();
      }

      return Array.Empty<(UInt160 hash, ContractManifest manifest)>();
    }

    public async Task<IReadOnlyList<TokenContract>> ListTokenContractsAsync()
    {
      var json = await rpcClient.RpcSendAsync("expresslisttokencontracts").ConfigureAwait(false);

      if (json is not null && json is JArray array)
      {
        return array.Select(j => TokenContract.FromJson(j!)).ToList();
      }

      return Array.Empty<TokenContract>();
    }

    public async Task<Block> GetLatestBlockAsync()
    {
      var hash = await rpcClient.GetBestBlockHashAsync().ConfigureAwait(false);
      var rpcBlock = await rpcClient.GetBlockAsync(hash).ConfigureAwait(false);
      return rpcBlock.Block;
    }

    public async Task<Block> GetBlockAsync(UInt256 blockHash)
    {
      var rpcBlock = await rpcClient.GetBlockAsync(blockHash.ToString()).ConfigureAwait(false);
      return rpcBlock.Block;
    }

    public async Task<Block> GetBlockAsync(uint blockIndex)
    {
      var rpcBlock = await rpcClient.GetBlockAsync(blockIndex.ToString()).ConfigureAwait(false);
      return rpcBlock.Block;
    }

    public async Task<(Transaction tx, Neo.Network.RPC.Models.RpcApplicationLog? appLog)> GetTransactionAsync(UInt256 txHash)
    {
      var hash = txHash.ToString();
      var response = await rpcClient.GetRawTransactionAsync(hash).ConfigureAwait(false);
      var log = await rpcClient.GetApplicationLogAsync(hash).ConfigureAwait(false);
      return (response.Transaction, log);
    }
  }
}
