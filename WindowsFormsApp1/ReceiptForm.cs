using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

public class ReceiptForm : Form
{
    public ReceiptForm(Order order, Action onPayConfirmed)
    {
        Text = "Rachunek";
        Size = new Size(400, 500);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.White;

        var box = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 16, FontStyle.Regular),
            ScrollBars = ScrollBars.Vertical,
            Text = GenerateReceiptText(order),
            BackColor = Color.White,
            BorderStyle = BorderStyle.None
        };

        var btn = new Button
        {
            Text = "Zapłać",
            Dock = DockStyle.Bottom,
            Height = 50,
            Font = new Font("Segoe UI", 10, FontStyle.Regular),
            BackColor = SystemColors.Control,
            UseVisualStyleBackColor = true
        };
        btn.Click += (s, e) =>
        {
            onPayConfirmed?.Invoke();
            Close();
        };

        Controls.Add(box);
        Controls.Add(btn);
    }

    private string GenerateReceiptText(Order order)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🧾 Rachunek");
        sb.AppendLine($"Stolik: {order.TableId}");
        sb.AppendLine($"Godzina: {order.CreatedAt:HH:mm:ss}");
        sb.AppendLine();

        foreach (var item in order.Items)
            sb.AppendLine($"- {item.Name} x1 - {item.Price} zł");

        sb.AppendLine();
        sb.AppendLine($"💰 Do zapłaty: {order.TotalPrice} zł");

        return sb.ToString();
    }
}
