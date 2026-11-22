using PhotinoEx.Core;

namespace PhotinoEx.IntegrationTests;

internal static class NativeWindowHarness
{
    public static async Task RunAsync(Func<PhotinoExWindow, Task> test)
    {
        var ready = new TaskCompletionSource<PhotinoExWindow>(TaskCreationOptions.RunContinuationsAsynchronously);
        var stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            var window = new PhotinoExWindow();
            window.RegisterWebMessageReceivedHandler((_, message) =>
            {
                if (message == "ready")
                {
                    ready.TrySetResult(window);
                }
            });
            window.LoadRawString(
                "<html><body onload=\"window.external.sendMessage('ready')\">Clipboard integration test</body></html>"
            );
            try
            {
                window.WaitForClose();
                stopped.TrySetResult();
            }
            catch (Exception exception)
            {
                ready.TrySetException(exception);
                stopped.TrySetException(exception);
            }
        })
        {
            IsBackground = true,
            Name = "PhotinoEx integration window",
        };

        if (OperatingSystem.IsWindows())
        {
            thread.SetApartmentState(ApartmentState.STA);
        }

        thread.Start();
        var window = await ready.Task.WaitAsync(TimeSpan.FromSeconds(30));
        await Task.Delay(200);
        try
        {
            await test(window);
        }
        finally
        {
            window.Close();
            await stopped.Task.WaitAsync(TimeSpan.FromSeconds(15));
        }
    }
}
