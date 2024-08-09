using CsvHelper;
using Fusi.Tools.Data;
using Microsoft.AspNetCore.Mvc;
using Pythia.Api.Models;
using Pythia.Api.Services;
using Pythia.Core;
using Pythia.Core.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Pythia.Api.Controllers;

/// <summary>
/// Search.
/// </summary>
/// <seealso cref="ControllerBase" />
[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IIndexRepository _repository;
    private readonly IQueryPythiaFactoryProvider _factoryProvider;
    private readonly ILogger<SearchController> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchController"/> class.
    /// </summary>
    /// <param name="repository">The repository.</param>
    /// <param name="factoryProvider">The factory provider, used to get
    /// the optional literal filters.</param>
    public SearchController(IIndexRepository repository,
        IQueryPythiaFactoryProvider factoryProvider,
        ILogger<SearchController> logger,
        IWebHostEnvironment environment)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
        _factoryProvider = factoryProvider
            ?? throw new ArgumentNullException(nameof(factoryProvider));
        _logger = logger
            ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment
            ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Executes the search specified.
    /// </summary>
    /// <param name="model">The query model.</param>
    /// <returns>page of results</returns>
    [HttpGet()]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public ActionResult<ResultWrapperModel<DataPage<KwicSearchResult>>>
        Search([FromQuery] SearchBindingModel model)
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
                Query = model.Query,
                SortFields = model.SortFields
            }, filters);

            IList<KwicSearchResult> results =
                _repository.GetResultContext(page.Items, model.ContextSize ?? 5);
            DataPage<KwicSearchResult> wrapped =
                new(model.PageNumber, model.PageSize, page.Total, results);

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

    private static void WriteCsvHeader(int contextSize, CsvWriter csv)
    {
        csv.WriteField("id");
        csv.WriteField("doc_id");
        csv.WriteField("p1");
        csv.WriteField("p2");
        csv.WriteField("index");
        csv.WriteField("length");
        csv.WriteField("type");
        for (int i = 0; i < contextSize; i++)
            csv.WriteField($"c{i - contextSize}");
        csv.WriteField("value");
        for (int i = 0; i < contextSize; i++)
            csv.WriteField($"c{i + 1}");
        csv.WriteField("author");
        csv.WriteField("title");
        csv.WriteField("sort");
        csv.NextRecord();
    }

    private static void WriteCsvResult(KwicSearchResult result, CsvWriter csv)
    {
        csv.WriteField(result.Id);
        csv.WriteField(result.DocumentId);
        csv.WriteField(result.P1);
        csv.WriteField(result.P2);
        csv.WriteField(result.Index);
        csv.WriteField(result.Length);
        csv.WriteField(result.Type);
        foreach (string s in result.LeftContext)
            csv.WriteField(s);
        csv.WriteField(result.Value);
        foreach (string s in result.RightContext)
            csv.WriteField(s);
        csv.WriteField(result.Author);
        csv.WriteField(result.Title);
        csv.WriteField(result.SortKey);
        csv.NextRecord();
    }

    private async Task ExportDataAsync(ExportSearchBindingModel model,
        CsvWriter csvWriter, CancellationToken cancel)
    {
        try
        {
            // prepare the search request
            SearchRequest request = new()
            {
                Query = model.Query,
                PageNumber = model.PageNumber,
                PageSize = model.PageSize
            };

            int lastPage = model.LastPage ?? 0;

            while ((lastPage == 0 || request.PageNumber <= lastPage) &&
                !cancel.IsCancellationRequested)
            {
                // perform the search
                DataPage<SearchResult> page = _repository.Search(request);
                if (page.PageCount == 0) break;

                // update last page if needed
                if (lastPage == 0 || lastPage > page.PageCount)
                    lastPage = page.PageCount;

                // get the result context
                IList<KwicSearchResult> results = _repository.GetResultContext(
                    page.Items, model.ContextSize ?? 5);

                // write results to CSV
                foreach (var result in results)
                {
                    WriteCsvResult(result, csvWriter);
                    await csvWriter.FlushAsync();
                }

                // move to the next page
                request.PageNumber++;
            }
            await csvWriter.FlushAsync();
        }
        catch (OperationCanceledException)
        {
            // The operation was canceled, do nothing
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during export");
        }
    }

    [HttpGet("csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportSearchAsync([FromQuery]
        ExportSearchBindingModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Query))
            return BadRequest("Query is required.");

        _logger.LogInformation("Export search: {SearchModel}\n", model);

        // set response headers for CSV download
        Response.Headers.Append(
            "Content-Disposition", "attachment; filename=search_results.csv");
        Response.ContentType = "text/csv";

        // create a token that will be canceled when the client disconnects
        using CancellationTokenSource cts = new();
        HttpContext.RequestAborted.Register(() => cts.Cancel());

        // use a custom StreamWriter that writes to the response body
        await using StreamWriter streamWriter = new(Response.Body, Encoding.UTF8);
        await using CsvWriter csvWriter =
            new(streamWriter, CultureInfo.InvariantCulture);

        // write the CSV header
        WriteCsvHeader(model.ContextSize ?? 5, csvWriter);

        // start the export process as a low-priority task
        try
        {
            await Task.Factory.StartNew(
                async () => await ExportDataAsync(model, csvWriter, cts.Token),
                cts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting export task");

            if (!_environment.IsProduction())
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ex.ToString());
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    "An error occurred while processing your request.");
            }
        }

        return new EmptyResult();
    }
}
