using McMaster.Extensions.CommandLineUtils;

namespace NeoWorkNet.Commands
{
    [Command("wallet", Description = "Manage neo-worknet wallets")]
    [Subcommand(typeof(Create))]
    [Subcommand(typeof(Delete))]
    [Subcommand(typeof(List))]
    partial class WalletCommand
    {
        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify at a subcommand.");
            app.ShowHelp(false);
            return 1;
        }
    }
}
