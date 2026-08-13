using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using PhotinoEx.Core;
using Microsoft.Extensions.Options;

namespace PhotinoEx.Blazor;

public class PhotinoExBlazorApp
{
    /// <summary>
    /// Gets configuration for the service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; } = null!;

    /// <summary>
    /// Gets configuration for the root components in the window.
    /// </summary>
    public BlazorWindowRootComponents RootComponents { get; private set; } = null!;

    internal void Initialize(IServiceProvider services, RootComponentList rootComponents)
    {
        Services = services;
        RootComponents = Services.GetRequiredService<BlazorWindowRootComponents>();
        MainWindow = Services.GetRequiredService<PhotinoExWindow>();
        WindowManager = Services.GetRequiredService<PhotinoExWebViewManager>();
        var config = Services.GetRequiredService<IOptions<PhotinoExBlazorAppConfiguration>>().Value;

        MainWindow.ApplicationId = config.ApplicationId;

        MainWindow
            .SetTitle("PhotinoEx.Blazor App")
            .SetUseOsDefaultSize(false)
            .SetWidth(1000)
            .SetHeight(900);

        MainWindow.RegisterCustomSchemeHandler(config.AppBaseUri.Scheme, HandleWebRequest);

        foreach (var component in rootComponents)
        {
            RootComponents.Add(component.Item1, component.Item2);
        }
    }

    public PhotinoExWindow MainWindow { get; private set; } = null!;

    public PhotinoExWebViewManager WindowManager { get; private set; } = null!;

    public void Run()
    {
        if (string.IsNullOrWhiteSpace(MainWindow.StartUrl))
        {
            MainWindow.StartUrl = "/";
        }

        WindowManager.Navigate(MainWindow.StartUrl);
        MainWindow.WaitForClose();
    }

    public Stream? HandleWebRequest(object? sender, string? scheme, string url, out string contentType)
    {
        return WindowManager.HandleWebRequest(sender, scheme, url, out contentType);
    }
}
