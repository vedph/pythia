using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Api.Services;
using Pythia.Core;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Pythia.Api.Controllers
{
    /// <summary>
    /// Search.
    /// </summary>
    /// <seealso cref="ControllerBase" />
    [ApiController]
    public sealed class SearchController : ControllerBase
    {
        private readonly IIndexRepository _repository;
        private readonly IQueryPythiaFactoryProvider _factoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchController"/> class.
        /// </summary>
        /// <param name="repository">The repository.</param>
        /// <param name="factoryProvider">The factory provider, used to get
        /// the optional literal filters.</param>
        public SearchController(IIndexRepository repository,
            IQueryPythiaFactoryProvider factoryProvider)
        {
            _repository = repository
                ?? throw new ArgumentNullException(nameof(repository));
            _factoryProvider = factoryProvider
                ?? throw new ArgumentNullException(nameof(factoryProvider));
        }

        /// <summary>
        /// Executes the search specified.
        /// </summary>
        /// <param name="model">The query model.</param>
        /// <returns>page of results</returns>
        [HttpPost("api/search")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        public ActionResult<ResultWrapperModel<DataPage<KwicSearchResult>>>
            Search([FromBody] SearchBindingModel model)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                IList<ILiteralFilter> filters = _factoryProvider.GetFactory()
                    .GetLiteralFilters();

                DataPage<SearchResult> page = _repository.Search(new SearchRequest
                {
                    PageNumber = model.PageNumber,
                    PageSize = model.PageSize,
                    Query = model.Query
                }, filters);

                IList<KwicSearchResult> results =
                    _repository.GetResultContext(page.Items, model.ContextSize ?? 5);
                DataPage<KwicSearchResult> wrapped =
                    new(
                        model.PageNumber, model.PageSize, page.Total, results);

                return Ok(new ResultWrapperModel<DataPage<KwicSearchResult>>
                {
                    Value = wrapped
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                return Ok(new ResultWrapperModel<DataPage<KwicSearchResult>>
                {
                    Error = ex.Message
                });
            }
        }
    }
}
