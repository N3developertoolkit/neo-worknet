using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
using Newtonsoft.Json;

namespace NeoNft.Commands
{
    [Command("owner", Description = "Transfer a NFT to another address")]
    partial class OwnerOfCommand
    {
        [Option(Description = "Path to neo data file")]
        internal string Input { get; init; } = string.Empty;
        [Argument(0, Description = "Contract hash of the NFT contract")]
        [Required]
        internal string Contract { get; init; } = string.Empty;

        [Argument(1, Description = "NFT ID to transfer")]
        [Required]
        internal string Id { get; init; } = string.Empty;

        [Option(Description = "Account")]
        internal string Account { get; init; } = string.Empty;

        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                var payload = new { Contract = this.Contract, Method = "ownerOf", Account = this.Account, Arguments = new[] { this.Id } };
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
