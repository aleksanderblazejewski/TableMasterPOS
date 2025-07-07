using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace WindowsFormsApp1
{
    public partial class SettingsForm : Form
    {
        private TextBox txtNewPassword;
        private Button btnSave;

        public SettingsForm()
        {

            Text = "Ustawienia";
            Size = new System.Drawing.Size(400, 200);

            Label lblNewPassword = new Label()
            {
                Text = "Nowe hasło do trybu edycji:",
                Location = new System.Drawing.Point(20, 20),
                AutoSize = true
            };

            txtNewPassword = new TextBox()
            {
                Location = new System.Drawing.Point(20, 50),
                Width = 300,
                UseSystemPasswordChar = true // <-- to dodaj
            };

            btnSave = new Button()
            {
                Text = "Zapisz",
                Location = new System.Drawing.Point(20, 90)
            };
            btnSave.Click += BtnSave_Click;

            Controls.Add(lblNewPassword);
            Controls.Add(txtNewPassword);
            Controls.Add(btnSave);
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string configPath = "settings.json";

            if (!File.Exists(configPath))
            {
                MessageBox.Show("Brakuje pliku settings.json.");
                return;
            }

            var json = File.ReadAllText(configPath);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (!settings.TryGetValue("EditModePassword", out string currentPassword))
            {
                MessageBox.Show("Brakuje pola EditModePassword w pliku.");
                return;
            }

            string input = Interaction.InputBox("Podaj aktualne hasło:");
            if (string.IsNullOrWhiteSpace(input)) return;

            if (input != currentPassword)
            {
                MessageBox.Show("Nieprawidłowe hasło.");
                return;
            }

            settings["EditModePassword"] = txtNewPassword.Text;
            File.WriteAllText(configPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
            MessageBox.Show("Hasło zostało zmienione.");
            Close();
        }
    }
}
