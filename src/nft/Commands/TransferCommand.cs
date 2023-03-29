using System.ComponentModel.DataAnnotations;
using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Neo;
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

        [Option(Description = "Account to pay contract invocation GAS fee")]
        internal string Account { get; init; } = string.Empty;

        [Argument(3, Description = "Data")]
        internal string Data { get; init; } = string.Empty;


        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            try
            {
                var payload = new { Contract = this.Contract, Method = "transfer", Arguments = new[] { this.To, this.Id, this.Account, this.Data } };
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
