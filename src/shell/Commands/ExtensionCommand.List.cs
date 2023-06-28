using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("list", Description = "List all registered extensions")]
        internal class List
        {
            private readonly IFileSystem fileSystem;

            public List(IFileSystem fileSystem)
            {
                this.fileSystem = fileSystem;
            }

            internal int OnExecute(CommandLineApplication app, IConsole console)
            {
                try
                {
                    JArray extensionsArray = ExtensionCommand.LoadExtensions(this.fileSystem);
                    console.WriteLine("Installed Extensions:");

                    foreach (JObject extension in extensionsArray)
                    {
                        string? name = extension.Value<string>("name");
                        if (name != null)
                        {
                            console.WriteLine(name);
                        }

                    }

                    return 0;
                }
                catch (Exception ex)
                {
                    app.WriteException(ex, showInnerExceptions: true);
                    return 1;
                }
            }


        }
    }
}
