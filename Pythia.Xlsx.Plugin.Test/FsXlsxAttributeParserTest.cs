using Corpus.Core;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Xunit;

namespace Pythia.Xlsx.Plugin.Test;

public sealed class FsXlsxAttributeParserTest
{
    [Fact]
    public void Parse_Ok()
    {
        string filePath = Path.GetFullPath(Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            @"..\..\..\Assets\Sample.xlsx"));

        Document document = new()
        {
            Source = filePath
        };

        FsExcelAttributeParser parser = new();
        parser.Configure(new FsXlsxAttributeParserOptions
        {
            NameColumnIndex = 0,
            ValueColumnIndex = 1,
            NameMappings = new[]
            {
                "materia=subject",
                "sede di raccolta=coll-place",
                "organo giurisdizionale=",  // omit this attribute
                "sede organo giurisdizionale=court-place",
                "atto=act-type",
                "data=ymd",
                "grado#=degree"
            },
            ValueTrimming = true,
            SheetIndex = 0
        });

        IList<Attribute> attrs =
            parser.Parse(new StringReader("A fake document"), document);
        Assert.Equal(6, attrs.Count);

        var attr = attrs.FirstOrDefault(a => a.Name == "subject");
        Assert.NotNull(attr);
        Assert.Equal("civile", attr.Value);
        Assert.Equal(AttributeType.Text, attr.Type);

        attr = attrs.FirstOrDefault(a => a.Name == "coll-place");
        Assert.NotNull(attr);
        Assert.Equal("Lecce", attr.Value);
        Assert.Equal(AttributeType.Text, attr.Type);

        attr = attrs.FirstOrDefault(a => a.Name == "court-place");
        Assert.NotNull(attr);
        Assert.Equal("Taranto", attr.Value);
        Assert.Equal(AttributeType.Text, attr.Type);

        attr = attrs.FirstOrDefault(a => a.Name == "act-type");
        Assert.NotNull(attr);
        Assert.Equal("citazione", attr.Value);
        Assert.Equal(AttributeType.Text, attr.Type);

        attr = attrs.FirstOrDefault(a => a.Name == "ymd");
        Assert.NotNull(attr);
        Assert.Equal("20150418", attr.Value);
        Assert.Equal(AttributeType.Text, attr.Type);

        attr = attrs.FirstOrDefault(a => a.Name == "degree");
        Assert.NotNull(attr);
        Assert.Equal("2", attr.Value);
        Assert.Equal(AttributeType.Number, attr.Type);
    }
}
