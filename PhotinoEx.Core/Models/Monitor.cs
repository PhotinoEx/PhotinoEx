using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct Monitor
{
    public MonitorRect MonitorArea { get; set; }
    public MonitorRect WorkArea { get; set; }
    public double Scale { get; set; }
}
