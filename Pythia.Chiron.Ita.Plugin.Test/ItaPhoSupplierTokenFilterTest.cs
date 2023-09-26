using Pythia.Chiron.Plugin;
using Pythia.Core;
using System.Linq;
using Xunit;

namespace Pythia.Chiron.Ita.Plugin.Test;

public sealed class ItaPhoSupplierTokenFilterTest
{
    [Fact]
    public void Apply_WithDigits_Nope()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 2,
            Position = 1,
            Value = "A4"
        };

        filter.Apply(token, 1);

        Assert.Empty(token.Attributes!);
    }

    [Fact]
    public void Apply_Email_Nope()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 17,
            Position = 1,
            Value = "abc@somewhere.com"
        };

        filter.Apply(token, 1);

        Assert.Empty(token.Attributes!);
    }

    [Fact]
    public void Apply_WithoutLetters_Nope()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 10,
            Position = 1,
            Value = "30/05/1970"
        };

        filter.Apply(token, 1);

        Assert.Empty(token.Attributes!);
    }

    [Fact]
    public void Apply_Word_Ok()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 1,
            Position = 1,
            Value = "imprescindibile"
        };

        filter.Apply(token, 1);

        Assert.Equal(2, token.Attributes!.Count);
        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "sylc"
            && a.Value == "6"));
        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "ipa"
            && a.Value == "impreʃindibile"));
    }

    [Fact]
    public void Apply_Sylc_Ok()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 1,
            Position = 1,
            Value = "imprescindibile"
        };
        filter.Configure(new PhoSupplierTokenFilterOptions
        {
            Sylc = true,
            Ipa = false,
            Ipas = false,
        }); 

        filter.Apply(token, 1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "sylc"
            && a.Value == "6"));
    }

    [Fact]
    public void Apply_Ipa_Ok()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 1,
            Position = 1,
            Value = "imprescindibile"
        };
        filter.Configure(new PhoSupplierTokenFilterOptions
        {
            Sylc = false,
            Ipa = true,
            Ipas = false,
        });

        filter.Apply(token, 1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "ipa"
            && a.Value == "impreʃindibile"));
    }

    [Fact]
    public void Apply_Ipas_Ok()
    {
        ItaPhoSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 1,
            Position = 1,
            Value = "imprescindibile"
        };
        filter.Configure(new PhoSupplierTokenFilterOptions
        {
            Sylc = false,
            Ipa = false,
            Ipas = true,
        });

        filter.Apply(token, 1);

        Assert.Single(token.Attributes!);
        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "ipas"
            && a.Value == "im|pre|ʃin|di|bi|le"));
    }
}