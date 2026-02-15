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
        EmbedProvider = new ManifestEmbeddedFileProvider(typeof(Program).Assembly, "wwwroot");
        var appBuilder = PhotinoBlazorAppBuilder.CreateDefault(EmbedProvider, args);

        appBuilder.RootComponents.Add<App>("app");

        App = appBuilder.Build();

        App.MainWindow.SetDevToolsEnabled(true);

        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
        {
            App.MainWindow.ShowMessageDialog("Fatal exception", error.ExceptionObject.ToString() ?? "");
        };

        App.MainWindow.WebMessageReceived += (sender, s) =>
        {
            Console.WriteLine(s);
        };

        App.Run();
    }
}
