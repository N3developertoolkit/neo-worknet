using System.IO.Abstractions;
using Neo;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;

namespace NeoShell
{
  internal class ChainManager
  {
    const string GLOBAL_PREFIX = "Global\\";
    const string CHECKPOINT_EXTENSION = ".neoxp-checkpoint";

    readonly IFileSystem fileSystem;
    readonly ExpressChain chain;
    public ProtocolSettings ProtocolSettings { get; }

    public ChainManager(IFileSystem fileSystem, ExpressChain chain, uint? secondsPerBlock = null)
    {
      this.fileSystem = fileSystem;
      this.chain = chain;

      uint secondsPerBlockResult = secondsPerBlock.HasValue
          ? secondsPerBlock.Value
          : chain.TryReadSetting<uint>("chain.SecondsPerBlock", uint.TryParse, out var secondsPerBlockSetting)
              ? secondsPerBlockSetting
              : 0;

      this.ProtocolSettings = chain.GetProtocolSettings(secondsPerBlockResult);
    }

    public ExpressChain Chain => chain;

    string ResolveCheckpointFileName(string path) => fileSystem.ResolveFileName(path, CHECKPOINT_EXTENSION, () => $"{DateTimeOffset.Now:yyyyMMdd-hhmmss}");

    static bool IsNodeRunning(ExpressConsensusNode node)
    {
      // Check to see if there's a neo-express blockchain currently running by
      // attempting to open a mutex with the multisig account address for a name

      var account = node.Wallet.Accounts.Single(a => a.IsDefault);
      return Mutex.TryOpenExisting(GLOBAL_PREFIX + account.ScriptHash, out var _);
    }

    public bool IsRunning(ExpressConsensusNode? node = null)
    {
      if (node is null)
      {
        for (var i = 0; i < chain.ConsensusNodes.Count; i++)
        {
          if (IsNodeRunning(chain.ConsensusNodes[i]))
          {
            return true;
          }
        }
        return false;
      }
      else
      {
        return IsNodeRunning(node);
      }
    }

    public void SaveChain(string path)
    {
      fileSystem.SaveChain(chain, path);
    }

    public void ResetNode(ExpressConsensusNode node, bool force)
    {
      if (IsNodeRunning(node))
      {
        var scriptHash = node.Wallet.DefaultAccount?.ScriptHash ?? "<unknown>";
        throw new InvalidOperationException($"node {scriptHash} currently running");
      }

      var nodePath = fileSystem.GetNodePath(node);
      if (fileSystem.Directory.Exists(nodePath))
      {
        if (!force)
        {
          throw new InvalidOperationException("--force must be specified when resetting a node");
        }

        fileSystem.Directory.Delete(nodePath, true);
      }
    }

    public async Task<bool> StopNodeAsync(ExpressConsensusNode node)
    {
      if (!IsNodeRunning(node)) return false;

      var rpcClient = new Neo.Network.RPC.RpcClient(new Uri($"http://localhost:{node.RpcPort}"), protocolSettings: ProtocolSettings);
      var json = await rpcClient.RpcSendAsync("expressshutdown").ConfigureAwait(false);
      var processId = int.Parse(json["process-id"]!.AsString());
      var process = System.Diagnostics.Process.GetProcessById(processId);
      await process.WaitForExitAsync().ConfigureAwait(false);
      return true;
    }

    public INode GetNode(bool offlineTrace = false)
    {
      var consensusNode = chain.ConsensusNodes?[0];
      if (consensusNode == null)
      {
        throw new Exception("No consensus node found");
      }
      return new Node.OnlineNode(ProtocolSettings, chain, consensusNode);
    }
  }
}
