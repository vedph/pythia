using ConsoleTables;
using Corpus.Sql;
using Fusi.Cli;
using Fusi.Cli.Commands;
using Fusi.Tools.Data;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Configuration;
using Pythia.Cli.Services;
using Pythia.Core;
using Pythia.Sql;
using Pythia.Sql.PgSql;
using System.Threading.Tasks;

namespace Pythia.Cli.Commands;

public sealed class QueryCommand : ICommand
{
    private readonly QueryCommandOptions _options;
    private readonly History<string> _history;
    private readonly SearchRequest _request;
    private DataPage<SearchResult>? _page;
    private SqlIndexRepository? _repository;

    public QueryCommand(QueryCommandOptions options)
    {
        _options = options;
        _history = new History<string>();
        _request = new SearchRequest
        {
            Query = "[value=\"chommoda\"]",
            PageNumber = 1,
            PageSize = 20
        };
        _history.Add(_request.Query);
    }

    public static void Configure(CommandLineApplication app,
        ICliAppContext context)
    {
        app.Description =
            "Query the Pythia database with the specified name.";
        app.HelpOption("-?|-h|--help");

        CommandArgument dbNameArgument = app.Argument("[dbName]",
            "The database name.");

        app.OnExecute(() =>
        {
            context.Command = new QueryCommand(
                new QueryCommandOptions(context)
                {
                    DbName = dbNameArgument.Value
                });
            return 0;
        });
    }

    private void HandleHistory()
    {
        if (_history.Count == 0) return;

        _request.Query = Prompt.ForHistory("Enter nr.: ", _history);
        ColorConsole.WriteInfo(_request.Query);
    }

    private void ShowPage()
    {
        if (_page == null || _page.Total == 0)
        {
            ColorConsole.WriteInfo("(no result)");
            return;
        }

        while (true)
        {
            ColorConsole.WriteInfo("-- page " +
                $"{_page.PageNumber}/{_page.PageCount} ({_page.Total})");

            ConsoleTable table = ConsoleTable.From(_page.Items);
            table.Options.EnableCount = false;
            table.Write();

            ColorConsole.WriteEmbeddedColorLine(
                "[cyan]N[/cyan]ext | [cyan]P[/cyan]rev | [cyan]F[/cyan]irst | " +
                "[cyan]L[/cyan]ast | [yellow]C[/yellow]lose");
            switch (Prompt.ForChar(""))
            {
                case 'n':
                    if (_page.PageNumber == _page.PageCount) break;
                    _request.PageNumber++;
                    _page = _repository!.Search(_request);
                    break;

                case 'p':
                    if (_page.PageNumber == 1) break;
                    _request.PageNumber--;
                    _page = _repository!.Search(_request);
                    break;

                case 'f':
                    if (_page.PageNumber == 1) break;
                    _request.PageNumber = 1;
                    _page = _repository!.Search(_request);
                    break;

                case 'l':
                    if (_page.PageNumber == _page.PageCount) break;
                    _request.PageNumber = _page.PageCount;
                    _page = _repository!.Search(_request);
                    break;

                case 'c':
                    return;
            }
        }
    }

    public Task Run()
    {
        ColorConsole.WriteWrappedHeader("Query");

        string cs = string.Format(
            _options.Context!.Configuration!.GetConnectionString("Default")!,
            _options.DbName);
        _repository = new PgSqlIndexRepository();
        _repository.Configure(new SqlRepositoryOptions
        {
            ConnectionString = cs
        });

        while (true)
        {
            string query = Prompt.ForString("Query (x=exit, h=history): ",
                "[value=\"chommoda\"]");
            switch (query)
            {
                case "x":
                    return Task.FromResult(0);

                case "h":
                    HandleHistory();
                    _request.PageNumber = 1;
                    _page = _repository.Search(_request);
                    ShowPage();
                    break;

                default:
                    _history.Add(query);
                    _request.PageNumber = 1;
                    _page = _repository.Search(_request);
                    ShowPage();
                    break;
            }
        }
    }
}

public class QueryCommandOptions : CommandOptions<PythiaCliAppContext>
{
    public QueryCommandOptions(ICliAppContext options)
    : base((PythiaCliAppContext)options)
    {
    }

    public string? DbName { get; set; }
}