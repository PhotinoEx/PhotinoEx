namespace PhotinoEx.Core.Models;

public struct RGBA
{
    public byte Red { get; set; }
    public byte Green { get; set; }
    public byte Blue { get; set; }
    public byte Alpha { get; set; }

    public static RGBA NewRGBA(byte red, byte green, byte blue, byte alpha)
    {
        return new RGBA
        {
            Red = red,
            Green = green,
            Blue = blue,
            Alpha = alpha,
        };
    }
}
