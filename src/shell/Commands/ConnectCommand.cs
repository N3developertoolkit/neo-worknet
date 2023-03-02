using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using NeoShell.Models;

namespace NeoShell.Commands
{
  [Command("connect", Description = "Connect to a network for example worknet or testnet")]
  [Subcommand(typeof(Current))]
  partial class ConnectCommand
  {
    private readonly IFileSystem fileSystem;

    [Option(Description = "Path to neo data file")]
    internal string Input { get; init; } = string.Empty;
    public ConnectCommand(IFileSystem fileSystem)
    {
      this.fileSystem = fileSystem;
    }

    internal int OnExecute(CommandLineApplication app, IConsole console)
    {
      try
      {
        string rootPath = this.fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".neo");
        if (!Directory.Exists(rootPath))
        {
          Directory.CreateDirectory(rootPath);
        }


        var connections = this.fileSystem.LoadConnections();
        var inputFile = Input;
        if (!fileSystem.Path.IsPathFullyQualified(Input))
        {
          inputFile = fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), Input);
        }
        connections.Upsert(inputFile);
        this.fileSystem.SaveConnections(connections);
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
