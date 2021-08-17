using System.Linq;
using Fusi.Tools.Config;
using Pythia.Core.Analysis;
using System;
using System.Globalization;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// Token value's length supplier. This filter just adds an attribute
    /// to the token, with name <c>len</c> (or the one specified by the
    /// configuration) and value equal to the length of the token's value,
    /// counting only its letters.
    /// </summary>
    /// <seealso cref="Pythia.Core.Analysis.ITokenFilter" />
    [Tag("token-filter.len-supplier")]
    public sealed class LenSupplierTokenFilter : ITokenFilter,
        IConfigurable<LenSupplierTokenFilterOptions>
    {
        private LenSupplierTokenFilterOptions _options;

        /// <summary>
        /// Apply the filter to the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. Not used.
        /// </param>
        /// <exception cref="ArgumentNullException">token</exception>
        public void Apply(Token token, int position)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            int len = _options?.LetterOnly == true?
                token.Value.Count(c => char.IsLetter(c)) :
                token.Value.Length;

            token.Attributes.Add(new Corpus.Core.Attribute
            {
                TargetId = token.Position,
                Name = _options?.AttributeName ?? "len",
                Value = len.ToString(CultureInfo.InvariantCulture),
                Type = Corpus.Core.AttributeType.Number
            });
        }

        /// <summary>
        /// Configures the object with the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        public void Configure(LenSupplierTokenFilterOptions options)
        {
            _options = options;
        }
    }

    /// <summary>
    /// Options for <see cref="LenSupplierTokenFilter"/>.
    /// </summary>
    public sealed class LenSupplierTokenFilterOptions
    {
        /// <summary>
        /// Gets or sets the name of the attribute supplied by this filter.
        /// The default is <c>len</c>.
        /// </summary>
        public string AttributeName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether only letters should
        /// be counted when calculating the token value's length.
        /// </summary>
        public bool LetterOnly { get; set; }
    }
}
