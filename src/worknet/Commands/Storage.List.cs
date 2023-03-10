using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo.BlockchainToolkit.Models;
using NeoWorkNet.Node;
using Newtonsoft.Json;

namespace NeoWorkNet.Commands
{
    internal partial class StorageCommand
    {
        [Command("list", Description = "List all storage values associated with a contract")]
        internal class List
        {
            readonly IFileSystem fs;
            public List(IFileSystem fileSystem)
            {
                this.fs = fileSystem;
            }

            [Argument(0, Description = "Contract name or hash")]
            [Required]
            internal string Contract { get; init; } = string.Empty;

            [Option(Description = "Output as JSON")]
            internal bool Json { get; }

            internal async Task<int> OnExecuteAsync(CommandLineApplication app, IConsole console)
            {
                try
                {
                    var (chain, filename) = await fs.LoadWorknetAsync(app).ConfigureAwait(false);
                    var node = new WorkNetNode(chain, filename);
                    ContractInfo? contractInfo = StorageCommand.FindContractInfo(chain, Contract);
                    var storageValues = node.ListStorage(contractInfo);
                    await WriteOutputAsync(console.Out, contractInfo, storageValues).ConfigureAwait(false);
                    return 0;
                }
                catch (Exception ex)
                {
                    app.WriteException(ex);
                    return 1;
                }
            }

            private ReadOnlySpan<byte> StripContractIdStorageKey(byte[] key)
            {
                return key.AsSpan(sizeof(int));
            }

            private async Task WriteOutputAsync(TextWriter writer, ContractInfo contractInfo, IReadOnlyList<(byte[] key, byte[] value)> storageValues)
            {
                if (Json)
                {
                    using var jsonWriter = new JsonTextWriter(writer);
                    await jsonWriter.WriteStartObjectAsync().ConfigureAwait(false);

                    await jsonWriter.WritePropertyNameAsync("script-hash").ConfigureAwait(false);
                    await jsonWriter.WriteValueAsync(contractInfo.Hash.ToString()).ConfigureAwait(false);

                    await jsonWriter.WritePropertyNameAsync("storages").ConfigureAwait(false);
                    await jsonWriter.WriteStartArrayAsync().ConfigureAwait(false);

                    for (int i = 0; i < storageValues.Count; i++)
                    {
                        await jsonWriter.WriteStartObjectAsync().ConfigureAwait(false);
                        await jsonWriter.WritePropertyNameAsync("key").ConfigureAwait(false);
                        await jsonWriter.WriteValueAsync($"0x{Convert.ToHexString(StripContractIdStorageKey(storageValues[i].key))}").ConfigureAwait(false);
                        await jsonWriter.WritePropertyNameAsync("value").ConfigureAwait(false);
                        await jsonWriter.WriteValueAsync($"0x{Convert.ToHexString(storageValues[i].value)}").ConfigureAwait(false);
                        await jsonWriter.WriteEndObjectAsync().ConfigureAwait(false);
                    }
                    await jsonWriter.WriteEndArrayAsync().ConfigureAwait(false);
                    await jsonWriter.WriteEndObjectAsync().ConfigureAwait(false);
                }
                else
                {
                    await writer.WriteLineAsync($"contract:  {contractInfo.Hash}").ConfigureAwait(false);
                    for (int i = 0; i < storageValues.Count; i++)
                    {
                        await writer.WriteLineAsync($"  key:     0x{Convert.ToHexString(StripContractIdStorageKey(storageValues[i].key))}").ConfigureAwait(false);
                        await writer.WriteLineAsync($"    value: 0x{Convert.ToHexString(storageValues[i].value)}").ConfigureAwait(false);
                    }

                }
            }
        }
    }
}
