﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Pythia.Api.Services;

/// <summary>
/// Database initializer extension to <see cref="IHost"/>.
/// See https://stackoverflow.com/questions/45148389/how-to-seed-in-entity-framework-core-2.
/// </summary>
public static class HostSeedExtensions
{
    private static Task SeedAsync(IServiceProvider serviceProvider)
    {
        ApplicationDatabaseInitializer initializer =
            new(serviceProvider);

        return Policy.Handle<DbException>()
            .WaitAndRetry(
            [
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            ], (exception, timeSpan, _) =>
            {
                ILogger? logger = serviceProvider
                    .GetService<ILoggerFactory>()!
                    .CreateLogger(typeof(HostSeedExtensions));

                string message = "Unable to connect to DB" +
                    $" (sleep {timeSpan}): {exception.Message}";
                Console.WriteLine(message);
                logger.LogError(exception, message);
            }).Execute(async () =>
            {
                await initializer.SeedAsync();
            });
    }

    /// <summary>
    /// Seeds the database.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <returns>The received host, to allow concatenation.</returns>
    /// <exception cref="ArgumentNullException">serviceProvider</exception>
    public static async Task<IHost> SeedAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        IServiceProvider serviceProvider = scope.ServiceProvider;
        ILogger? logger = serviceProvider
            .GetService<ILoggerFactory>()!
            .CreateLogger(typeof(HostSeedExtensions));

        try
        {
            IConfiguration config = serviceProvider.GetService<IConfiguration>()!;

            int delay = config.GetValue("SeedDelay", 0);
            if (delay > 0)
            {
                logger.LogInformation("Waiting {Delay} seconds...", delay);
                Thread.Sleep(delay * 1000);
            }

            logger.LogInformation("Seeding database...");
            await SeedAsync(serviceProvider);
            logger.LogInformation("Seeding completed.");

            return host;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database: {Error}", ex.Message);
            throw;
        }
    }
}
