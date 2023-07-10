using Microsoft.Extensions.Configuration;
using Neo;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace WorkNetExt;

public class WorkNetLogger : Plugin
{
    private string _logFile = "./logger.log";

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

    // Overwrite Config file method to find the config.json from the same directory as the plugin dll directory
    public override string ConfigFile => System.IO.Path.Combine(AppContext.BaseDirectory, "plugins", "config.json");

    protected override void Configure()
    {
        IConfigurationSection config = GetConfiguration();
        _logFile = config.GetValue<string>("LogFile", _logFile);

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

        WriteToFile($"{GetContractName(args.ScriptHash)} Log: \"{args.Message}\" {container}");
    }

    void OnNeoUtilityLog(string source, LogLevel level, object message)
    {
        WriteToFile($"{DateTimeOffset.Now:HH:mm:ss.ff} {source} {level} {message}");
    }

    void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        WriteToFile($"Blockchain Committing: {block.Hash} {block.Index} {block.Timestamp} {block.Transactions.Length} txs");
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

    private void WriteToFile(string logMessage)
    {
        using(StreamWriter writer = new StreamWriter(_logFile, true))
        {
            writer.WriteLine(logMessage);
        }
    }
}
