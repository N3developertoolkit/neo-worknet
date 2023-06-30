using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using NeoShell.Models;

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
                    ShellExtensions extensions = ShellExtensions.Load(this.fileSystem);
                    console.WriteLine("Installed Extensions:");

                    foreach (var extension in extensions)
                    {
                        if (extension.Name != null)
                        {
                            console.WriteLine(extension.Name);
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
