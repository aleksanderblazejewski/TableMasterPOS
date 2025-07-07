using System;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class PasswordPromptForm : Form
    {
        public string EnteredPassword => txtPassword.Text;

        private TextBox txtPassword;
        private Button btnOk;
        private Button btnCancel;

        public PasswordPromptForm(string promptText)
        {

            Text = "Wprowadź hasło";
            Size = new System.Drawing.Size(400, 150);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;

            Label lblPrompt = new Label()
            {
                Text = promptText,
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };

            txtPassword = new TextBox()
            {
                Location = new System.Drawing.Point(20, 45),
                Width = 340,
                UseSystemPasswordChar = true
            };

            btnOk = new Button()
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(200, 80)
            };

            btnCancel = new Button()
            {
                Text = "Anuluj",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(280, 80)
            };

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            Controls.Add(lblPrompt);
            Controls.Add(txtPassword);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);
        }
    }
}
