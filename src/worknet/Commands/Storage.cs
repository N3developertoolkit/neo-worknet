using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;

namespace NeoWorkNet.Commands
{

    [Command("Storage", Description = "Manage neo-worknet values")]
    [Subcommand(typeof(List), typeof(Get), typeof(Update))]
    partial class StorageCommand
    {
        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify at a subcommand.");
            app.ShowHelp(false);
            return 1;
        }

        private static ContractInfo FindContractInfo(WorknetChain chain, string contract)
        {
            var contracts = chain.BranchInfo.Contracts;
            ContractInfo? contractInfo;
            if (!UInt160.TryParse(contract, out var contractHash))
            {
                var contractCount = contracts.Count(c => c.Name.Equals(contract, StringComparison.OrdinalIgnoreCase));
                if (contractCount == 0) throw new Exception($"Cannot find contract: {contract}");
                if (contractCount > 1) throw new Exception("Contract name is not unique. Please use contract hash instead.");
                contractInfo = contracts.SingleOrDefault(c => c.Name.Equals(contract, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                contractInfo = contracts.SingleOrDefault(c => c.Hash == contractHash);
            }
            contractHash = contractInfo?.Hash ?? UInt160.Zero;
            if (contractHash == UInt160.Zero) throw new Exception("Invalid Contract argument");
            if (string.IsNullOrEmpty(contractInfo?.Name)) throw new Exception("Invalid Contract argument");
            return contractInfo;
        }

        private static byte[] GetKeyInBytes(string key)
        {
            byte[] keyBytes;
            if (key.StartsWith("0x") == true)
            {
                keyBytes = Convert.FromHexString(key.Substring(2));
            }
            else
            {
                keyBytes = Convert.FromHexString(key);
            }

            return keyBytes;
        }
    }
}
