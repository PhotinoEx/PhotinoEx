using System;
using System.IO;
using System.Net.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PhotinoEx.Core;

namespace PhotinoEx.Blazor;

public static class PhotinoExServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorDesktop(
        this IServiceCollection services,
        IFileProvider? fileProvider = null,
        string? applicationId = null)
    {
        var id = PhotinoExApplicationIdentity.Normalize(applicationId ?? PhotinoExApplicationIdentity.DefaultId);
        var appBaseUri = PhotinoExWebViewManager.CreateAppBaseUri(id);
        services
            .AddOptions<PhotinoExBlazorAppConfiguration>()
            .Configure(opts =>
            {
                opts.ApplicationId = id;
                opts.AppBaseUri = appBaseUri;
                opts.HostPage = "index.html";
            });

        return services
            .AddScoped(sp =>
            {
                var handler = sp.GetRequiredService<PhotinoExHttpHandler>();
                return new HttpClient(handler)
                {
                    BaseAddress = appBaseUri
                };
            })
            .AddSingleton(sp =>
            {
                var manager = sp.GetRequiredService<PhotinoExWebViewManager>();
                var store = sp.GetRequiredService<JSComponentConfigurationStore>();

                return new BlazorWindowRootComponents(manager, store);
            })
            .AddSingleton<Dispatcher, PhotinoExDispatcher>()
            .AddSingleton<IFileProvider>(_ =>
            {
                if (fileProvider is null)
                {
                    var root = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot");
                    return new PhysicalFileProvider(root);
                }
                else
                {
                    return fileProvider;
                }
            })
            .AddSingleton<JSComponentConfigurationStore>()
            .AddSingleton<PhotinoExBlazorApp>()
            .AddScoped<IPhotinoExClipboard, PhotinoExClipboard>()
            .AddScoped<IPhotinoExFileDragDrop, PhotinoExFileDragDrop>()
            .AddSingleton<PhotinoExHttpHandler>()
            .AddSingleton<PhotinoExSynchronizationContext>()
            .AddSingleton<PhotinoExWebViewManager>()
            .AddSingleton(new PhotinoExWindow())
            .AddBlazorWebView();
    }
}
