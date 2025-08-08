using System.Drawing;
using System.Windows.Forms;

namespace TableMasterPOS
{
    public class ModernForm : Form
    {
        public static readonly Color PrimaryBackColor = Color.FromArgb(45, 45, 48);
        public static readonly Color SecondaryBackColor = Color.FromArgb(63, 63, 70);
        public static readonly Color PrimaryForeColor = Color.White;
        public static readonly Color AccentColor = Color.FromArgb(0, 122, 204);
        public static readonly Color AccentHoverColor = Color.FromArgb(28, 151, 234);
        private static readonly Font DefaultFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        public ModernForm()
        {
            BackColor = PrimaryBackColor;
            ForeColor = PrimaryForeColor;
            Font = DefaultFont;
            DoubleBuffered = true;
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            ApplyTheme(e.Control);
        }

        public static void ApplyTheme(Control control)
        {
            control.ForeColor = PrimaryForeColor;
            control.Font = DefaultFont;

            switch (control)
            {
                case Button btn:
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.BackColor = AccentColor;
                    btn.ForeColor = PrimaryForeColor;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseOverBackColor = AccentHoverColor;
                    btn.FlatAppearance.MouseDownBackColor = AccentColor;
                    btn.Cursor = Cursors.Hand;
                    break;
                case TextBoxBase textBox:
                    textBox.BackColor = SecondaryBackColor;
                    textBox.BorderStyle = BorderStyle.FixedSingle;
                    break;
                case ComboBox:
                case ListBox:
                case DateTimePicker:
                case NumericUpDown:
                    control.BackColor = SecondaryBackColor;
                    break;
                case Panel:
                case GroupBox:
                    control.BackColor = PrimaryBackColor;
                    break;
                default:
                    control.BackColor = PrimaryBackColor;
                    break;
            }

            foreach (Control child in control.Controls)
            {
                ApplyTheme(child);
            }
        }
    }
}
