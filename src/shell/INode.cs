using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Network.RPC.Models;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.Wallets;
using NeoShell.Models;

namespace NeoShell
{
  interface INode : IDisposable
  {
    ProtocolSettings ProtocolSettings { get; }

    enum CheckpointMode { Online, Offline }

    Task<RpcInvokeResult> InvokeAsync(Script script, Signer? signer = null);

    Task<UInt256> ExecuteAsync(Wallet wallet, UInt160 accountHash, WitnessScope witnessScope, Script script, decimal additionalGas = 0);

    Task<IReadOnlyList<(UInt160 hash, ContractManifest manifest)>> ListContractsAsync();

    Task<IReadOnlyList<TokenContract>> ListTokenContractsAsync();

  }
}
