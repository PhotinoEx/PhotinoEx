using System;

namespace PhotinoEx.Blazor;

public class PhotinoExBlazorAppConfiguration
{
    public string ApplicationId { get; set; } = null!;

    public Uri AppBaseUri { get; set; } = null!;

    public string HostPage { get; set; } = string.Empty;
}
