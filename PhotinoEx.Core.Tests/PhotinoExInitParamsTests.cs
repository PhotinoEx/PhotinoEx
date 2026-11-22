using PhotinoEx.Core.Models;
using Xunit;

namespace PhotinoEx.Core.Tests;

public sealed class PhotinoExInitParamsTests
{
    [Fact]
    public void MissingInitialContentIsRejected()
    {
        var parameters = new PhotinoExInitParams();

        var errors = parameters.GetParamErrors();

        Assert.Contains(errors, error => error.Contains("initial URL or HTML string"));
    }

    [Theory]
    [InlineData("https://example.test", "")]
    [InlineData("", "<html></html>")]
    public void EitherInitialContentSourceIsAccepted(string url, string html)
    {
        var parameters = new PhotinoExInitParams { StartUrl = url, StartString = html };

        Assert.Empty(parameters.GetParamErrors());
    }

    [Fact]
    public void MaximizedAndMinimizedAreMutuallyExclusive()
    {
        var parameters = ValidParameters();
        parameters.Maximized = true;
        parameters.Minimized = true;

        var errors = parameters.GetParamErrors();

        Assert.Contains(errors, error => error.Contains("both maximized and minimized"));
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void FullScreenCannotBeCombinedWithWindowState(bool maximized, bool minimized)
    {
        var parameters = ValidParameters();
        parameters.FullScreen = true;
        parameters.Maximized = maximized;
        parameters.Minimized = minimized;

        var errors = parameters.GetParamErrors();

        Assert.Contains(errors, error => error.Contains("FullScreen cannot be combined"));
    }

    [Fact]
    public void ValidationResetsInteropSize()
    {
        var parameters = ValidParameters();
        parameters.Size = 123;

        parameters.GetParamErrors();

        Assert.Equal(0, parameters.Size);
    }

    private static PhotinoExInitParams ValidParameters() => new() { StartString = "<html></html>" };
}
