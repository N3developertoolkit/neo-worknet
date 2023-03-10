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
    // [Command("storage", Description = "Manage neo-worknet values")]
    // [Subcommand(typeof(Update))]
    internal partial class Storage
    {
      [Command("list", Description = "List all storage values associated with a contract")]
      internal class List
      {
        readonly IFileSystem fs;
        public List(IFileSystem fileSystem)
        {
          this.fs = fileSystem;
        }

        [Argument(0, Description = "Contract name or hash")]
        [Required]
        internal string Contract { get; init; } = string.Empty;

        internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
          try
          {
            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            ContractInfo? contractInfo = ContractCommand.FindContractInfo(chain, Contract);
            var writer = console.Out;

            var storageValues = node.ListStorage(contractInfo);
            await writer.WriteLineAsync($"contract:  {contractInfo.Hash}").ConfigureAwait(false);
            for (int j = 0; j < storageValues.Count; j++)
            {
              await writer.WriteLineAsync($"  key:     0x{Convert.ToHexString(StripContractIdStorageKey(storageValues[j].key))}").ConfigureAwait(false);
              await writer.WriteLineAsync($"    value: 0x{Convert.ToHexString(storageValues[j].value)}").ConfigureAwait(false);
            }

            return 0;
          }
          catch (Exception ex)
          {
            app.WriteException(ex);
            return 1;
          }
        }

        private ReadOnlySpan<byte> StripContractIdStorageKey(byte[] key)
        {
          return key.AsSpan(sizeof(int));
        }
      }
    }
  }
}
