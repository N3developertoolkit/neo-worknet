using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;

namespace NeoWorkNet.Commands
{
  [Command("contract", Description = "Manage neo-worknet values")]
  [Subcommand(typeof(Storage))]
  partial class ContractCommand
  {
    internal int OnExecute(CommandLineApplication app, IConsole console)
    {
      console.WriteLine("You must specify at a subcommand.");
      app.ShowHelp(false);
      return 1;
    }

    internal static ContractInfo FindContractInfo(WorknetChain chain, string contract)
    {
      var contracts = chain.BranchInfo.Contracts;
      ContractInfo? contractInfo;
      if (!UInt160.TryParse(contract, out var contractHash))
      {
        var contractCount = contracts.Count(c => c.Name.Equals(contract, StringComparison.OrdinalIgnoreCase));
        if (contractCount == 0) throw new Exception($"Cannot find contract: {contract}");
        if (contractCount > 1) throw new Exception("Contract name is not unique. Please use contract hash instead.");
        contractInfo = contracts.SingleOrDefault(c => c.Name.Equals(contract, StringComparison.OrdinalIgnoreCase));
        contractHash = contractInfo?.Hash ?? UInt160.Zero;
      }

      if (contractHash == UInt160.Zero) throw new Exception("Invalid Contract argument");

      contractInfo = contracts.SingleOrDefault(c => c.Hash == contractHash);

      if (string.IsNullOrEmpty(contractInfo?.Name)) throw new Exception("Invalid Contract argument");
      return contractInfo;
    }
  }
}
