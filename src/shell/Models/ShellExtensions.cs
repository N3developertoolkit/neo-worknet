using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using System.IO.Abstractions;

namespace NeoShell.Models
{
    class ShellExtensions : List<ShellExtension>
    {
        private const string BaseFolder = ".neo";
        private const string ExtensionsFile = "neosh-extensions.json";

        public bool TryFindCommand(string[] args, [MaybeNullWhen(false)] out ShellExtension command)
        {
            foreach (string arg in args)
            {
                foreach (var extension in this)
                {
                    if (string.Equals(arg, extension.Command, StringComparison.OrdinalIgnoreCase))
                    {
                        command = extension;
                        return true;
                    }
                }

            }
            command = null;
            return false;
        }

        public bool TryFindCommandByName(string name, [MaybeNullWhen(false)] out ShellExtension command)
        {
            foreach (var extension in this)
            {
                if (string.Equals(name, extension.Name, StringComparison.OrdinalIgnoreCase))
                {
                    command = extension;
                    return true;
                }
            }

            command = null;
            return false;
        }



        public static ShellExtensions FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ShellExtensions>(json) ?? new ShellExtensions();
        }
        public static ShellExtensions Load(IFileSystem fileSystem)
        {
            string filePath = GetExtensionFilePath(fileSystem);
            if (!fileSystem.File.Exists(filePath))
            {
                return new ShellExtensions();
            }
            string json = File.ReadAllText(filePath);
            var extensions = ShellExtensions.FromJson(json);
            return extensions;
        }

        public static string GetExtensionFilePath(IFileSystem fileSystem)
        {
            return fileSystem.Path.Combine(GetRootPath(fileSystem), ExtensionsFile);
        }

        public static string GetRootPath(IFileSystem fileSystem)
        {
            return fileSystem.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), BaseFolder);
        }

        public void Persist(IFileSystem fileSystem)
        {
            string rootPath = GetRootPath(fileSystem);
            string updatedExtensionsJson = this.ToJson();
            string extensionsFilePath = GetExtensionFilePath(fileSystem);
            fileSystem.File.WriteAllText(extensionsFilePath, updatedExtensionsJson);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }


    }
}