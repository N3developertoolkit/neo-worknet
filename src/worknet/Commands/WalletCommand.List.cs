using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Newtonsoft.Json;
using static Crayon.Output;

namespace NeoWorkNet.Commands
{
    partial class WalletCommand
    {
        [Command("list", Description = "List neo-worknet wallets")]
        internal class List
        {
            readonly IFileSystem fs;

            public List(IFileSystem fileSystem)
            {
                this.fs = fileSystem;
            }

            [Option(Description = "Output as JSON")]
            internal bool Json { get; init; } = false;

            internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                try
                {
                    var (chain, _) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);

                    using var walletWriter = IWalletWriter.Create(console.Out, Json);

                    walletWriter.WriteWallet(chain.ConsensusWallet);

                    foreach (var wallet in chain.Wallets)
                    {
                        walletWriter.WriteWallet(wallet);
                    }

                    if (!Json)
                    {
                        console.WriteLine(Yellow("Note: The private keys for these wallets are *not* encrypted."));
                        console.WriteLine(Yellow("      Do not use these wallets on MainNet or in any other system where security is a concern."));
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
