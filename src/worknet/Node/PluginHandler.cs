using System.Reflection;
using Neo.Plugins;

static class PluginHandler
{
    public static void LoadPlugins(string directory, TextWriter? writer = null)
    {
        if (!Directory.Exists(directory)) return;
        List<Assembly> assemblies = new();

        foreach (var filename in Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                assemblies.Add(Assembly.Load(File.ReadAllBytes(filename)));
                writer?.WriteLine($"Loaded plugin: {filename}");
            }
            catch { }
        }

        foreach (Assembly assembly in assemblies)
        {
            LoadPlugin(assembly, writer);
        }
    }

    public static void LoadPlugin(Assembly assembly, TextWriter? writer = null)
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
                writer?.WriteLine($"Failed to load plugin: {type.FullName}");
                writer?.WriteLine(ex.Message);
            }
        }
    }
}