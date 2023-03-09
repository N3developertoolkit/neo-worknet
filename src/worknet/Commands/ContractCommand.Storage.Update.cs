using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;
using NeoWorkNet.Node;

namespace NeoWorkNet.Commands
{
  partial class ContractCommand
  {
    partial class Storage
    {
      [Command("update", Description = "Update a value in neo-worknet")]
      internal class Update
      {
        readonly IFileSystem fs;

        public Update(IFileSystem fileSystem)
        {
          this.fs = fileSystem;
        }

        [Argument(1, Description = "Contract name or hash")]
        [Required]
        internal string Contract { get; init; } = string.Empty;

        [Argument(2, Description = "Key")]
        [Required]
        internal string Key { get; init; } = string.Empty;

        [Argument(3, Description = "New value in Hex")]
        [Required]
        internal string Value { get; init; } = string.Empty;

        internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
          try
          {
            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            ContractInfo? contractInfo = ContractCommand.FindContractInfo(chain, Contract);
            node.UpdateValue(contractInfo, Key.Substring(2), Value.Substring(2));
            return 0;
          }
          catch (Exception ex)
          {
            app.WriteException(ex);
            return 1;
          }
        }

      }
    }
  }
}
