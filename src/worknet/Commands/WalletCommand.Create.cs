using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Models;
using static Crayon.Output;

namespace NeoWorkNet.Commands
{
    partial class WalletCommand
    {
        [Command("create", Description = "Create neo-worknet wallet")]
        internal class Create
        {
            readonly IFileSystem fs;

            public Create(IFileSystem fileSystem)
            {
                this.fs = fileSystem;
            }

            [Argument(0, Description = "Wallet name")]
            [Required]
            internal string Name { get; init; } = string.Empty;

            [Option(Description = "Overwrite existing data")]
            internal bool Force { get; }

            [Option(Description = "Output as JSON")]
            internal bool Json { get; init; } = false;

            internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                try
                {
                    var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);

                    if (chain.ConsensusWallet.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new Exception($"{Name} is a reserved name. Choose a different wallet name.");
                    }

                    if (!Force && chain.Wallets.Any(w => w.Name.Equals(Name, StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new Exception($"{Name} wallet already exists. Use --force to overwrite.");
                    }

                    var wallet = new ToolkitWallet(Name, chain.BranchInfo.ProtocolSettings);
                    var account = wallet.CreateAccount();
                    account.IsDefault = true;

                    var wallets = chain.Wallets
                        .Where(w => !w.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                        .Append(wallet);
                    var newChain = new WorknetChain(chain, wallets);
                    fs.SaveWorknet(filename, newChain);

                    using var walletWriter = IWalletWriter.Create(console.Out, Json);  
                    walletWriter.WriteWallet(wallet);
                    if (!Json)
                    {
                        console.WriteLine(Yellow("Note: The private keys for this wallet are *not* encrypted."));
                        console.WriteLine(Yellow("      Do not use this wallet on MainNet or in any other system where security is a concern."));
                    }

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
