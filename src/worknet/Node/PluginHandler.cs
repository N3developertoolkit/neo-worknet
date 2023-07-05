using System.Reflection;
using Neo.Plugins;
using McMaster.Extensions.CommandLineUtils;

class PluginHandler
{
    public static void LoadPlugins(string directory, IConsole console)
    {
        if (!Directory.Exists(directory)) return;
        List<Assembly> assemblies = new();

        foreach (var filename in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                assemblies.Add(Assembly.Load(File.ReadAllBytes(filename)));
                console.Out.WriteLine($"Loaded plugin: {filename}");
            }
            catch { }
        }

        foreach (Assembly assembly in assemblies)
        {
            LoadPlugin(assembly);
        }
    }

    public static void LoadPlugin(Assembly assembly)
    {
        foreach (Type type in assembly.ExportedTypes)
        {
            if (!type.IsSubclassOf(typeof(Plugin))) continue;
            if (type.IsAbstract) continue;

            ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
            try
            {
                constructor?.Invoke(null);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to load plugin: {type.FullName}", ex);
            }
        }
    }
}