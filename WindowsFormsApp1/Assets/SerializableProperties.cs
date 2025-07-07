using System.Drawing;
using Newtonsoft.Json;

public class SerializablePoint
{
    [JsonProperty("X")]
    public int X { get; set; }

    [JsonProperty("Y")]
    public int Y { get; set; }

    public SerializablePoint() { }

    public SerializablePoint(Point point)
    {
        X = point.X;
        Y = point.Y;
    }

    public Point ToPoint() => new Point(X, Y);
}

public class SerializableSize
{
    [JsonProperty("Width")]
    public int Width { get; set; }

    [JsonProperty("Height")]
    public int Height { get; set; }

    public SerializableSize() { }

    public SerializableSize(Size size)
    {
        Width = size.Width;
        Height = size.Height;
    }

    public Size ToSize() => new Size(Width, Height);
}
