using System.Drawing;
using System.Windows.Forms;

namespace TableMasterPOS
{
    public class ModernForm : Form
    {
        public static readonly Color PrimaryBackColor = Color.FromArgb(45, 45, 48);
        public static readonly Color SecondaryBackColor = Color.FromArgb(63, 63, 70);
        public static readonly Color PrimaryForeColor = Color.White;
        private static readonly Font DefaultFont = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);

        public ModernForm()
        {
            BackColor = PrimaryBackColor;
            ForeColor = PrimaryForeColor;
            Font = DefaultFont;
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
                    btn.BackColor = SecondaryBackColor;
                    btn.ForeColor = PrimaryForeColor;
                    break;
                case TextBox:
                case ComboBox:
                case ListBox:
                case DateTimePicker:
                    control.BackColor = SecondaryBackColor;
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
