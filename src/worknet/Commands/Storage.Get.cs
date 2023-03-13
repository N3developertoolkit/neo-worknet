using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;
using NeoWorkNet.Node;
using Newtonsoft.Json;

namespace NeoWorkNet.Commands
{
    internal partial class StorageCommand
    {
        [Command("get", Description = "Update a value in neo-worknet")]
        internal class Get
        {
            readonly IFileSystem fs;
            public Get(IFileSystem fileSystem)
            {
                this.fs = fileSystem;
            }

            [Argument(0, Description = "Contract name or hash")]
            [Required]
            internal string Contract { get; init; } = string.Empty;

            [Argument(1, Description = "Key")]
            [Required]
            internal string Key { get; init; } = string.Empty;

            [Option(Description = "Output as JSON")]
            internal bool Json { get; }
            internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                try
                {
                    var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
                    var node = new WorkNetNode(chain, filename);
                    ContractInfo? contractInfo = StorageCommand.FindContractInfo(chain, Contract);
                    var writer = console.Out;
                    byte[]? value = node.GetStorage(contractInfo, StorageCommand.GetKeyInBytes(Key));
                    await WriteOutputAsync(writer, contractInfo, StorageCommand.GetKeyInBytes(Key), value).ConfigureAwait(false);
                    return 0;
                }
                catch (Exception ex)
                {
                    app.WriteException(ex);
                    return 1;
                }
            }

            private async Task WriteOutputAsync(TextWriter writer, ContractInfo contractInfo, byte[] key, byte[]? value)
            {
                if (Json)
                {
                    using var jsonWriter = new JsonTextWriter(writer);
                    await jsonWriter.WriteStartObjectAsync().ConfigureAwait(false);

                    await jsonWriter.WritePropertyNameAsync("script-hash").ConfigureAwait(false);
                    await jsonWriter.WriteValueAsync(contractInfo.Hash.ToString()).ConfigureAwait(false);

                    await jsonWriter.WritePropertyNameAsync("storages").ConfigureAwait(false);
                    await jsonWriter.WriteStartArrayAsync().ConfigureAwait(false);

                    if (value != null)
                    {
                        await jsonWriter.WriteStartObjectAsync().ConfigureAwait(false);
                        await jsonWriter.WritePropertyNameAsync("key").ConfigureAwait(false);
                        await jsonWriter.WriteValueAsync($"0x{Convert.ToHexString(key)}").ConfigureAwait(false);
                        await jsonWriter.WritePropertyNameAsync("value").ConfigureAwait(false);
                        await jsonWriter.WriteValueAsync($"0x{Convert.ToHexString(value)}").ConfigureAwait(false);
                        await jsonWriter.WriteEndObjectAsync().ConfigureAwait(false);
                    }
                    await jsonWriter.WriteEndArrayAsync().ConfigureAwait(false);
                    await jsonWriter.WriteEndObjectAsync().ConfigureAwait(false);
                }
                else
                {
                    if (value == null)
                    {
                        await writer.WriteLineAsync($"Storage value not found for key: {Key}").ConfigureAwait(false);
                        return;
                    }
                    var stringValue = $"0x{Convert.ToHexString(value)}";
                    await writer.WriteLineAsync($"  key:     {Convert.ToHexString(key)}").ConfigureAwait(false);
                    await writer.WriteLineAsync($"    value: {stringValue}").ConfigureAwait(false);

                }
            }
        }
    }
}
