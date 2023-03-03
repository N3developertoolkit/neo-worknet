using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using NeoWorkNet.Node;

namespace NeoWorkNet.Commands;

[Command("run", Description = "Run Neo-WorkNet instance node")]
partial class RunCommand
{
    readonly IFileSystem fs;

    public RunCommand(IFileSystem fs)
    {
        this.fs = fs;
    }

    [Option(Description = "Time between blocks")]
    internal uint? SecondsPerBlock { get; }

    internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console, CancellationToken token)
    {
        try
        {
            var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
            var node = new WorkNetNode(chain, filename);
            await node.RunAsync(SecondsPerBlock ?? 0, console, token).ConfigureAwait(false);
            return 0;
        }
        catch (Exception ex)
        {
            app.WriteException(ex);
            return 1;
        }
    }
}
