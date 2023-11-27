using Fusi.Tools.Configuration;
using McMaster.NETCore.Plugins;
using Pythia.Cli.Core;
using System;
using System.IO;
using System.Linq;

// https://github.com/natemcmaster/DotNetCorePlugins

namespace Pythia.Cli.Services;

/// <summary>
/// Plugins-based provider for Pythia factory providers. This is used to
/// load a factory provider from an external plugin.
/// </summary>
public static class PluginPythiaFactoryProvider
{
    /// <summary>
    /// Gets the plugins directory.
    /// </summary>
    /// <returns>Directory.</returns>
    public static string GetPluginsDir() =>
        Path.Combine(AppContext.BaseDirectory, "plugins");

    /// <summary>
    /// Scans all the plugins in the plugins folder and returns the first
    /// plugin matching the requested tag.
    /// </summary>
    /// <param name="tag">The requested plugin tag.</param>
    /// <returns>The provider, or null if not found.</returns>
    public static ICliPythiaFactoryProvider? GetFromTag(string tag)
    {
        // create plugin loaders
        string pluginsDir = GetPluginsDir();
        foreach (string dir in Directory.GetDirectories(pluginsDir))
        {
            string dirName = Path.GetFileName(dir);
            string pluginDll = Path.Combine(dir, dirName + ".dll");

            ICliPythiaFactoryProvider? provider = Get(pluginDll, tag);
            if (provider != null) return provider;
        }

        return null;
    }

    /// <summary>
    /// Gets the provider plugin from the specified directory.
    /// </summary>
    /// <param name="path">The path to the plugin file.</param>
    /// <param name="tag">The optional provider tag. If null, the first
    /// matching plugin in the target assembly will be returned. This can
    /// be used when an assembly just contains a single plugin implementation.
    /// </param>
    /// <returns>Provider, or null if not found.</returns>
    /// <exception cref="ArgumentNullException">path</exception>
    public static ICliPythiaFactoryProvider? Get(string path, string? tag = null)
    {
        ArgumentNullException.ThrowIfNull(path);

        if (!File.Exists(path)) return null;

        PluginLoader loader = PluginLoader.CreateFromAssemblyFile(
                path,
                sharedTypes: new[] { typeof(ICliPythiaFactoryProvider) });

        foreach (Type type in loader.LoadDefaultAssembly()
            .GetExportedTypes()
            .Where(t => typeof(ICliPythiaFactoryProvider).IsAssignableFrom(t) && !t.IsAbstract))
        {
            if (tag == null)
                return (ICliPythiaFactoryProvider?)Activator.CreateInstance(type);

            TagAttribute? tagAttr = (TagAttribute?)Attribute.GetCustomAttribute(
                type, typeof(TagAttribute));
            if (tagAttr?.Tag == tag)
                return (ICliPythiaFactoryProvider?)Activator.CreateInstance(type);
        }

        return null;
    }
}
