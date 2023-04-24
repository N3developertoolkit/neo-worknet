using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace NeoShell.Models
{
    class ShellExtensions : List<ShellExtension>
    {
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

        public static ShellExtensions FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ShellExtensions>(json) ?? new ShellExtensions();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}