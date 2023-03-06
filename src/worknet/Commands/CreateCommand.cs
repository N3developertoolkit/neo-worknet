using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Persistence;
using Newtonsoft.Json;
using NeoWorkNet.Node;
using static Neo.BlockchainToolkit.Constants;
using static Neo.BlockchainToolkit.Utility;
using static Crayon.Output;

namespace NeoWorkNet.Commands;

[Command("create", Description = "Create a Neo-Worknet branch")]
class CreateCommand
{
    readonly IFileSystem fs;

    public CreateCommand(IFileSystem fileSystem)
    {
        this.fs = fileSystem;
    }

    [Argument(0, Description = "URL of Neo JSON-RPC Node. Specify MainNet, TestNet or JSON-RPC URL")]
    [Required]
    internal string RpcUri { get; } = string.Empty;

    [Argument(1, Description = "Name of " + WORKNET_EXTENSION + " file to create (Default: ./" + DEFAULT_WORKNET_FILENAME + ")")]
    internal string Output { get; set; } = string.Empty;

    [Option(Description = "Block height to branch at")]
    internal uint Index { get; } = 0;

    [Option(Description = "Overwrite existing data")]
    internal bool Force { get; set; }

    [Option("--disable-log", Description = "Disable verbose data logging")]
    internal bool DisableLog { get; set; }

    internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
    {
        try
        {
            if (!DisableLog)
            {
                var stateServiceObserver = new KeyValuePairObserver(Utility.GetDiagnosticWriter(console));
                var diagnosticObserver = new DiagnosticObserver(StateServiceStore.LoggerCategory, stateServiceObserver);
                DiagnosticListener.AllListeners.Subscribe(diagnosticObserver);
            }

            if (!TryParseRpcUri(RpcUri, out var uri))
            {
                throw new ArgumentException($"Invalid RpcUri value \"{RpcUri}\"");
            }

            var filename = fs.ResolveWorkNetFileName(Output);
            if (fs.File.Exists(filename) && !Force) throw new Exception($"{filename} already exists");

            console.WriteLine($"Retrieving branch information from {RpcUri}");
            var chain = await WorkNetNode.CreateAsync(uri, Index).ConfigureAwait(false);
            fs.SaveWorknet(filename, chain);

            console.WriteLine($"Created {filename}");
            console.WriteLine(Yellow("Note: The consensus node private keys for this chain are *not* encrypted."));
            console.WriteLine(Yellow("      Do not use this wallet on MainNet or in any other system where security is a concern."));
            return 0;

        }
        catch (Exception ex)
        {
            app.WriteException(ex);
            return 1;
        }
    }
}