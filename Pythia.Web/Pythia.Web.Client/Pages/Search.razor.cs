using Fusi.Tools.Data;
using Microsoft.AspNetCore.Components;
using Pythia.Core;
using Pythia.Core.Analysis;
using Radzen;

namespace Pythia.Web.Client.Pages;

public partial class Search : ComponentBase
{
    [Parameter]
    public string? Query { get; set; }

    [Parameter]
    public int ContextSize { get; set; } = 5;

    public DataPage<KwicSearchResult>? CurrentPage { get; set; }

    private DataPage<KwicSearchResult> GetPage(int pageNumber,
        int pageSize, IList<string>? sortFields = null)
    {
        try
        {
            IList<ILiteralFilter> filters = FactoryProvider.GetFactory()
                .GetLiteralFilters();

            DataPage<SearchResult> page = Repository.Search(new SearchRequest
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Query = Query,
                SortFields = sortFields
            }, filters);

            IList<KwicSearchResult> results =
                Repository.GetResultContext(page.Items, ContextSize);

            return new DataPage<KwicSearchResult>(pageNumber, pageSize,
                page.Total, results);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Search failed: {Error}", ex.Message);
            throw;
        }
    }

    private Task LoadData(LoadDataArgs args)
    {
        if (Query == null)
        {
            CurrentPage = null;
        }
        else
        {
            // get the current page number and page size
            int pageSize = args.Top ?? 20;
            int pageNumber = ((args.Skip ?? 0) / pageSize) + 1;
            // TODO args.OrderBy

            CurrentPage = GetPage(pageNumber, pageSize);
        }

        return Task.CompletedTask;
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (Query != null)
        {
            CurrentPage = GetPage(1, 20);
        }
    }

    private void OnSearch()
    {
        CurrentPage = GetPage(1, 20);
    }
}
