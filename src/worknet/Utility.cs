using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit;
using Neo.BlockchainToolkit.Models;
using Newtonsoft.Json;
using System.IO.Abstractions;
using static Neo.BlockchainToolkit.Constants;
using static Crayon.Output;

namespace NeoWorkNet;

static class Utility
{
    public static void WriteException(this CommandLineApplication app, Exception exception)
    {
        app.Error.WriteLine(Bright.Red($"{exception.GetType()}: {exception.Message}"));
    }

    public static async Task<(WorknetChain chain, string fileName)> LoadWorknetAsync(this IFileSystem fs, CommandLineApplication app)
    {
        var option = app.GetOptions().Single(o => o.LongName == "input");
        var fileName = fs.ResolveWorkNetFileName(option?.Value() ?? string.Empty);
        var chain = await fs.LoadWorknetAsync(fileName).ConfigureAwait(false);
        return (chain, fileName);
    }

    public static string ResolveWorkNetFileName(this IFileSystem fs, string path)
        => fs.ResolveFileName(path, WORKNET_EXTENSION, () => DEFAULT_WORKNET_FILENAME);

    public static string GetWorknetDataDirectory(this IFileSystem fs, string filename)
    {
        var dirname = fs.Path.GetDirectoryName(filename) ?? throw new Exception($"GetDirectoryName({filename}) returned null");
        return fs.Path.Combine(dirname, "data");
    }

    public static async Task<WorknetChain> LoadWorknetAsync(this IFileSystem fs, string filename)
    {
        using var stream = fs.File.OpenRead(filename);
        return await WorknetChain.ParseAsync(stream).ConfigureAwait(false);
    }

    public static void SaveWorknet(this IFileSystem fs, string filename, WorknetChain chain)
    {
        using var stream = fs.File.Open(filename, FileMode.Create, FileAccess.Write);
        using var textWriter = new StreamWriter(stream);
        using var writer = new JsonTextWriter(textWriter);
        writer.Formatting = Formatting.Indented;
        chain.WriteJson(writer);
    }

    public static CancellationToken OverrideCancelKeyPress(this IConsole console, CancellationToken token, bool continueRunning = false)
    {
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        console.CancelKeyPress += (o, args) =>
        {
            args.Cancel = continueRunning;
            linkedTokenSource.Cancel();
        };
        return linkedTokenSource.Token;
    }

    public static Action<string, object?> GetDiagnosticWriter(IConsole console)
        => (name, value) =>
        {
            var text = value switch
            {
                GetStorageStart v => $"GetStorage for {v.ContractName} ({v.ContractHash}) with key {Convert.ToHexString(v.Key.Span)}",
                GetStorageStop v => $"GetStorage complete in {v.Elapsed}",
                DownloadStatesStart v =>
                    $"DownloadStates starting for {v.ContractName} ({(v.Prefix.HasValue ? $"{v.ContractHash} prefix {v.Prefix.Value}" : $"{v.ContractHash}")})",
                DownloadStatesStop v => $"DownloadStates complete. {v.Count} records downloaded in {v.Elapsed}",
                DownloadStatesFound v => $"DownloadStates {v.Count} records found, {v.Total} records total",
                _ => $"{name}: {value}"
            };

            console.WriteLine(Bright.Blue(text));
        };
}
