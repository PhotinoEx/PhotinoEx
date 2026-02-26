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
    public static IServiceCollection AddBlazorDesktop(this IServiceCollection services, IFileProvider? fileProvider = null)
    {
        services
            .AddOptions<PhotinoExBlazorAppConfiguration>()
            .Configure(opts =>
            {
                opts.AppBaseUri = new Uri(PhotinoExWebViewManager.AppBaseUri);
                opts.HostPage = "index.html";
            });

        return services
            .AddScoped(sp =>
            {
                var handler = sp.GetService<PhotinoExHttpHandler>();
                return new HttpClient(handler)
                {
                    BaseAddress = new Uri(PhotinoExWebViewManager.AppBaseUri)
                };
            })
            .AddSingleton(sp =>
            {
                var manager = sp.GetService<PhotinoExWebViewManager>();
                var store = sp.GetService<JSComponentConfigurationStore>();

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
            .AddSingleton<PhotinoExHttpHandler>()
            .AddSingleton<PhotinoExSynchronizationContext>()
            .AddSingleton<PhotinoExWebViewManager>()
            .AddSingleton(new PhotinoExWindow())
            .AddBlazorWebView();
    }
}
