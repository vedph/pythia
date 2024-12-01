using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System.Threading.Tasks;
using Pythia.Api.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using Pythia.Api.Controllers;
using MessagingApi;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;
using Fusi.Api.Auth.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Corpus.Core;
using Corpus.Sql;
using Fusi.Api.Auth.Services;
using Pythia.Core;
using Pythia.Sql.PgSql;

namespace Pythia.Api;

/// <summary>
/// Main program.
/// </summary>
public static class Program
{
    // startup log file name, Serilog is configured later via appsettings.json
    private const string STARTUP_LOG_NAME = "startup.log";

    private static void DumpEnvironmentVars()
    {
        Console.WriteLine("ENVIRONMENT VARIABLES:");
        IDictionary dct = Environment.GetEnvironmentVariables();
        List<string> keys = [];
        var enumerator = dct.GetEnumerator();
        while (enumerator.MoveNext())
        {
            keys.Add(((DictionaryEntry)enumerator.Current).Key.ToString()!);
        }

        foreach (string key in keys.OrderBy(s => s))
            Console.WriteLine($"{key} = {dct[key]}");
    }

    #region Options
    private static void ConfigureOptionsServices(IServiceCollection services,
        IConfiguration config)
    {
        // configuration sections
        // https://andrewlock.net/adding-validation-to-strongly-typed-configuration-objects-in-asp-net-core/
        services.Configure<MessagingOptions>(config.GetSection("Messaging"));
        services.Configure<DotNetMailerOptions>(config.GetSection("Mailer"));

        // explicitly register the settings object by delegating to the IOptions object
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<MessagingOptions>>().Value);
        services.AddSingleton(resolver =>
            resolver.GetRequiredService<IOptions<DotNetMailerOptions>>().Value);
    }
    #endregion

    #region CORS
    private static void ConfigureCorsServices(IServiceCollection services,
        IConfiguration config)
    {
        string[] origins = ["http://localhost:4200"];

        IConfigurationSection section = config.GetSection("AllowedOrigins");
        if (section.Exists())
        {
            origins = section.AsEnumerable()
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => p.Value).ToArray()!;
        }

        services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
        {
            builder.AllowAnyMethod()
                .AllowAnyHeader()
                // https://github.com/aspnet/SignalR/issues/2110 for AllowCredentials
                .AllowCredentials()
                .WithOrigins(origins);
        }));
    }
    #endregion

    #region Auth
    private static void ConfigureAuthServices(IServiceCollection services,
        IConfiguration config)
    {
        // identity
        string csTemplate = config.GetConnectionString("Default")!;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(string.Format(csTemplate,
                config.GetValue<string>("DatabaseName")));
        });

        services.AddIdentity<NamedUser, IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // authentication service
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
           .AddJwtBearer(options =>
           {
               // NOTE: remember to set the values in configuration:
               // Jwt:SecureKey, Jwt:Audience, Jwt:Issuer
               IConfigurationSection jwtSection = config.GetSection("Jwt");
               string? key = jwtSection["SecureKey"];
               if (string.IsNullOrEmpty(key))
                   throw new InvalidOperationException("Required JWT SecureKey not found");

               options.SaveToken = true;
               options.RequireHttpsMetadata = false;
               options.TokenValidationParameters = new TokenValidationParameters()
               {
                   ValidateIssuer = true,
                   ValidateAudience = true,
                   ValidAudience = jwtSection["Audience"],
                   ValidIssuer = jwtSection["Issuer"],
                   IssuerSigningKey = new SymmetricSecurityKey(
                       Encoding.UTF8.GetBytes(key))
               };

               // support refresh
               // https://stackoverflow.com/questions/55150099/jwt-token-expiration-time-failing-net-core
               options.Events = new JwtBearerEvents
               {
                   OnAuthenticationFailed = context =>
                   {
                       if (context.Exception.GetType() ==
                            typeof(SecurityTokenExpiredException))
                       {
                           context.Response.Headers["Token-Expired"] = "true";
                       }
                       return Task.CompletedTask;
                   }
               };
           });
#if DEBUG
        // use to show more information when troubleshooting JWT issues
        IdentityModelEventSource.ShowPII = true;
