using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using NeoShell.Models;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("register", Description = "Registers an extension")]
        internal class Register
        {
            private readonly IFileSystem fileSystem;

            public Register(IFileSystem fileSystem)
            {
                this.fileSystem = fileSystem;
            }

            [Argument(0, Description = "Display of the extension")]
            internal string Name { get; init; } = string.Empty;

            [Argument(1, Description = "Top level command name. For example: if command name is nft. The shell command will be \"neosh nft\"")]
            internal string Command { get; init; } = string.Empty;

            [Argument(2, Description = "Path to the extension. Can be a full path to the executable or name of an executable in the PATH.")]
            internal string Path { get; init; } = string.Empty;


            internal int OnExecute(CommandLineApplication app, IConsole console)
            {
                try
                {
                    if (Name == string.Empty || Command == string.Empty || Path == string.Empty)
                    {
                        console.WriteLine("Name, Command and Path are required.");
                        return 1;
                    }

                    CreateBaseFilesIfNotExist();
                    ShellExtensions extensions = ShellExtensions.Load(this.fileSystem);
                    ShellExtension newExtension = new ShellExtension()
                    {
                        Name = Name,
                        Command = Command,
                        MapsToCommand = Path
                    };
                    extensions.Add(newExtension);
                    extensions.Persist(this.fileSystem);

                    console.WriteLine("Extension installed successfully.");
                    return 0;
                }
                catch (Exception ex)
                {
                    app.WriteException(ex, showInnerExceptions: true);
                    return 1;
                }
            }

            private void CreateBaseFilesIfNotExist()
            {
                string rootPath = ShellExtensions.GetRootPath(this.fileSystem);
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
                string extensionsFilePath = ShellExtensions.GetExtensionFilePath(this.fileSystem);
                if (!this.fileSystem.File.Exists(extensionsFilePath))
                {
                    string extensionsJson = "[]";
                    this.fileSystem.File.WriteAllText(extensionsFilePath, extensionsJson);
                }
            }
        }
    }
}
