using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using NeoShell.Models;

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

                    ShellExtensions extensions = ShellExtensions.Load(this.fileSystem);

                    var found = extensions.TryFindCommandByName(Name, out ShellExtension? extension);

                    if (!found)
                    {
                        console.WriteLine("Extension not found.");
                        return 1;
                    }
                    if (extension != null)
                    {
                        extensions.Remove(extension);
                        extensions.Persist(this.fileSystem);
                        console.WriteLine("Extension uninstalled successfully.");
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
