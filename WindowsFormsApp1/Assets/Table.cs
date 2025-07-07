using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;

public class Table
{
    [JsonProperty("Id")]
    public int Id { get; set; }

    [JsonProperty("Position")]
    public SerializablePoint PositionData { get; set; }

    [JsonProperty("Size")]
    public SerializableSize SizeData { get; set; }

    [JsonProperty("Rotation")]
    public float Rotation { get; set; }

    [JsonIgnore]
    public Point Position
    {
        get => PositionData?.ToPoint() ?? Point.Empty;
        set => PositionData = new SerializablePoint(value);
    }

    [JsonIgnore]
    public Size Size
    {
        get => SizeData?.ToSize() ?? Size.Empty;
        set => SizeData = new SerializableSize(value);
    }

    [JsonConstructor]
    public Table(int id, SerializablePoint positionData, SerializableSize sizeData, float rotation = 0)
    {
        Id = id;
        PositionData = positionData;
        SizeData = sizeData;
        Rotation = rotation;
    }

    public Table(int id, Point position, Size size, float rotation = 0)
        : this(id, new SerializablePoint(position), new SerializableSize(size), rotation) { }

    public Rectangle Bounds => new Rectangle(Position, Size);

    public bool IsCollidingWith(Table other)
    {
        if (this == other) return false;

        var a = this.GetRotatedCorners();
        var b = other.GetRotatedCorners();

        return IsPolygonsIntersecting(a, b);
    }

    public bool IsCollidingWithAny(List<Table> others, Table exclude = null)
    {
        foreach (var other in others)
        {
            if (other == exclude) continue;
            if (IsCollidingWith(other))
                return true;
        }
        return false;
    }

    public PointF[] GetRotatedCorners()
    {
        Point center = new Point(Position.X + Size.Width / 2, Position.Y + Size.Height / 2);
        float angleRad = Rotation * (float)(Math.PI / 180.0);

        PointF[] corners = new PointF[4];
        PointF[] originalCorners = new PointF[]
        {
            new(Position.X, Position.Y),
            new(Position.X + Size.Width, Position.Y),
            new(Position.X + Size.Width, Position.Y + Size.Height),
            new(Position.X, Position.Y + Size.Height)
        };

        for (int i = 0; i < 4; i++)
        {
            float dx = originalCorners[i].X - center.X;
            float dy = originalCorners[i].Y - center.Y;

            float rotatedX = center.X + dx * (float)Math.Cos(angleRad) - dy * (float)Math.Sin(angleRad);
            float rotatedY = center.Y + dx * (float)Math.Sin(angleRad) + dy * (float)Math.Cos(angleRad);

            corners[i] = new PointF(rotatedX, rotatedY);
        }

        return corners;
    }

    private static bool IsPolygonsIntersecting(PointF[] a, PointF[] b)
    {
        foreach (var polygon in new[] { a, b })
        {
            for (int i = 0; i < 4; i++)
            {
                PointF p1 = polygon[i];
                PointF p2 = polygon[(i + 1) % 4];

                float dx = p2.X - p1.X;
                float dy = p2.Y - p1.Y;
                PointF axis = new(-dy, dx);

                float minA = float.MaxValue, maxA = float.MinValue;
                foreach (var p in a)
                {
                    float proj = (p.X * axis.X + p.Y * axis.Y);
                    minA = Math.Min(minA, proj);
                    maxA = Math.Max(maxA, proj);
                }

                float minB = float.MaxValue, maxB = float.MinValue;
                foreach (var p in b)
                {
                    float proj = (p.X * axis.X + p.Y * axis.Y);
                    minB = Math.Min(minB, proj);
                    maxB = Math.Max(maxB, proj);
                }

                if (maxA < minB || maxB < minA)
                    return false;
            }
        }

        return true;
    }
}