#endif
    }
    #endregion

    #region Rate limiter
    private static async Task NotifyLimitExceededToRecipients(IConfiguration config,
        IHostEnvironment hostEnvironment)
    {
        // mailer must be enabled
        if (!config.GetValue<bool>("Mailer:IsEnabled"))
        {
            Log.Information("Mailer not enabled");
            return;
        }

        // recipients must be set
        IConfigurationSection recSection = config.GetSection("Mailer:Recipients");
        if (!recSection.Exists()) return;
        string[] recipients = recSection.AsEnumerable()
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .Select(p => p.Value!).ToArray();
        if (recipients.Length == 0)
        {
            Log.Information("No recipients for limit notification");
            return;
        }

        // build message
        MessagingOptions msgOptions = new();
        config.GetSection("Messaging").Bind(msgOptions);
        FileMessageBuilderService messageBuilder = new(
            msgOptions,
            hostEnvironment);

        Message? message = messageBuilder.BuildMessage("rate-limit-exceeded",
            new Dictionary<string, string>()
            {
                ["EventTime"] = DateTime.UtcNow.ToString()
            });
        if (message == null)
        {
            Log.Warning("Unable to build limit notification message");
            return;
        }

        // send message to all the recipients
        DotNetMailerOptions mailerOptions = new();
        config.GetSection("Mailer").Bind(mailerOptions);
        DotNetMailerService mailer = new(mailerOptions);

        foreach (string recipient in recipients)
        {
            Log.Logger.Information("Sending rate email message");
            await mailer.SendEmailAsync(
                recipient,
                "Test Recipient",
                message);
            Log.Logger.Information("Email message sent");
        }
    }

    private static void ConfigureRateLimiterService(IServiceCollection services,
        IConfiguration config, IHostEnvironment hostEnvironment)
    {
        // nope if Disabled
        IConfigurationSection limit = config.GetSection("RateLimit");
        if (limit.GetValue("IsDisabled", false))
        {
            Log.Information("Rate limiter is disabled");
            return;
        }

        // PermitLimit (10)
        int permit = limit.GetValue("PermitLimit", 10);
        if (permit < 1) permit = 10;

        // QueueLimit (0)
        int queue = limit.GetValue("QueueLimit", 0);

        // TimeWindow (00:01:00 = HH:MM:SS)
        string? windowText = limit.GetValue<string>("TimeWindow");
        TimeSpan window;
        if (!string.IsNullOrEmpty(windowText))
        {
            if (!TimeSpan.TryParse(windowText, CultureInfo.InvariantCulture, out window))
                window = TimeSpan.FromMinutes(1);
        }
        else
        {
            window = TimeSpan.FromMinutes(1);
        }

        Log.Information("Configuring rate limiter: " +
            "limit={PermitLimit}, queue={QueueLimit}, window={Window}",
            permit, queue, window);

        // https://blog.maartenballiauw.be/post/2022/09/26/aspnet-core-rate-limiting-middleware.html
        // default = 10 requests per minute, per authenticated username,
        // or hostname if not authenticated.
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter
                .Create<HttpContext, string>(httpContext =>
                {
                    string key = httpContext.User.Identity?.Name
                        ?? httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown";
                    Log.Information("Rate limit key: {Key}", key);

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: key,
                        factory: partition => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            PermitLimit = permit,
                            QueueLimit = queue,
                            Window = window
                        });
                });

            options.OnRejected = async (context, token) =>
            {
                Log.Warning("Rate limit exceeded");

                // 429 too many requests
                context.HttpContext.Response.StatusCode = 429;

                // send
                await NotifyLimitExceededToRecipients(config, hostEnvironment);

                // ret JSON with error
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter,
                    out var retryAfter))
                {
                    await context.HttpContext.Response.WriteAsync("{\"error\": " +
                        "\"Too many requests. Please try again after " +
                        $"{retryAfter.TotalMinutes} minute(s).\"" +
                        "}", cancellationToken: token);
                }
                else
                {
                    await context.HttpContext.Response.WriteAsync(
                        "{\"error\": " +
                        "\"Too many requests. Please try again later.\"" +
                        "}", cancellationToken: token);
                }
            };
        });
    }
    #endregion

    #region Messaging
    private static void ConfigureMessagingServices(IServiceCollection services)
    {
        services.AddScoped<IMailerService, DotNetMailerService>();
        services.AddScoped<IMessageBuilderService,
            FileMessageBuilderService>();
    }
    #endregion

    #region Logging
    private static void ConfigureLogging(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton(sp =>
        {
            ILoggerFactory factory = sp.GetRequiredService<ILoggerFactory>();
            return factory.CreateLogger("Logger");
        });

        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            // https://github.com/serilog/serilog-settings-configuration
            loggerConfiguration.ReadFrom.Configuration(
                hostingContext.Configuration);

            loggerConfiguration.WriteTo.File("cadmus.log",
                rollingInterval: RollingInterval.Day);

            loggerConfiguration.WriteTo.Console();
        });
    }
    #endregion

    #region Pythia
    private static void ConfigureAppServices(IServiceCollection services,
        IConfiguration config)
    {
        // user repository service
        services.AddScoped<IUserRepository<NamedUser>,
            UserRepository<NamedUser, IdentityRole>>();

        // pythia repository services
        string cs = string.Format(
            config.GetConnectionString("Default")!,
            config.GetValue<string>("DatabaseName"));
        services.AddScoped<ICorpusRepository>(_ =>
        {
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            return repository;
        });
        services.AddScoped<IIndexRepository>(_ =>
        {
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            return repository;
        });

        // pythia factories
        services.AddSingleton<IQueryPythiaFactoryProvider>(_ =>
        {
            // the "query" profile is reserved for literal filters, if any
            // IIndexRepository repository = new PgSqlIndexRepository(cs)
            PgSqlIndexRepository repository = new();
            repository.Configure(new SqlRepositoryOptions
            {
                ConnectionString = cs
            });
            string profile = repository.GetProfile("query")?.Content ?? "{}";
            return new StandardQueryPythiaFactoryProvider(profile, cs);
        });

        services.AddSingleton<IPythiaFactoryProvider>(
            _ => new StandardPythiaFactoryProvider(cs));
    }
    #endregion

    /// <summary>
    /// Entry point.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns>0=ok, else error.</returns>
    public static async Task<int> Main(string[] args)
    {
        // early startup logging to ensure we catch any exceptions
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console()
#if DEBUG
            .WriteTo.File(STARTUP_LOG_NAME, rollingInterval: RollingInterval.Day)
#endif
            .CreateLogger();

        try
        {
            Log.Information("Starting Pythia host");
            DumpEnvironmentVars();

            // create builder
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
            IConfiguration config = new ConfigurationService(builder.Environment)
                .Configuration;

            // configure services
            ConfigureCorsServices(builder.Services, config);
            ConfigureAuthServices(builder.Services, config);
            ConfigureOptionsServices(builder.Services, config);
            ConfigureLogging(builder);
            ConfigureRateLimiterService(builder.Services, config, builder.Environment);
            ConfigureMessagingServices(builder.Services);
            ConfigureAppServices(builder.Services, config);

            // IMemoryCache: https://docs.microsoft.com/en-us/aspnet/core/performance/caching/memory
            builder.Services.AddMemoryCache();
            // add OpenAPI
            builder.Services.AddOpenApi();
            // add controllers
            builder.Services.AddControllers()
                .AddApplicationPart(typeof(DocumentController).Assembly)
                .AddControllersAsServices();

            WebApplication app = builder.Build();

            // forward headers for use with an eventual reverse proxy
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor
                    | ForwardedHeaders.XForwardedProto
            });

            // development or production
            if (builder.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-5.0&tabs=visual-studio
                app.UseExceptionHandler("/Error");
                if (config.GetValue<bool>("Server:UseHSTS"))
                {
                    Console.WriteLine("HSTS: yes");
                    app.UseHsts();
                }
                else
                {
                    Console.WriteLine("HSTS: no");
                }
            }

            // HTTPS redirection
            if (config.GetValue<bool>("Server:UseHttpsRedirection"))
            {
                Console.WriteLine("HttpsRedirection: yes");
                app.UseHttpsRedirection();
            }
            else
            {
                Console.WriteLine("HttpsRedirection: no");
            }

            // CORS
            app.UseCors("CorsPolicy");
            // rate limiter
            if (!config.GetValue<bool>("RateLimit:IsDisabled"))
                app.UseRateLimiter();
            // authentication
            app.UseAuthentication();
            app.UseAuthorization();
            // proxy
            app.UseResponseCaching();

            // await app.SeedAuthAsync();

            // seed Cadmus database (via Services/HostSeedExtension)
            await app.SeedAsync();

            // map controllers and Scalar API
            app.MapControllers();
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                options.WithTitle("Pythia API")
                       .WithPreferredScheme("Bearer");
            });

            Log.Information("Running API");
            await app.RunAsync();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Pythia host terminated unexpectedly");
            Debug.WriteLine(ex.ToString());
            Console.WriteLine(ex.ToString());
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
