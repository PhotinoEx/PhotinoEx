// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using PhotinoEx.Core;
using PhotinoEx.Core.Models;

namespace PhotinoEx.Blazor;

public class PhotinoExWebViewManager : WebViewManager
{
    private readonly PhotinoExWindow _exWindow;
    private readonly Channel<string> _channel;
    private readonly Uri _appBaseUri;
    private readonly EventHandler<WebMessageReceivedEventArgs> _webMessageReceivedHandler;
    private readonly Task _messagePumpTask;

    // On Windows, we can't use a custom scheme to host the initial HTML,
    // because webview2 won't let you do top-level navigation to such a URL.
    // On Linux/Mac, we must use a custom scheme, because their webviews
    // don't have a way to intercept http:// scheme requests.
    public static Uri CreateAppBaseUri(string applicationId)
    {
        var id = PhotinoExApplicationIdentity.Normalize(applicationId);
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new Uri($"http://{id}.localhost/")
            : new Uri($"app-{id}://localhost/");
    }

    public PhotinoExWebViewManager(PhotinoExWindow exWindow, IServiceProvider provider, Dispatcher dispatcher,
        IFileProvider fileProvider, JSComponentConfigurationStore jsComponents, IOptions<PhotinoExBlazorAppConfiguration> config)
        : base(provider, dispatcher, config.Value.AppBaseUri, fileProvider, jsComponents, config.Value.HostPage)
    {
        _exWindow = exWindow ?? throw new ArgumentNullException(nameof(exWindow));
        _appBaseUri = config.Value.AppBaseUri;
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        // Create a scheduler that uses one threads.
        var sts = new Utils.PhotinoExSynchronousTaskScheduler();

        _webMessageReceivedHandler = (sender, args) =>
        {
            if (args.Source is null || !IsSameOrigin(args.Source, _appBaseUri))
            {
                return;
            }

            // On some platforms, we need to move off the browser UI thread
            Task.Factory.StartNew(message =>
            {
                MessageReceived(args.Source, (string) message!);
            }, args.Message, CancellationToken.None, TaskCreationOptions.DenyChildAttach, sts);
        };
        _exWindow.WebMessageReceivedWithSource += _webMessageReceivedHandler;

        _messagePumpTask = Task.Run(MessagePump);
    }

    public Stream? HandleWebRequest(object? sender, string? schema, string url, out string contentType)
    {
        // It would be better if we were told whether or not this is a navigation request, but
        // since we're not, guess.
        var localPath = new Uri(url).LocalPath;
        var hasFileExtension = localPath.LastIndexOf('.') > localPath.LastIndexOf('/');

        //Remove parameters before attempting to retrieve the file. For example: http://localhost/_content/Blazorise/button.js?v=1.0.7.0
        if (url.Contains('?'))
        {
            url = url.Substring(0, url.IndexOf('?'));
        }

        if (url.StartsWith(_appBaseUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase)
            && TryGetResponseContent(url, !hasFileExtension, out var statusCode, out var statusMessage,
                out var content, out var headers))
        {
            contentType = headers.TryGetValue("Content-Type", out var value)
                ? value ?? "application/octet-stream"
                : "application/octet-stream";
            return content;
        }
        else
        {
            contentType = string.Empty;
            return null;
        }
    }

    protected override void NavigateCore(Uri absoluteUri)
    {
        _exWindow.Load(absoluteUri);
    }

    protected override void SendMessage(string message)
    {
        if (!_channel.Writer.TryWrite(message))
        {
            throw new ObjectDisposedException(nameof(PhotinoExWebViewManager));
        }
    }

    internal static bool IsSameOrigin(Uri source, Uri appBaseUri) =>
        source.Scheme.Equals(appBaseUri.Scheme, StringComparison.OrdinalIgnoreCase)
        && source.Host.Equals(appBaseUri.Host, StringComparison.OrdinalIgnoreCase)
        && source.Port == appBaseUri.Port;

    private async Task MessagePump()
    {
        await foreach (var message in _channel.Reader.ReadAllAsync())
        {
            await _exWindow.SendWebMessageAsync(message);
        }
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _exWindow.WebMessageReceivedWithSource -= _webMessageReceivedHandler;
        _channel.Writer.TryComplete();
        await _messagePumpTask.ConfigureAwait(false);
        await base.DisposeAsyncCore();
    }
}
