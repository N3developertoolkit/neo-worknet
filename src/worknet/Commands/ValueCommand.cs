using McMaster.Extensions.CommandLineUtils;

namespace NeoWorkNet.Commands
{
  [Command("value", Description = "Manage neo-worknet values")]
  [Subcommand(typeof(Update))]
  partial class ValueCommand
  {
    internal int OnExecute(CommandLineApplication app, IConsole console)
    {
      console.WriteLine("You must specify at a subcommand.");
      app.ShowHelp(false);
      return 1;
    }
  }
}
