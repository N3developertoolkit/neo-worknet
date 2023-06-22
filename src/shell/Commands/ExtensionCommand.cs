using McMaster.Extensions.CommandLineUtils;

namespace NeoShell.Commands
{
  [Command("extension", Description = "Commands to manage NEO shell extensions")]
  [Subcommand(typeof(Install), typeof(List), typeof(Uninstall))]
  partial class ExtensionCommand
  {
    internal int OnExecute(CommandLineApplication app, IConsole console)
    {
      console.WriteLine("You must specify at a subcommand.");
      app.ShowHelp(false);
      return 1;
    }
  }
}
