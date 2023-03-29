using System.IO.Abstractions;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using NeoNft.Commands;

namespace NeoNft
{
    [Command("nft")]
    [Subcommand(typeof(TransferCommand))]
    class Program
    {
        public static Task<int> Main(string[] args)
        {
            var services = new ServiceCollection()
           .AddSingleton<IConsole>(PhysicalConsole.Singleton)
           .BuildServiceProvider();

            var app = new CommandLineApplication<Program>();
            app.Conventions
                      .UseDefaultConventions()
                      .UseConstructorInjection(services);
            return app.ExecuteAsync(args);
        }
    }
}
