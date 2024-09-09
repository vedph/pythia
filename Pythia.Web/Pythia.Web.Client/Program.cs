using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

namespace Pythia.Web.Client;

internal static class Program
{
    static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddScoped<DialogService>();
        builder.Services.AddScoped<NotificationService>();
        builder.Services.AddScoped<TooltipService>();
        builder.Services.AddScoped<ContextMenuService>();

        builder.Services.AddRadzenComponents();

        await builder.Build().RunAsync();
    }
}
