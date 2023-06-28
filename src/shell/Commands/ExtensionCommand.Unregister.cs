using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("unregister", Description = "Unregisters an extension")]
        internal class Unregister
        {
            private readonly IFileSystem fileSystem;

            public Unregister(IFileSystem fileSystem)
            {
                this.fileSystem = fileSystem;
            }

            [Argument(0, Description = "Display of the extension")]
            internal string Name { get; init; } = string.Empty;


            internal int OnExecute(CommandLineApplication app, IConsole console)
            {
                try
                {

                    JArray extensionsArray = ExtensionCommand.LoadExtensions(this.fileSystem);

                    var extensionToRemove = extensionsArray.FirstOrDefault(extension => extension.Value<string>("name") == Name);

                    if (extensionToRemove == null)
                    {
                        console.WriteLine("Extension not found.");
                        return 1;
                    }

                    extensionsArray.Remove(extensionToRemove);
                    ExtensionCommand.WriteExtensions(this.fileSystem, extensionsArray);

                    console.WriteLine("Extension uninstalled successfully.");
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
