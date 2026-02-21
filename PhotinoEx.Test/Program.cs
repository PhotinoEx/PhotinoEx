using Microsoft.Extensions.FileProviders;
using PhotinoEx.Blazor;
using PhotinoEx.Core;

namespace PhotinoEx.Test;

class Program
{
    private static ManifestEmbeddedFileProvider? EmbedProvider { get; set; }

    public static PhotinoBlazorApp? App { get; set; }

    [STAThread]
    private static void Main(string[] args)
    {
        // EmbedProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
        // var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(EmbedProvider, args);
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(args);

        appBuilder.RootComponents.Add<App>("app");

        App = appBuilder.Build();

        App.MainWindow.SetDevToolsEnabled(true);
        if (PhotinoWindow.IsWindowsPlatform)
        {
            App.MainWindow.SetIconFile("C:\\Users\\craig\\Desktop\\Repos\\PhotinoEx\\PhotinoEx.Test\\wwwroot\\Icon_PhotinoEx.ico");
        }

        if (PhotinoWindow.IsLinuxPlatform)
        {
            App.MainWindow.SetIconFile("/home/cwx/Repos/PhotinoEx/PhotinoEx.Test/wwwroot/hicolor/48x48/apps/Icon_PhotinoEx.png");
        }

        App.MainWindow.SetHeight(300);
        App.MainWindow.SetWidth(300);
        App.MainWindow.SetMinWidth(200);
        App.MainWindow.SetMinHeight(200);
        App.MainWindow.SetMaxHeight(400);
        App.MainWindow.SetMaxWidth(400);
        App.MainWindow.SetUseOsDefaultSize(false);
        App.MainWindow.SetDarkmode(true);
        App.MainWindow.SetTitle("PhotinoEx Test Application");

        AppDomain.CurrentDomain.UnhandledException += async (_, error) =>
        {
            await App.MainWindow.ShowMessageDialogAsync("Fatal exception", error.ExceptionObject.ToString() ?? "");
        };

        App.Run();
    }
}
