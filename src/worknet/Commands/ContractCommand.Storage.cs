using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;

namespace NeoWorkNet.Commands
{

  partial class ContractCommand
  {
    [Command("Storage", Description = "Manage neo-worknet values")]
    [Subcommand(typeof(List), typeof(Get), typeof(Update))]
    partial class Storage
    {
      internal int OnExecute(CommandLineApplication app, IConsole console)
      {
        console.WriteLine("You must specify at a subcommand.");
        app.ShowHelp(false);
        return 1;
      }
    }
  }
}
