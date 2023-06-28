using McMaster.Extensions.CommandLineUtils;
using System.IO.Abstractions;
using Newtonsoft.Json.Linq;
namespace NeoShell.Commands
{
    [Command("extension", Description = "Commands to manage Neo shell extensions")]
    [Subcommand(typeof(List), typeof(Register), typeof(Unregister))]
    partial class ExtensionCommand
    {
        private const string BaseFolder = ".neo";
        private const string ExtensionsFile = "extensions.json";

        internal int OnExecute(CommandLineApplication app, IConsole console)
        {
            console.WriteLine("You must specify at a subcommand.");
            app.ShowHelp(false);
            return 1;
        }

        private static JArray LoadExtensions(IFileSystem fileSystem)
        {
            string extensionsFilePath = GetExtensionFilePath(fileSystem);
            string extensionsJsonContent = fileSystem.File.ReadAllText(extensionsFilePath);
            JArray extensionsArray = JArray.Parse(extensionsJsonContent);
            return extensionsArray;
        }

        private static void WriteExtensions(IFileSystem fileSystem, JArray extensionsArray)
        {
            string rootPath = GetRootPath(fileSystem);
            string updatedExtensionsJson = extensionsArray.ToString();
            string extensionsFilePath = fileSystem.Path.Combine(rootPath, ExtensionsFile);
            fileSystem.File.WriteAllText(extensionsFilePath, updatedExtensionsJson);
        }

        private static string GetRootPath(IFileSystem fileSystem)
        {
            return fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaseFolder);
        }

        private static string GetExtensionFilePath(IFileSystem fileSystem)
        {
            return fileSystem.Path.Combine(GetRootPath(fileSystem), ExtensionsFile);
        }
    }
}
