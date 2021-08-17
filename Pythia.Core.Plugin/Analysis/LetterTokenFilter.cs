﻿using System;
using System.Linq;
using Fusi.Tools.Config;
using Pythia.Core.Analysis;

namespace Pythia.Core.Plugin.Analysis
{
    /// <summary>
    /// A token filter which removes from <see cref="Token.Value"/> any
    /// non-letter/digit/' char.
    /// </summary>
    /// <seealso cref="ITokenFilter" />
    [Tag("token-filter.alnumapos")]
    public sealed class LetterTokenFilter : ITokenFilter
    {
        /// <summary>
        /// Filters the specified token.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <param name="position">The position which will be assigned to
        /// the resulting token, provided that it's not empty. Not used.
        /// </param>
        /// <returns>The input token (used for chaining)</returns>
        /// <exception cref="ArgumentNullException">null token</exception>
        public void Apply(Token token, int position)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            token.Value = new string(token.Value
                .Where(c => char.IsLetterOrDigit(c) || c == '\'')
                .ToArray());
        }
    }
}
