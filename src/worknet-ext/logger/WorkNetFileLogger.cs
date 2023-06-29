using Neo;
using Neo.BlockchainToolkit.Persistence;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Plugins;
using Neo.SmartContract;
using Neo.SmartContract.Native;

namespace WorkNetExt;

public class WorkNetFileLogger : Plugin
{
    public WorkNetFileLogger()
    {
        Blockchain.Committing += OnCommitting;
    }

    public override void Dispose()
    {
        Blockchain.Committing -= OnCommitting;
        GC.SuppressFinalize(this);
    }

    void OnCommitting(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList)
    {
        Console.WriteLine("Blockchain Committing");
    }
}
