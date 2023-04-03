using Pythia.Core;
using System.Linq;
using Xunit;

namespace Pythia.Chiron.Ita.Plugin.Test;

public class ItaSylCountSupplierTokenFilterTest
{
    [Fact]
    public void Apply_Ok()
    {
        ItaSylCountSupplierTokenFilter filter = new();
        Token token = new()
        {
            Length = 1,
            Position = 1,
            Value = "imprescindibile"
        };

        filter.Apply(token, 1);

        Assert.NotNull(token.Attributes!.FirstOrDefault(a => a.Name == "sylc"
            && a.Value == "6"));
    }
}