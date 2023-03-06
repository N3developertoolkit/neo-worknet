using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Models;

namespace NeoWorkNet.Commands
{
    partial class WalletCommand
    {
        [Command("delete", Description = "Delete neo-worknet wallet")]
        internal class Delete
        {
            readonly IFileSystem fs;

            public Delete(IFileSystem fileSystem)
            {
                this.fs = fileSystem;
            }

            [Argument(0, Description = "Wallet name")]
            [Required]
            internal string Name { get; init; } = string.Empty;

            [Option(Description = "Overwrite existing data")]
            internal bool Force { get; }


            internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                try
                {
                    var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);

                    var wallets = chain.Wallets.Where(w => !w.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
                    if (wallets.Count() == chain.Wallets.Count)
                    {
                        throw new Exception($"{Name} worknet wallet not found.");
                    }
                    else
                    {
                        if (!Force) throw new Exception("You must specify force to delete a worknet wallet.");
                    }

                    var newChain = new WorknetChain(chain, wallets);
                    fs.SaveWorknet(filename, newChain);

                    console.WriteLine($"{Name} worknet wallet deleted.");
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
