using Microsoft.Extensions.FileProviders;
using PhotinoEx.Blazor;

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
        // App.MainWindow.SetIconFile("C:\\Users\\craig\\Desktop\\Repos\\PhotinoEx\\PhotinoEx.Test\\wwwroot\\Icon_PhotinoEx.ico");
        App.MainWindow.SetHeight(300);
        App.MainWindow.SetWidth(300);
        App.MainWindow.SetMinWidth(200);
        App.MainWindow.SetMinHeight(200);
        App.MainWindow.SetMaxHeight(400);
        App.MainWindow.SetMaxWidth(400);
        App.MainWindow.SetUseOsDefaultSize(false);

        AppDomain.CurrentDomain.UnhandledException += async (_, error) =>
        {
            await App.MainWindow.ShowMessageDialogAsync("Fatal exception", error.ExceptionObject.ToString() ?? "");
        };

        App.Run();
    }
}
