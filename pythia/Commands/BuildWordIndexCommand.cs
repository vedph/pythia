using Corpus.Sql;
using Pythia.Cli.Services;
using Pythia.Sql.PgSql;
using Pythia.Sql;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Threading;
using Fusi.Tools;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics;
using MessagingApi.Extras;
using MessagingApi.Mailjet;

namespace Pythia.Cli.Commands;

/// <summary>
/// Build words index from tokens in the specified database.
/// </summary>
internal sealed class BuildWordIndexCommand :
    AsyncCommand<BuildWordIndexCommandSettings>
{
    private MessageSink? _sink;

    private async Task Notify(MessageSinkEntry entry)
    {
        if (_sink == null) return;

        try
        {
            await _sink.AddEntry(entry);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Notification error: ", ex.ToString());
            AnsiConsole.WriteException(ex);
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context,
        BuildWordIndexCommandSettings settings, CancellationToken cancel)
    {
        AnsiConsole.MarkupLine("[red underline]INDEX WORDS[/]");
        AnsiConsole.MarkupLine($"Database: [cyan]{settings.DbName}[/]");
        AnsiConsole.MarkupLine($"Language: [cyan]{settings.Language ?? "NULL"}[/]");
        if (settings.BinCounts.Length > 0)
        {
            AnsiConsole.MarkupLine(
                $"Bin counts: [cyan]{string.Join(",", settings.BinCounts)}[/]");
        }
        if (settings.ExcludedDocAttrs.Length > 0)
        {
            AnsiConsole.MarkupLine("Excluded doc attrs: " +
                $"[cyan]{string.Join(",", settings.ExcludedDocAttrs)}[/]");
        }
        if (settings.ExcludedPosValues.Length > 0)
        {
            AnsiConsole.MarkupLine("Excluded POS values: " +
                $"[cyan]{string.Join(",", settings.ExcludedPosValues)}[/]");
        }
        if (settings.ExcludedSpanAttrs.Length > 0)
        {
            AnsiConsole.MarkupLine("Excluded span attrs: " +
                $"[cyan]{string.Join(",", settings.ExcludedSpanAttrs)}[/]");
        }

        // setup notification if requested
        if (!string.IsNullOrEmpty(settings.NotifierEmail))
        {
            if (string.IsNullOrEmpty(settings.NotifierEmail))
            {
                AnsiConsole.MarkupLine("[red]Notifier email not set[/]");
                return 1;
            }

            AnsiConsole.MarkupLine(
                $"Notifier email: [cyan]{settings.NotifierEmail}[/]");
            AnsiConsole.MarkupLine(
                $"Notifier span: [cyan]{settings.NotifierSpan}[/]");
            AnsiConsole.MarkupLine(
                $"Notifier tail limit: [cyan]{settings.NotifierLimit}[/]");

            _sink = new(new MailjetMailerService(
                new MailjetMailerOptions
                {
                    SenderEmail = settings.NotifierEmail,
                    SenderName = "Pythia Indexing Bot",
                    IsEnabled = true
                }), new MessageSinkOptions
                {
                    RecipientAddress = settings.NotifierEmail!,
                    FlushSpan = TimeSpan.FromMinutes(settings.NotifierSpan),
                    MaxTailSize = settings.NotifierLimit,
                    ImmediateFlushThreshold = 1
                });
            _sink.OnFlush += (_) => AnsiConsole.MarkupLine(
                $"[yellow]Sending message #{_sink.MessageCount}[/]");
        }

        try
        {
            string cs = string.Format(
                CliAppContext.Configuration!.GetConnectionString("Default")!,
                settings.DbName);

            SqlIndexRepository repository = new PgSqlIndexRepository();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });

            string? prevMessage = null;
            int prevPercent = -1;

            if (_sink != null && settings.NotifyStart)
            {
                await _sink.AddEntry(
                    new MessageSinkEntry(0, "Building word indexes..."));
                await _sink.FlushAsync();
            }

            await repository.BuildWordIndexAsync(
                settings.Language,
                settings.ParseBinCounts(),
                [.. settings.ExcludedDocAttrs],
                [.. settings.ExcludedSpanAttrs],
                [.. settings.ExcludedPosValues],
                CancellationToken.None,
                new Progress<ProgressReport>(async report =>
                {
                    prevMessage = report.Message;
                    prevPercent = report.Percent;

                    AnsiConsole.MarkupLine(
                        $"[yellow]{report.Percent:000}[/] " +
                        $"[green]{DateTime.Now:HH:mm:ss}[/] " +
                        $"[cyan]{report.Message}[/]");

                    if (_sink != null)
                    {
                        await Notify(new MessageSinkEntry(0,
                            $"{report.Count}: {report.Message}"));
                    }
                }));

            AnsiConsole.MarkupLine("[green]Completed[/]");
            return 0;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
            AnsiConsole.WriteException(ex);
            return 1;
        }
    }
}

public class BuildWordIndexCommandSettings : CommandSettings
{
    [Description("The database name")]
    [CommandOption("-d|--db <NAME>")]
    [DefaultValue("pythia")]
    public string DbName { get; set; } = "pythia";

    [Description("The language code for the index (not set=NULL)")]
    [CommandOption("-l|--lang <LANG>")]
    public string? Language { get; set; }

    [Description("The class counts for document attribute bins ([^]name=N, multiple)")]
    [CommandOption("-c|--class-counts <COUNTS>")]
    [DefaultValue(new string[] { "date_value=3" })]
    public string[] BinCounts { get; set; } = ["date_value=3"];

    [Description("The document attributes to exclude from word index (multiple)")]
    [CommandOption("-x|--exclude-doc <ATTR>")]
    [DefaultValue(new string[] { "date" })]
    public string[] ExcludedDocAttrs { get; set; } = ["date"];

    [Description("The span attributes names to exclude from word index (multiple)")]
    [CommandOption("-n|--exclude-span <ATTR>")]
    public string[] ExcludedSpanAttrs { get; set; } = [];

    [Description("The POS values to exclude from the index (multiple)")]
    [CommandOption("-p|--exclude-pos <POS>")]
    public string[] ExcludedPosValues { get; set; } = [];

    [Description("The email address to send notifications to")]
    [CommandOption("--n-email <EMAIL>")]
    public string? NotifierEmail { get; set; }

    [Description("The timespan in minutes to wait between notifications (15')")]
    [CommandOption("--n-span <SPAN>")]
    [DefaultValue(15)]
    public int NotifierSpan { get; set; } = 15;

    [Description("The maximum number of entries to keep in the notifier's tail (100)")]
    [CommandOption("--n-limit <LIMIT>")]
    [DefaultValue(100)]
    public int NotifierLimit { get; set; } = 100;

    [Description("Whether to notify the start of the indexing process")]
    [CommandOption("--n-start")]
    public bool NotifyStart { get; set; }

    public Dictionary<string, int> ParseBinCounts()
    {
        Regex r = new(@"^([^=]+)=([0-9]+)$");

        Dictionary<string, int> dct = [];
        foreach (string s in BinCounts)
        {
            Match m = r.Match(s);
            if (m.Success)
            {
                dct[m.Groups[1].Value] = int.Parse(
                    m.Groups[2].Value, CultureInfo.InvariantCulture);
            }
        }
        return dct;
    }
}
