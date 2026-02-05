using System.Runtime.InteropServices;

namespace PhotinoEx.Core.Models;

[StructLayout(LayoutKind.Sequential)]
public struct Monitor
{
    public MonitorRect MonitorArea { get; set; }
    public MonitorRect WorkArea { get; set; }
    public double Scale { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Monitor"/> struct.
    /// </summary>
    /// <param name="monitor">The area of monitor.</param>
    /// <param name="work">The working area of the monitor.</param>
    public Monitor(MonitorRect monitor, MonitorRect work, double scale)
    {
        MonitorArea = monitor;
        WorkArea = work;
        Scale = scale;
    }
}
