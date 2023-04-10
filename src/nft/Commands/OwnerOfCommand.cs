using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.VM;
using Neo.VM.Types;
using Newtonsoft.Json;

namespace NeoNft.Commands
{
    [Command("ownerof", Description = "Transfer a NFT to another address")]
    partial class OwnerOfCommand
    {
        [Argument(0, Description = "Contract hash of the NFT contract")]
        [Required]
        internal string Contract { get; init; } = string.Empty;

        [Argument(1, Description = "NFT ID")]
        [Required]
        internal string Id { get; init; } = string.Empty;

        [Option(Description = "Path to neo data file")]
        internal string Input { get; init; } = string.Empty;

        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                UInt160.TryParse(this.Contract, out var contractHash);
                var hexString = this.Id;
                if (this.Id.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    hexString = hexString.Substring(2);
                }

                var idBytes = hexString.HexToBytes();
                var script = contractHash.MakeScript("ownerOf", idBytes);
                var payload = new { Script = Convert.ToBase64String(script) };
                Console.WriteLine(JsonConvert.SerializeObject(payload));
                return 0;
            }
            catch (Exception ex)
            {
                app.Error.Write(ex.Message);
                return 1;
            }
        }
    }
}
