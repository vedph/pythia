using System.Collections.Generic;
using ExpectedObjects;
using Moq;
using Pythia.Tagger.Lookup;
using Xunit;

// ReSharper disable SuspiciousTypeConversion.Global

// https://liftcodeplay.com/2017/01/02/setting-up-moq-with-your-net-core-test-project/

namespace Pythia.Tagger.Ita.Plugin.Test
{
    public class ItalianVariantBuilderTest
    {
        /// <summary>
        /// Setups the mock index so that on the specified input value it returns
        /// the specified output entries. Note that this requires to add ExpectedObjects
        /// from NuGet.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="input">The input value.</param>
        /// <param name="output">The output entries.</param>
        private static void SetupMockIndexEntry(Mock<ILookupIndex> index,
            string input, 
            params LookupEntry[] output)
        {
            // https://stackoverflow.com/questions/11271057/how-to-use-moq-to-verify-that-a-similar-object-was-passed-in-as-argument
            index.Setup(i => i.Find(It.Is<LookupFilter>(f => new LookupFilter
                {
                    Value = input,
                    PageNumber = 1,
                    PageSize = 100
                }.ToExpectedObject().Equals(f))))
                .Returns(new List<LookupEntry>(output));
        }

        private static ItalianVariantBuilderOptions GetZeroOptions()
        {
            ItalianVariantBuilderOptions options = new ItalianVariantBuilderOptions();
            options.SetAll(false);
            return options;
        }

        #region Superlatives
        [Fact]
        public void Build_Bellissimo_Bello()
        {
            var options = GetZeroOptions();
            options.Superlatives = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "bello", new LookupEntry
            {
                Signature = "A",
                Value = "bello",
                Text = "bello"
            });

            IList<Variant> variants = builder.Build("bellissimo", index.Object);

            Assert.Single(variants);
            Variant variant = variants[0];
            Assert.Equal("bello", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("bellissimo", variant.Source);
            Assert.Equal("super", variant.Type);
        }

        [Fact]
        public void Build_Pochissimo_Poco()
        {
            var options = GetZeroOptions();
            options.Superlatives = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "poco", new LookupEntry
            {
                Signature = "A",
                Value = "poco",
                Text = "poco"
            });

