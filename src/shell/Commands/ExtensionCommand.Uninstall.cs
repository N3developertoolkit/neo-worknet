using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("uninstall", Description = "Uninstalls an extension")]
        internal class Uninstall
        {
            private const string BaseFolder = ".neo";
            private const string ExtensionsFile = "extensions.json";
            private readonly IFileSystem fileSystem;

            public Uninstall(IFileSystem fileSystem)
            {
                this.fileSystem = fileSystem;
            }

            [Argument(0, Description = "Display of the extension")]
            internal string Name { get; init; } = string.Empty;


            internal int OnExecute(CommandLineApplication app, IConsole console)
            {
                try
                {
                    string rootPath = this.fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaseFolder);

                    string extensionsFilePath = this.fileSystem.Path.Combine(rootPath, ExtensionsFile);
                    string extensionsJsonContent = this.fileSystem.File.ReadAllText(extensionsFilePath);
                    JArray extensionsArray = JArray.Parse(extensionsJsonContent);

                    var extensionToRemove = extensionsArray.FirstOrDefault(extension => extension.Value<string>("name") == Name);

                    if (extensionToRemove == null)
                    {
                        console.WriteLine("Extension not found.");
                        return 1;
                    }

                    extensionsArray.Remove(extensionToRemove);

                    // Save the changes back to the file
                    this.fileSystem.File.WriteAllText(extensionsFilePath, extensionsArray.ToString());

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
