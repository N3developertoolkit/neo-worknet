using McMaster.Extensions.CommandLineUtils;

namespace NeoWorkNet.Commands
{
  [Command("contract", Description = "Manage neo-worknet values")]
  [Subcommand(typeof(Storage))]
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