            IList<Variant> variants = builder.Build("pochissimo", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("poco", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("pochissimo", variant.Source);
            Assert.Equal("super", variant.Type);
        }

        [Fact]
        public void Build_Abilissimo_Abile()
        {
            var options = GetZeroOptions();
            options.Superlatives = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "abile", new LookupEntry
            {
                Signature = "A",
                Value = "abile",
                Text = "abile"
            });

            IList<Variant> variants = builder.Build("abilissimo", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("abile", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("abilissimo", variant.Source);
            Assert.Equal("super", variant.Type);
        }
        #endregion

        #region Enclitic groups
        [Fact]
        public void Build_Dammi_Da()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "da'", new LookupEntry
            {
                Signature = "V@DaMtTeP2Ns",
                Value = "da'",
                Text = "da'"
            });

            IList<Variant> variants = builder.Build("dammi", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("da'", variant.Value);
            Assert.Equal("V@DaMtTeP2Ns", variant.Signature);
            Assert.Equal("dammi", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Leggilo_Leggi()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "leggi", new LookupEntry
            {
                Signature = "V@DaMtTeP2Ns",
                Value = "leggi",
                Text = "leggi"
            });

            IList<Variant> variants = builder.Build("leggilo", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("leggi", variant.Value);
            Assert.Equal("V@DaMtTeP2Ns", variant.Signature);
            Assert.Equal("leggilo", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Fermiamoci_Fermiamo()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "fermiamo", new LookupEntry
            {
                Signature = "V@DaMtTeP1Np",
                Value = "fermiamo",
                Text = "fermiamo"
            });

            IList<Variant> variants = builder.Build("fermiamoci", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("fermiamo", variant.Value);
            Assert.Equal("V@DaMtTeP1Np", variant.Signature);
            Assert.Equal("fermiamoci", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Andarci_Andare()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "andare", new LookupEntry
            {
                Signature = "V@DaMfTe",
                Value = "andare",
                Text = "andare"
            });

            IList<Variant> variants = builder.Build("andarci", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("andare", variant.Value);
            Assert.Equal("V@DaMfTe", variant.Signature);
            Assert.Equal("andarci", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Porgli_Porre()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "porre", new LookupEntry
            {
                Signature = "V@DaMfTe",
                Value = "porre",
                Text = "porre"
            });

            IList<Variant> variants = builder.Build("porgli", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("porre", variant.Value);
            Assert.Equal("V@DaMfTe", variant.Signature);
            Assert.Equal("porgli", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Avendomi_Avendo()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "avendo", new LookupEntry
            {
                Signature = "V@DaMgTe",
                Value = "avendo",
                Text = "avendo"
            });

            IList<Variant> variants = builder.Build("avendomi", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("avendo", variant.Value);
            Assert.Equal("V@DaMgTe", variant.Signature);
            Assert.Equal("avendomi", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Allontanatomi_Allontanato()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "allontanato", new LookupEntry
            {
                Signature = "V@MpTr",
                Value = "allontanato",
                Text = "allontanato"
            });

            IList<Variant> variants = builder.Build("allontanatomi", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("allontanato", variant.Value);
            Assert.Equal("V@MpTr", variant.Signature);
            Assert.Equal("allontanatomi", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }

        [Fact]
        public void Build_Intrecciantesi_Intrecciante()
        {
            var options = GetZeroOptions();
            options.EncliticGroups = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "intrecciante", new LookupEntry
            {
                Signature = "V@MpTe",
                Value = "intrecciante",
                Text = "intrecciante"
            });

            IList<Variant> variants = builder.Build("intrecciantesi", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("intrecciante", variant.Value);
            Assert.Equal("V@MpTe", variant.Signature);
            Assert.Equal("intrecciantesi", variant.Source);
            Assert.Equal("enclitic", variant.Type);
        }
        #endregion

        #region Untruncated
        [Fact]
        public void Build_Suor_Suora()
        {
            var options = GetZeroOptions();
            options.UntruncatedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "suora", new LookupEntry
            {
                Signature = "S",
                Value = "suora",
                Text = "suora"
            });

            IList<Variant> variants = builder.Build("suor", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("suora", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("suor", variant.Source);
            Assert.Equal("untruncated", variant.Type);
        }

        [Fact]
        public void Build_Cuor_Cuore()
        {
            var options = GetZeroOptions();
            options.UntruncatedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "cuore", new LookupEntry
            {
                Signature = "S",
                Value = "cuore",
                Text = "cuore"
            });

            IList<Variant> variants = builder.Build("cuor", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("cuore", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("cuor", variant.Source);
            Assert.Equal("untruncated", variant.Type);
        }

        [Fact]
        public void Build_Tor_Torre()
        {
            var options = GetZeroOptions();
            options.UntruncatedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "torre", new LookupEntry
            {
                Signature = "S",
                Value = "torre",
                Text = "torre"
            });

            IList<Variant> variants = builder.Build("tor", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("torre", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("tor", variant.Source);
            Assert.Equal("untruncated", variant.Type);
        }
        #endregion

        #region Unelided
        [Fact]
        public void Build_Bell_Bello()
        {
            var options = GetZeroOptions();
            options.UnelidedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "bello", new LookupEntry
            {
                Signature = "A",
                Value = "bello",
                Text = "bello"
            });

            IList<Variant> variants = builder.Build("bell'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("bello", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("bell'", variant.Source);
            Assert.Equal("elided", variant.Type);
        }

        [Fact]
        public void Build_Bell_Bella()
        {
            var options = GetZeroOptions();
            options.UnelidedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "bella", new LookupEntry
            {
                Signature = "A",
                Value = "bella",
                Text = "bella"
            });

            IList<Variant> variants = builder.Build("bell'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("bella", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("bell'", variant.Source);
            Assert.Equal("elided", variant.Type);
        }

        [Fact]
        public void Build_Bell_Belli()
        {
            var options = GetZeroOptions();
            options.UnelidedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "belli", new LookupEntry
            {
                Signature = "A",
                Value = "belli",
                Text = "belli"
            });

            IList<Variant> variants = builder.Build("bell'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("belli", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("bell'", variant.Source);
            Assert.Equal("elided", variant.Type);
        }

        [Fact]
        public void Build_Bell_Belle()
        {
            var options = GetZeroOptions();
            options.UnelidedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "belle", new LookupEntry
            {
                Signature = "A",
                Value = "belle",
                Text = "belle"
            });

            IList<Variant> variants = builder.Build("bell'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("belle", variant.Value);
            Assert.Equal("A", variant.Signature);
            Assert.Equal("bell'", variant.Source);
            Assert.Equal("elided", variant.Type);
        }
        #endregion

        #region Apostrophe artifacts
        [Fact]
        public void Build_ApostropheLeft_NoApostrophe()
        {
            var options = GetZeroOptions();
            options.ApostropheArtifacts = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "oh", new LookupEntry
            {
                Signature = "N",
                Value = "oh",
                Text = "oh"
            });

            IList<Variant> variants = builder.Build("'oh", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("oh", variant.Value);
            Assert.Equal("N", variant.Signature);
            Assert.Equal("'oh", variant.Source);
            Assert.Equal("apostrophe", variant.Type);
        }

        [Fact]
        public void Build_ApostropheRight_NoApostrophe()
        {
            var options = GetZeroOptions();
            options.ApostropheArtifacts = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "oh", new LookupEntry
            {
                Signature = "N",
                Value = "oh",
                Text = "oh"
            });

            IList<Variant> variants = builder.Build("oh'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("oh", variant.Value);
            Assert.Equal("N", variant.Signature);
            Assert.Equal("oh'", variant.Source);
            Assert.Equal("apostrophe", variant.Type);
        }

        [Fact]
        public void Build_ApostropheLeftAndRight_NoApostrophe()
        {
            var options = GetZeroOptions();
            options.ApostropheArtifacts = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "oh", new LookupEntry
            {
                Signature = "N",
                Value = "oh",
                Text = "oh"
            });

            IList<Variant> variants = builder.Build("'oh'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("oh", variant.Value);
            Assert.Equal("N", variant.Signature);
            Assert.Equal("'oh'", variant.Source);
            Assert.Equal("apostrophe", variant.Type);
        }
        #endregion

        #region Accent artifacts
        [Fact]
        public void Build_CittaApostrophe_CittaGrave()
        {
            var options = GetZeroOptions();
            options.AccentArtifacts = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "città", new LookupEntry
            {
                Signature = "S",
                Value = "città",
                Text = "città"
            });

            IList<Variant> variants = builder.Build("citta'", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("città", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("citta'", variant.Source);
            Assert.Equal("accent", variant.Type);
        }

        [Fact]
        public void Build_CittaBacktick_CittaGrave()
        {
            var options = GetZeroOptions();
            options.AccentArtifacts = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "città", new LookupEntry
            {
                Signature = "S",
                Value = "città",
                Text = "città"
            });

            IList<Variant> variants = builder.Build("citta`", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("città", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("citta`", variant.Source);
            Assert.Equal("accent", variant.Type);
        }
        #endregion

        #region Iota
        [Fact]
        public void Build_Jeri_Ieri()
        {
            var options = GetZeroOptions();
            options.IotaVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "ieri", new LookupEntry
            {
                Signature = "N",
                Value = "ieri",
                Text = "ieri"
            });

            IList<Variant> variants = builder.Build("jeri", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("ieri", variant.Value);
            Assert.Equal("N", variant.Signature);
            Assert.Equal("jeri", variant.Source);
            Assert.Equal("iota", variant.Type);
        }
        #endregion

        #region Isc
        [Fact]
        public void Build_Iscuola_Scuola()
        {
            var options = GetZeroOptions();
            options.IscVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "scuola", new LookupEntry
                {
                    Signature = "S",
                    Value = "scuola",
                    Text = "scuola"
                });

            IList<Variant> variants = builder.Build("iscuola", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("scuola", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("iscuola", variant.Source);
            Assert.Equal("isc", variant.Type);
        }
        #endregion

        #region Accented
        [Fact]
        public void Build_CittaAcute_CittaGrave()
        {
            var options = GetZeroOptions();
            options.AccentedVariants = true;
            ItalianVariantBuilder builder = new ItalianVariantBuilder();
            builder.Configure(options);

            Mock<ILookupIndex> index = new Mock<ILookupIndex>();
            SetupMockIndexEntry(index, "città", new LookupEntry
            {
                Signature = "S",
                Value = "città",
                Text = "città"
            });

            IList<Variant> variants = builder.Build("cittá", index.Object);

            Assert.Equal(1, variants.Count);
            Variant variant = variants[0];
            Assert.Equal("città", variant.Value);
            Assert.Equal("S", variant.Signature);
            Assert.Equal("cittá", variant.Source);
            Assert.Equal("acute-grave", variant.Type);
        }
        #endregion
    }
}
