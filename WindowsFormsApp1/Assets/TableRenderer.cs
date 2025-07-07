using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

public static class TableRenderer
{
    public static void DrawTables(Graphics g, List<Table> tables, Table selected)
    {
        foreach (var table in tables)
        {
            var rect = new Rectangle(table.Position, table.Size);
            var center = new PointF(
                table.Position.X + table.Size.Width / 2f,
                table.Position.Y + table.Size.Height / 2f
            );

            g.TranslateTransform(center.X, center.Y);
            g.RotateTransform(table.Rotation);

            // Rysowanie w układzie obróconym
            var drawRect = new RectangleF(
                -table.Size.Width / 2f,
                -table.Size.Height / 2f,
                table.Size.Width,
                table.Size.Height
            );

            using (Brush brush = new SolidBrush(Color.LightBlue))
                g.FillRectangle(brush, drawRect);

            using (Pen pen = new Pen(table == selected ? Color.Red : Color.Black, 2))
                g.DrawRectangle(pen, drawRect.X, drawRect.Y, drawRect.Width, drawRect.Height);

            var idStr = table.Id.ToString();
            var strSize = g.MeasureString(idStr, SystemFonts.DefaultFont);
            var textPosition = new PointF(center.X - strSize.Width / 2f, center.Y - strSize.Height / 2f);

            g.ResetTransform(); // Cofnij rotację i translację

            g.DrawString(idStr, SystemFonts.DefaultFont, Brushes.Black, textPosition);

        }
    }

}
