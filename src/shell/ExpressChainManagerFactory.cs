using System.IO.Abstractions;
using Neo;
using Neo.BlockchainToolkit;
using Neo.SmartContract;
using Neo.Wallets;

namespace NeoShell
{
  internal class ExpressChainManagerFactory
  {
    readonly IFileSystem fileSystem;

    public ExpressChainManagerFactory(IFileSystem fileSystem)
    {
      this.fileSystem = fileSystem;
    }

    string ResolveChainFileName(string path) => fileSystem.ResolveFileName(path);

    public (ChainManager manager, string path) LoadChain(string path, uint? secondsPerBlock = null)
    {
      path = ResolveChainFileName(path);
      if (!fileSystem.File.Exists(path))
      {
        throw new Exception($"{path} file doesn't exist");
      }

      var chain = fileSystem.LoadChain(path);

      // validate neo-express file by ensuring stored node zero default account SignatureRedeemScript matches a generated script
      var account = chain.ConsensusNodes[0].Wallet.DefaultAccount ?? throw new InvalidOperationException("consensus node 0 missing default account");
      var keyPair = new KeyPair(account.PrivateKey.HexToBytes());
      var contract = Contract.CreateSignatureContract(keyPair.PublicKey);

      if (!Contract.CreateSignatureRedeemScript(keyPair.PublicKey).AsSpan().SequenceEqual(contract.Script))
      {
        throw new Exception("Invalid Signature Redeem Script. Was this neo-express file created before RC1?");
      }

      return (new ChainManager(fileSystem, chain, secondsPerBlock), path);
    }

    public string GetConnectionFilePath(string input)
    {
      if (!string.IsNullOrEmpty(input))
      {
        return input;
      }
      var connections = this.fileSystem.LoadConnections();
      var connection = connections.GetMostRecent();
      if (connection != null)
      {
        return connection.File;
      }
      return string.Empty;
    }
  }
}
