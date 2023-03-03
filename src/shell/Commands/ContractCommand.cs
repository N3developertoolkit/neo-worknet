using McMaster.Extensions.CommandLineUtils;

namespace NeoShell.Commands
{
    [Command("contract", Description = "Commands to manage smart contracts")]
    [Subcommand(typeof(Run))]
    [Subcommand(typeof(List))]
    [Subcommand(typeof(Deploy))]
    [Subcommand(typeof(Update))]
    partial class ContractCommand
    {
        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify at a subcommand.");
            app.ShowHelp(false);
            return 1;
        }
    }
}
