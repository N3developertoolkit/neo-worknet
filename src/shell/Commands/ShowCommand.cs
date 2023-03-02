using McMaster.Extensions.CommandLineUtils;

namespace NeoShell.Commands
{
  [Command("show", Description = "Show information")]
  [Subcommand(typeof(Balance), typeof(Block), typeof(Transaction))]
  partial class ShowCommand
  {
    internal int OnExecute(CommandLineApplication app, IConsole console)
    {
      console.WriteLine("You must specify at a subcommand.");
      app.ShowHelp(false);
      return 1;
    }
  }
}
