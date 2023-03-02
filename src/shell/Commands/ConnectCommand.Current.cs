using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using NeoShell.Models;

namespace NeoShell.Commands
{
  partial class ConnectCommand
  {
    [Command("current", Description = "Get the current connection")]
    internal class Current
    {
      readonly IFileSystem fileSystem;
      public Current(IFileSystem fileSystem)
      {
        this.fileSystem = fileSystem;
      }
      internal int OnExecute(CommandLineApplication app, IConsole console)
      {
        try
        {
          var connections = this.fileSystem.LoadConnections();
          var connection = connections.GetMostRecent();
          Console.WriteLine(connection?.File);
          return 0;
        }
        catch (Exception ex)
        {
          app.WriteException(ex, showInnerExceptions: true);
          return 1;
        }
      }
    }
  }
}
