using System.Diagnostics;
using Neo;
using Neo.BlockchainToolkit.Persistence;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace WorkNetExt;

public class WorkNetLogger : Plugin
{
    NeoSystem? neoSystem;

    public WorkNetLogger()
    {
        Blockchain.Committing += OnCommitting;
        ApplicationEngine.Log += OnAppEngineLog!;
        Neo.Utility.Logging += OnNeoUtilityLog;

    }

    public override void Dispose()
    {
        Neo.Utility.Logging -= OnNeoUtilityLog;
        ApplicationEngine.Log -= OnAppEngineLog!;
        Blockchain.Committing -= OnCommitting;
        GC.SuppressFinalize(this);
    }

    protected override void OnSystemLoaded(NeoSystem system)
    {
        if (neoSystem is not null) throw new Exception($"{nameof(OnSystemLoaded)} already called");
        neoSystem = system;
        base.OnSystemLoaded(system);
    }

    void OnAppEngineLog(object sender, LogEventArgs args)
    {
        var container = args.ScriptContainer is null
            ? string.Empty
            : $" [{args.ScriptContainer.GetType().Name}]";


        Console.WriteLine($"{GetContractName(args.ScriptHash)} Log: \"{args.Message}\" {container}");
    }

    void OnNeoUtilityLog(string source, LogLevel level, object message)
    {
        Console.WriteLine($"{DateTimeOffset.Now:HH:mm:ss.ff} {source} {level} {message}");
    }

    void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        Console.WriteLine($"Blockchain Committing: {block.Hash} {block.Index} {block.Timestamp} {block.Transactions.Length} txs");
    }

    protected string GetContractName(UInt160 scriptHash)
    {
        if (neoSystem is not null)
        {
            var contract = NativeContract.ContractManagement.GetContract(neoSystem.StoreView, scriptHash);
            if (contract is not null)
            {
                return contract.Manifest.Name;
            }
        }

        return scriptHash.ToString();
    }
}
