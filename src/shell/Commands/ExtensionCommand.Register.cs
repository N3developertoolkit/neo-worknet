using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("register", Description = "Registers an extension")]
        internal class Register
        {
            private const string BaseFolder = ".neo";
            private const string ExtensionsFile = "extensions.json";
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
                    string rootPath = this.fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaseFolder);
                    CreateBaseFiles(rootPath);
                    string extensionsFilePath = this.fileSystem.Path.Combine(rootPath, ExtensionsFile);
                    string extensionsJsonContent = this.fileSystem.File.ReadAllText(extensionsFilePath);
                    JArray extensionsArray = JArray.Parse(extensionsJsonContent);

                    JObject newExtension = new JObject();
                    newExtension["name"] = Name;
                    newExtension["command"] = Command;
                    newExtension["mapsToCommand"] = Path;

                    extensionsArray.Add(newExtension);

                    string updatedExtensionsJson = extensionsArray.ToString();
                    this.fileSystem.File.WriteAllText(extensionsFilePath, updatedExtensionsJson);

                    console.WriteLine("Extension installed successfully.");
                    return 0;
                }
                catch (Exception ex)
                {
                    app.WriteException(ex, showInnerExceptions: true);
                    return 1;
                }
            }

            private void CreateBaseFiles(string rootPath)
            {
                if (!Directory.Exists(rootPath))
                {
                    Directory.CreateDirectory(rootPath);
                }
                string extensionsFilePath = this.fileSystem.Path.Combine(rootPath, ExtensionsFile);
                if (!this.fileSystem.File.Exists(extensionsFilePath))
                {
                    string extensionsJson = "[]";
                    this.fileSystem.File.WriteAllText(extensionsFilePath, extensionsJson);
                }
            }
        }
    }
}
