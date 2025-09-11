using System;
using System.Windows.Forms;
using TableMasterPOS;

namespace WindowsFormsApp1
{
    public partial class PasswordPromptForm : ModernForm
    {
        public string EnteredPassword => txtPassword.Text;

        private TextBox txtPassword;
        private Button btnOk;
        private Button btnCancel;

        public PasswordPromptForm(string promptText)
        {

            Text = "Wprowadź hasło";
            Size = new System.Drawing.Size(400, 170);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;

            Label lblPrompt = new Label
            {
                Text = promptText,
                AutoSize = true,
                Dock = DockStyle.Fill
            };

            txtPassword = new TextBox
            {
                Dock = DockStyle.Fill,
                UseSystemPasswordChar = true
            };

            btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Width = 100,
                Height = 35
            };

            btnCancel = new Button
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Width = 100,
                Height = 35
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            var buttonsPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                Dock = DockStyle.Fill,
                AutoSize = true
            };
            buttonsPanel.Controls.Add(btnCancel);
            buttonsPanel.Controls.Add(btnOk);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(20)
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            layout.Controls.Add(lblPrompt, 0, 0);
            layout.Controls.Add(txtPassword, 0, 1);
            layout.Controls.Add(buttonsPanel, 0, 2);

            Controls.Add(layout);
        }
    }
}
