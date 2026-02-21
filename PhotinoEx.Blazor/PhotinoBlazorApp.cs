using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using PhotinoEx.Core;

namespace PhotinoEx.Blazor;

public class PhotinoBlazorApp
{
    /// <summary>
    /// Gets configuration for the service provider.
    /// </summary>
    public IServiceProvider Services { get; private set; }

    /// <summary>
    /// Gets configuration for the root components in the window.
    /// </summary>
    public BlazorWindowRootComponents RootComponents { get; private set; }

    internal void Initialize(IServiceProvider services, RootComponentList rootComponents)
    {
        Services = services;
        RootComponents = Services.GetService<BlazorWindowRootComponents>();
        MainWindow = Services.GetService<PhotinoWindow>();
        WindowManager = Services.GetService<PhotinoWebViewManager>();

        MainWindow
            .SetTitle("PhotinoEx.Blazor App")
            .SetUseOsDefaultSize(false)
            .SetWidth(1000)
            .SetHeight(900);

        MainWindow.RegisterCustomSchemeHandler(PhotinoWebViewManager.BlazorAppScheme, HandleWebRequest);

        foreach (var component in rootComponents)
        {
            RootComponents.Add(component.Item1, component.Item2);
        }
    }

    public PhotinoWindow MainWindow { get; private set; }

    public PhotinoWebViewManager WindowManager { get; private set; }

    public void Run()
    {
        if (string.IsNullOrWhiteSpace(MainWindow.StartUrl))
        {
            MainWindow.StartUrl = "/";
        }

        WindowManager.Navigate(MainWindow.StartUrl);
        MainWindow.WaitForClose();
    }

    public Stream HandleWebRequest(object sender, string scheme, string url, out string contentType)
    {
        return WindowManager.HandleWebRequest(sender, scheme, url, out contentType!)!;
    }
}
