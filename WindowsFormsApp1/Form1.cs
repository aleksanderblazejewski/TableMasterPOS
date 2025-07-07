// Form1.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.PropertyGridInternal;
using TableMasterPOS;
using TableMasterPOS.Properties;
using WindowsFormsApp1.Managers;
using Microsoft.VisualBasic;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private enum AppMode { Work, Edit }
        private AppMode currentMode = AppMode.Work;

        private Panel layoutPanel;
        private Panel infoPanel;
        private Panel loadingPanel;

        private List<Table> tables = new List<Table>();
        private List<TableGroup> tableGroups = new();
        private Table selectedTable;

        private Button btnWorkMode, btnEditMode, btnMenu, btnWorkers, btnTables, btnReport, btnSettings, btnExit;
        private EditModeManager editManager;
        private WorkModeManager workManager;
        private System.Timers.Timer syncTimer;

        private const string SaveFilePath = "tables.json";
        const string ApiBaseUrl = "https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net";
        private int nextId = 1;

        public Form1()
        {

            Load += async (s, e) =>
            {
                InitializeUI();

                Enabled = false;
                
                await LoadTables();
                await LoadTableGroups();

                workManager.UpdateGroups(tableGroups);
                SetAppMode(AppMode.Work);

                if (VerifyPassword("Podaj hasło, aby uruchomić aplikację"))
                {
                    StartSyncTimer();

                    loadingPanel.Visible = false;
                    Enabled = true;
                } else
                {
                    Application.Exit();
                }
            };

        }



        private void InitializeUI()
        {
            Screen actual_screen = Screen.FromControl(this);
            FormBorderStyle = FormBorderStyle.None;
            WindowState = FormWindowState.Maximized;

            loadingPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = true
            };

            var logo = new PictureBox
            {
                Image = Resources.Logo,
                SizeMode = PictureBoxSizeMode.Zoom,
                Size = new Size(300, 300),
                Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - 300) / 2, (Screen.PrimaryScreen.WorkingArea.Height - 300) / 2)
            };

            var loadingText = new Label
            {
                Text = "Ładowanie...",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                AutoSize = true,
                Location = new Point((Screen.PrimaryScreen.WorkingArea.Width - 200) / 2, (Screen.PrimaryScreen.WorkingArea.Height - 300) / 2 + 320)
            };

            loadingPanel.Controls.Add(logo);
            loadingPanel.Controls.Add(loadingText);
            this.Controls.Add(loadingPanel);
            loadingPanel.BringToFront();

            this.BackColor = Color.White;

            btnWorkMode = new Button
            {
                Text = "Tryb pracy",
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat,
                Enabled = false
            };
            btnWorkMode.Click += (s, e) => SetAppMode(AppMode.Work);

            btnEditMode = new Button
            {
                Text = "Tryb edycji",
                Location = new Point(120, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnEditMode.Click += (s, e) => SetAppMode(AppMode.Edit);
            btnMenu = new Button
            {
                Text = "Menu",
                Location = new Point(actual_screen.WorkingArea.Width - 315, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnMenu.Click += (s, e) =>
            {
                if (VerifyPassword("Podaj hasło, aby przejść do edycji menu."))
                {
                    var menuForm = new FormMenuEditor();
                    menuForm.Size = new Size(600, 770);
                    menuForm.BringToFront();
                    menuForm.TopMost = true;
                    menuForm.StartPosition = FormStartPosition.CenterScreen;
                    menuForm.ShowDialog();
                }
            };
            btnWorkers = new Button
            {
                Text = "Pracownicy",
                Location = new Point(actual_screen.WorkingArea.Width - 420, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnWorkers.Click += (s, e) =>
            {
                if (VerifyPassword("Podaj hasło, aby przejść do edycji pracowników."))
                {
                    var workersForm = new FormWorkersEditor();
                    workersForm.Size = new Size(600, 670);
                    workersForm.BringToFront();
                    workersForm.TopMost = true;
                    workersForm.StartPosition = FormStartPosition.CenterScreen;
                    workersForm.ShowDialog();
                }
            };
            btnReport = new Button
            {
                Text = "Raporty",
                Location = new Point(actual_screen.WorkingArea.Width - 630, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnReport.Click += async (s, e) =>
            {
                var salesReportForm = new SalesReportForm();
                salesReportForm.ShowDialog();
            };
            btnTables = new Button
            {
                Text = "Stoliki",
                Location = new Point(actual_screen.WorkingArea.Width - 525, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnTables.Click += async (s, e) =>
            {
                if (VerifyPassword("Podaj hasło, aby przejść do edycji grup stolików."))
                {
                    var tablesForm = new FormTablesEditor(tables);
                    tablesForm.Size = new Size(600, 770);
                    tablesForm.BringToFront();
                    tablesForm.TopMost = true;
                    tablesForm.StartPosition = FormStartPosition.CenterScreen;
                    tablesForm.ShowDialog();

                    await LoadTableGroups();
                }
            };
            btnSettings = new Button
            {
                Text = "Ustawienia", // Zmieniono tekst
                Location = new Point(actual_screen.WorkingArea.Width - 210, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnSettings.Click += (s, e) =>
            {
                var settingsForm = new SettingsForm();
                settingsForm.ShowDialog();
            };

            btnExit = new Button
            {
                Text = "Zakończ",
                Location = new Point(actual_screen.WorkingArea.Width - 105, 10),
                Size = new Size(100, 30),
                FlatStyle = FlatStyle.Flat
            };
            btnExit.Click += async (s, e) =>
            {
                var result = MessageBox.Show(
                    "Czy na pewno chcesz zakończyć aplikację?",
                    "Potwierdzenie",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.Yes)
                {
                    await SaveTables();
                    Application.Exit();
                }
            };

            layoutPanel = new DoubleBufferedPanel
            {
                Location = new Point(10, 50),
                Size = new Size(actual_screen.WorkingArea.Width - 300, actual_screen.WorkingArea.Height - 60),
                BackColor = Color.LightGray
            };
            layoutPanel.MouseDown += LayoutPanel_MouseDown;
            layoutPanel.MouseMove += LayoutPanel_MouseMove;
            layoutPanel.MouseUp += LayoutPanel_MouseUp;
            layoutPanel.Paint += LayoutPanel_Paint;

            infoPanel = new Panel
            {
                Location = new Point(actual_screen.WorkingArea.Width - 275, 50),
                Size = new Size(250, actual_screen.WorkingArea.Height - 60),
                BackColor = Color.LightGray,
                Visible = true
            };

            this.Controls.AddRange(new Control[]
            {
                btnWorkMode, btnEditMode, btnSettings, btnExit,
                layoutPanel, infoPanel, btnMenu,btnWorkers,btnReport, btnTables
            });
            infoPanel.Invalidate();
            this.FormClosing += Form1_FormClosing;


            editManager = new EditModeManager(
                tables,
                layoutPanel,
                infoPanel,
                this,
                () => selectedTable,
                t => selectedTable = t,
                () => nextId++
            );
            workManager = new WorkModeManager(tables, layoutPanel, infoPanel, selectedTable);



        }

        private void LayoutPanel_Paint(object sender, PaintEventArgs e)
        {
            if (currentMode == AppMode.Edit)
            {
                editManager.Draw(e.Graphics);
            }
            else
            {
                workManager.Draw(e.Graphics);
            }

        }

        private void SetAppMode(AppMode mode)
        {

            if (mode == AppMode.Edit)
            {
                if (VerifyPassword("Podaj hasło, aby przejść do trybu Edycji"))
                {
                    currentMode = mode;
                    btnEditMode.Enabled = false;
                    btnWorkMode.Enabled = true;
                    editManager.Enable();
                    workManager.Disable();
                }
            }
            else
            {
                currentMode = mode;
                btnEditMode.Enabled = true;
                btnWorkMode.Enabled = false;
                workManager.Enable();
                editManager.Disable();
            }

            layoutPanel.Invalidate();
        }

        private async void LayoutPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentMode == AppMode.Edit)
                await editManager.HandleMouseDownAsync(e);
            else
                workManager.HandleMouseDown(e);
        }

        private void LayoutPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentMode == AppMode.Edit)
                editManager.HandleMouseMove(e);
        }

        private void LayoutPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (currentMode == AppMode.Edit)
                editManager.HandleMouseUpAsync(e);
        }

        private async Task LoadTables()
        {
            using HttpClient client = new();

            try
            {
                var json = await client.GetStringAsync($"{ApiBaseUrl}/tables");
                var loaded = JsonConvert.DeserializeObject<List<Table>>(json);

                if (loaded != null)
                {
                    tables.Clear();
                    tables.AddRange(loaded);

                    nextId = (tables.Count > 0) ? tables.Max(t => t.Id) + 1 : 1;
                    File.WriteAllText(SaveFilePath, json); // cache lokalny
                }
            }
            catch (Exception)
            {
                // Fallback: z pliku
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    var loaded = JsonConvert.DeserializeObject<List<Table>>(json);
                    if (loaded != null)
                    {
                        tables.Clear();
                        tables.AddRange(loaded);
                    }
                }
                else
                {
                    MessageBox.Show("Nie udało się załadować stolików z API ani z pliku.");
                }
            }
        }

        private async Task SaveTables()
        {
            string json = JsonConvert.SerializeObject(tables, Formatting.Indented);
            File.WriteAllText(SaveFilePath, json);

            using HttpClient client = new();

            try
            {
                var deleteResponse = await client.DeleteAsync($"{ApiBaseUrl}/tables");
                if (!deleteResponse.IsSuccessStatusCode)
                {
                    await Task.Delay(3000); // poczekaj chwilę i spróbuj jeszcze raz
                    deleteResponse = await client.DeleteAsync($"{ApiBaseUrl}/tables");
                }

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    MessageBox.Show("Błąd czyszczenia API: " + deleteResponse.StatusCode);
                    return;
                }

                foreach (var table in tables)
                {
                    var content = new StringContent(JsonConvert.SerializeObject(table), Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"{ApiBaseUrl}/tables", content);
                    string responseText = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Błąd wysyłki stolika ID {table.Id}: {response.StatusCode}\n{responseText}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd zapisu do API: " + ex.Message);
            }
        }

        private void StartSyncTimer()
        {
            syncTimer = new System.Timers.Timer(5000);
            syncTimer.Elapsed += async (s, e) => await SyncWithApi();
            syncTimer.AutoReset = true;
            syncTimer.Start();
        }
        private async Task SyncWithApi()
        {
            using HttpClient client = new();

            try
            {
                // --- Pobierz zamówienia ---
                var ordersJson = await client.GetStringAsync($"{ApiBaseUrl}/orders");
                var updatedOrders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);

                layoutPanel.Invoke(() =>
                {
                    workManager.UpdateOrders(updatedOrders);
                });

                // --- Pobierz kelnerów ---
                var waitersJson = await client.GetStringAsync($"{ApiBaseUrl}/waiters");
                var waiters = JsonConvert.DeserializeObject<List<Worker>>(waitersJson);

                // --- Pobierz grupy stolików ---
                var groupsJson = await client.GetStringAsync($"{ApiBaseUrl}/tablegroups");
                var groups = JsonConvert.DeserializeObject<List<TableGroup>>(groupsJson);

                layoutPanel.Invoke(() =>
                {
                    workManager.UpdateWaiters(waiters);
                    workManager.UpdateGroups(groups);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd podczas synchronizacji z API:\n" + ex.Message);
            }
        }


        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true; // Zatrzymaj zamykanie

            await SaveTables();

            // Kontynuuj zamykanie po zapisaniu
            this.FormClosing -= Form1_FormClosing; // Odłącz handler, żeby nie wpadł w pętlę
            this.Close(); // Wywołaj zamknięcie ponownie
        }
        private async Task LoadTableGroups()
        {
            using HttpClient client = new();
            var json = await client.GetStringAsync($"{ApiBaseUrl}/tablegroups");
            var groups = JsonConvert.DeserializeObject<List<TableGroup>>(json) ?? new();

            tableGroups.Clear();                // <- aktualizujemy pole klasy Form1
            tableGroups.AddRange(groups);      // <- dodajemy grupy

            workManager.UpdateGroups(tableGroups); // <- przekazujemy do managera
        }

        private bool VerifyPassword(string prompt)
        {
            string configPath = "settings.json";

            if (!File.Exists(configPath))
            {
                MessageBox.Show("Brakuje pliku settings.json.");
                return false;
            }

            var json = File.ReadAllText(configPath);
            var settings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (!settings.TryGetValue("EditModePassword", out string correctPassword))
            {
                MessageBox.Show("Brakuje pola EditModePassword w pliku.");
                return false;
            }

            using var form = new PasswordPromptForm(prompt);
            if (form.ShowDialog() == DialogResult.OK)
            {
                if (form.EnteredPassword == correctPassword)
                    return true;
                else
                    MessageBox.Show("Nieprawidłowe hasło.");
            }

            return false;
        }

    }
}
