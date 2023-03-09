using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
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

        [Argument(0, Description = "Key")]
        [Required]
        internal string Key { get; init; } = string.Empty;

        [Argument(1, Description = "Key")]
        [Required]
        internal string Value { get; init; } = string.Empty;

        internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
        {
          try
          {
            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            node.UpdateValue(Key, Value);
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
