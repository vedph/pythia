using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using Pythia.Core;
using Pythia.Core.Analysis;
using Pythia.Core.Query;
using System;
using System.Collections.Generic;

namespace Pythia.Sql
{
    /// <summary>
    /// SQL query builder.
    /// </summary>
    public sealed class SqlQueryBuilder
    {
        private readonly ISqlHelper _sqlHelper;

        /// <summary>
        /// Gets or sets the optional literal filters.
        /// </summary>
        public IList<ILiteralFilter> LiteralFilters { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlQueryBuilder"/> class.
        /// </summary>
        /// <param name="sqlHelper">The SQL helper.</param>
        /// <exception cref="ArgumentNullException">sqlHelper</exception>
        public SqlQueryBuilder(ISqlHelper sqlHelper)
        {
            _sqlHelper = sqlHelper
                ?? throw new ArgumentNullException(nameof(sqlHelper));
        }

        /// <summary>
        /// Builds an SQL query from the specified Pythia query.
        /// </summary>
        /// <param name="request">The Pythia query request.</param>
        /// <returns>A tuple with 1=results page SQL query, and 2=total count
        /// SQL query.</returns>
        /// <exception cref="ArgumentNullException">request</exception>
        public Tuple<string,string> Build(SearchRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            AntlrInputStream input = new(request.Query);
            pythiaLexer lexer = new(input);
            CommonTokenStream tokens = new(lexer);
            pythiaParser parser = new(tokens);

            // throw at any parser error
            parser.RemoveErrorListeners();
            parser.AddErrorListener(new ThrowingErrorListener());

            pythiaParser.QueryContext tree = parser.query();
            ParseTreeWalker walker = new();
            SqlPythiaListener listener = new(
                lexer.Vocabulary,
                _sqlHelper)
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };
            if (LiteralFilters?.Count > 0)
            {
                foreach (ILiteralFilter filter in LiteralFilters)
                    listener.LiteralFilters.Add(filter);
            }

            walker.Walk(listener, tree);
            return Tuple.Create(listener.GetSql(false), listener.GetSql(true));
        }
    }
}
