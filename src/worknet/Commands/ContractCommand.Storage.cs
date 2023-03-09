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
    [Command("storage", Description = "Manage neo-worknet values")]
    [Subcommand(typeof(Update))]
    internal partial class Storage
    {

      readonly IFileSystem fs;
      public Storage(IFileSystem fileSystem)
      {
        this.fs = fileSystem;
      }

      [Argument(0, Description = "Contract name or invocation hash")]
      [Required]
      internal string Contract { get; init; } = string.Empty;

      [Argument(1, Description = "Key")]
      internal string Key { get; init; } = string.Empty;
      internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
      {
        var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
        var node = new WorkNetNode(chain, filename);
        var contracts = chain.BranchInfo.Contracts;
        ContractInfo? contractInfo;
        if (!UInt160.TryParse(Contract, out var contractHash))
        {
          contractInfo = contracts.SingleOrDefault(c => c.Name.Equals(Contract, StringComparison.OrdinalIgnoreCase));
          contractHash = contractInfo?.Hash ?? UInt160.Zero;
        }

        if (contractHash == UInt160.Zero) throw new Exception("Invalid Contract argument");

        contractInfo = contracts.SingleOrDefault(c => c.Hash == contractHash);
        if (string.IsNullOrEmpty(contractInfo?.Name)) throw new Exception("Invalid Contract argument");
        var writer = console.Out;
        if (Key == string.Empty)
        {
          var storageValues = node.ListStorage(contractInfo);
          await writer.WriteLineAsync($"contract:  {contractHash}").ConfigureAwait(false);
          for (int j = 0; j < storageValues.Count; j++)
          {
            await writer.WriteLineAsync($"  key:     0x{Convert.ToHexString(StripContractIdStorageKey(storageValues[j].key))}").ConfigureAwait(false);
            await writer.WriteLineAsync($"    value: 0x{Convert.ToHexString(storageValues[j].value)}").ConfigureAwait(false);
          }
        }
        else
        {
          byte[]? value = node.GetStorage(contractInfo, Convert.FromHexString(Key.Substring(2)));
          var stringValue = value == null ? string.Empty : $"0x{Convert.ToHexString(value)}";
          await writer.WriteLineAsync($"  key:     {Key}").ConfigureAwait(false);
          await writer.WriteLineAsync($"    value: {stringValue}").ConfigureAwait(false);
        }
        return 0;
      }
      private ReadOnlySpan<byte> StripContractIdStorageKey(byte[] key)
      {
        return key.AsSpan(sizeof(int));
      }
    }

  }
}
