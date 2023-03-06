using System.Diagnostics;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Persistence;
using NeoWorkNet.Node;

namespace NeoWorkNet.Commands;

[Command("prefetch", Description = "Fetch data for specified contract")]
class PrefetchCommand
{
    readonly IFileSystem fs;

    public PrefetchCommand(IFileSystem fileSystem)
    {
        fs = fileSystem;
    }

    [Argument(0, Description = "Name or Hash of contract to prefetch contract storage")]
    string Contract { get; set; } = string.Empty;

    [Option("--disable-log", Description = "Disable verbose data logging")]
    bool DisableLog { get; set; }

    internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console, CancellationToken token)
    {
        try
        {
            token = console.OverrideCancelKeyPress(token, true);

            if (!DisableLog)
            {
                var stateServiceObserver = new KeyValuePairObserver(Utility.GetDiagnosticWriter(console));
                var diagnosticObserver = new DiagnosticObserver(StateServiceStore.LoggerCategory, stateServiceObserver);
                DiagnosticListener.AllListeners.Subscribe(diagnosticObserver);
            }

            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            await node.PrefetchAsync(Contract, console, token).ConfigureAwait(false);

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 1;
        }
        catch (Exception ex)
        {
            app.WriteException(ex);
            return 1;
        }
    }



}
