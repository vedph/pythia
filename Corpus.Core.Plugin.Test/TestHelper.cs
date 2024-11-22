using System;
using System.IO;
using System.Reflection;
using System.Text;
using Corpus.Core.Plugin.Test.Reading;

namespace Corpus.Core.Plugin.Test;

internal static class TestHelper
{
    public static string LoadResourceText(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        using StreamReader reader = new(
            typeof(XmlTextMapperTest).GetTypeInfo().Assembly.GetManifestResourceStream(
                $"Corpus.Core.Plugin.Test.Assets.{name}")!, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
