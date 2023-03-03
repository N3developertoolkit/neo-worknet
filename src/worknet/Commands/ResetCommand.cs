using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using NeoWorkNet.Node;

namespace NeoWorkNet.Commands;

[Command("reset", Description = "Reset WorkNet back to initial branch point")]
class ResetCommand
{
    readonly IFileSystem fs;

    public ResetCommand(IFileSystem fs)
    {
        this.fs = fs;
    }

    [Option(Description = "Overwrite existing data")]
    internal bool Force { get; }

    internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console, CancellationToken token)
    {
        try
        {
            if (!Force) throw new InvalidOperationException("--force must be specified when resetting worknet");

            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            node.ResetStore();

            console.WriteLine("WorkNet node reset");
            return 0;
        }
        catch (Exception ex)
        {
            app.WriteException(ex);
            return 1;
        }
    }

}
