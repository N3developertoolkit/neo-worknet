using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Neo.BlockchainToolkit.Models;
using Neo.VM;
using Newtonsoft.Json;

namespace NeoNft.Commands
{
    [Command("transfer", Description = "Transfer a NFT to another address")]
    partial class TransferCommand
    {
        [Option(Description = "Path to neo data file")]
        internal string Input { get; init; } = string.Empty;

        [Argument(0, Description = "Contract hash of the NFT contract")]
        [Required]
        internal string Contract { get; init; } = string.Empty;

        [Argument(1, Description = "Address to transfer to")]
        [Required]
        internal string To { get; init; } = string.Empty;

        [Argument(2, Description = "NFT ID to transfer")]
        [Required]
        internal string Id { get; init; } = string.Empty;

        [Argument(3, Description = "NFT contract owner account")]
        [Required]
        internal string Account { get; init; } = string.Empty;



        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                UInt160.TryParse(this.Contract, out var contractHash);
                UInt160.TryParse(this.To, out var toHash);
                var hexString = this.Id;
                if (this.Id.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    hexString = hexString.Substring(2);
                }

                var idBytes = hexString.HexToBytes();
                var script = contractHash.MakeScript("transfer", toHash, idBytes, string.Empty);
                var payload = new { Script = Convert.ToBase64String(script), Account = this.Account };
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
