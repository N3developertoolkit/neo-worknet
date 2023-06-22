using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;

namespace NeoShell.Commands
{
    partial class ExtensionCommand
    {
        [Command("list", Description = "List all installed extensions")]
        internal class List
        {
            private const string BaseFolder = ".neo";
            private const string ExtensionsFile = "extensions.json";
            private readonly IFileSystem fileSystem;

            public List(IFileSystem fileSystem)
            {
                this.fileSystem = fileSystem;
            }

            internal int OnExecute(CommandLineApplication app, IConsole console)
            {
                try
                {
                    string rootPath = this.fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaseFolder);
                    string extensionsFilePath = this.fileSystem.Path.Combine(rootPath, ExtensionsFile);
                    string extensionsJsonContent = this.fileSystem.File.ReadAllText(extensionsFilePath);
                    JArray extensionsArray = JArray.Parse(extensionsJsonContent);
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
