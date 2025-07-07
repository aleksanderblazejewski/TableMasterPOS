// Refaktoryzacja WorkModeManager.cs
// Zakładamy, że usunięto GroupId z klasy Table i jeden stolik może należeć do wielu grup

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

namespace WindowsFormsApp1.Managers
{
    public class WorkModeManager
    {
        private List<Table> tables;
        private List<Order> orders = new();
        private List<Worker> waiters = new();
        private List<TableGroup> tableGroups = new();
        private Button payButton;
        const string ApiBaseUrl = "https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net";
        private System.Timers.Timer syncSelectedTableTimer;

        public void UpdateWaiters(List<Worker> allWaiters) => waiters = allWaiters;
        public void UpdateGroups(List<TableGroup> groups) => tableGroups = groups;

        private Panel layoutPanel;
        private Panel infoPanel;

        private Table selectedTable;
        public Table SelectedTable => selectedTable;

        private TextBox tableInfoBox;

        public WorkModeManager(List<Table> tables, Panel layoutPanel, Panel infoPanel, Table selectedTable)
        {
            this.tables = tables;
            this.layoutPanel = layoutPanel;
            this.infoPanel = infoPanel;
            this.selectedTable = selectedTable;

            InitializeControls();
            StartSelectedTableSyncTimer();
        }

        private void InitializeControls()
        {
            infoPanel.Controls.Clear();

            // Przycisk "Rozlicz"
            payButton = new Button()
            {
                Text = "Rozlicz",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Size = new Size(infoPanel.Width - 20, 40),
                Location = new Point(10, infoPanel.Height - 50),
                Visible = false
            };
            payButton.Click += PayButton_Click;

            // Box z informacjami
            tableInfoBox = new TextBox()
            {
                Multiline = true,
                ReadOnly = true,
                Location = new Point(10, 10),
                Size = new Size(infoPanel.Width - 20, infoPanel.Height - 70), // <-- skrócone o wysokość przycisku + odstęp
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                BackColor = infoPanel.BackColor,
                ScrollBars = ScrollBars.Vertical
            };

            infoPanel.Controls.Add(tableInfoBox);
            infoPanel.Controls.Add(payButton);
            infoPanel.Visible = true;
        }

        public void HandleMouseDown(MouseEventArgs e)
        {
            foreach (var table in tables)
            {
                var rect = new Rectangle(table.Position, table.Size);
                if (rect.Contains(e.Location))
                {
                    selectedTable = table;
                    UpdateInfoPanel();
                    layoutPanel.Invalidate();
                    return;
                }
            }

            selectedTable = null;
            UpdateInfoPanel();
            layoutPanel.Invalidate();
        }

        private void UpdateInfoPanel()
        {
            if (selectedTable != null)
            {
                var sb = new StringBuilder();

                sb.AppendLine($"🪑 Stolik ID: {selectedTable.Id}");
                sb.AppendLine($"📍 Pozycja: {selectedTable.Position.X}, {selectedTable.Position.Y}");
                sb.AppendLine($"📐 Rozmiar: {selectedTable.Size.Width}x{selectedTable.Size.Height}");
                sb.AppendLine($"🔄 Rotacja: {selectedTable.Rotation}°");

                var groupsForTable = tableGroups.Where(g => g.AssignedTableIds.Contains(selectedTable.Id)).ToList();

                if (groupsForTable.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("👥 Grupy przypisane do stolika:");
                    foreach (var group in groupsForTable)
                    {
                        sb.AppendLine($"- {group.Name} (ID: {group.Id})");
                        var assignedWaiters = waiters
                            .Where(w => group.AssignedWaiterIds.Contains(w.Id))
                            .Select(w => $"{w.FirstName} {w.LastName}");

                        if (assignedWaiters.Any())
                        {
                            sb.AppendLine("   Kelnerzy:");
                            foreach (var waiter in assignedWaiters)
                            {
                                sb.AppendLine($"     • {waiter}");
                            }
                        }
                    }
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("❌ Brak przypisanej grupy.");
                }

                var order = orders.FindLast(o => o.TableId == selectedTable.Id && !o.IsPaid);

                if (order != null)
                {
                    sb.AppendLine();
                    sb.AppendLine("🧾 Aktualne zamówienie:");
                    foreach (var item in order.Items)
                    {
                        string status = item.IsServed ? "✔" : "❌";
                        sb.AppendLine($"- {item.Name} ({item.Price} zł) [{status}]");
                    }

                    sb.AppendLine($"💰 Razem: {order.TotalPrice} zł");
                    sb.AppendLine($"🕒 Utworzone: {order.CreatedAt:HH:mm:ss}");
                    sb.AppendLine(order.IsPaid ? "✅ Opłacone" : "⌛ Oczekuje na płatność");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("🟨 Brak aktywnego zamówienia.");
                }
                payButton.Visible = order != null;

                if (!infoPanel.Controls.Contains(payButton))
                    infoPanel.Controls.Add(payButton);

                tableInfoBox.Text = sb.ToString();
            }
            else
            {
                tableInfoBox.Text = "Wybierz stolik, aby zobaczyć szczegóły.";
            }
        }

        public void Enable()
        {
            infoPanel.Visible = true;
            infoPanel.Controls.Clear();
            infoPanel.Controls.Add(tableInfoBox);
            infoPanel.Controls.Add(payButton);
        }


        public void Disable()
        {
            selectedTable = null;
            UpdateInfoPanel();
        }

        public void Draw(Graphics g)
        {
            TableRenderer.DrawTables(g, tables, selectedTable);
        }

        public void UpdateOrders(List<Order> newOrders)
        {
            orders = newOrders;
        }
        private void PayButton_Click(object sender, EventArgs e)
        {
            var order = orders.FindLast(o => o.TableId == selectedTable.Id && !o.IsPaid);
            if (order == null) return;

            var receiptForm = new ReceiptForm(order, async () =>
            {
                await DeleteOrderFromApi(order);
                SaveOrderToLocalFile(order);
                orders.Remove(order);
                UpdateInfoPanel();
            });
            receiptForm.BringToFront();
            receiptForm.TopMost = true;
            receiptForm.StartPosition = FormStartPosition.CenterParent;
            receiptForm.ShowDialog();
        }
        private async Task DeleteOrderFromApi(Order order)
        {
            using HttpClient client = new();
            var response = await client.DeleteAsync($"{ApiBaseUrl}/orders/{order.Id}");
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Błąd usuwania zamówienia: {response.StatusCode}", "API Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void SaveOrderToLocalFile(Order order)
        {
            string dir = "archiwum";
            Directory.CreateDirectory(dir);
            string filename = $"zamówienia-{DateTime.Now:MM-yyyy}.json";
            string path = Path.Combine(dir, filename);

            List<Order> archive = new();
            if (File.Exists(path))
            {
                string existing = File.ReadAllText(path);
                archive = JsonConvert.DeserializeObject<List<Order>>(existing) ?? new();
            }

            order.IsPaid = true;
            archive.Add(order);
            File.WriteAllText(path, JsonConvert.SerializeObject(archive, Formatting.Indented));
        }
        private void StartSelectedTableSyncTimer()
        {
            syncSelectedTableTimer = new System.Timers.Timer(5000); // co 5s
            syncSelectedTableTimer.Elapsed += async (s, e) => await SyncSelectedTable();
            syncSelectedTableTimer.AutoReset = true;
            syncSelectedTableTimer.Start();
        }
        private async Task SyncSelectedTable()
        {
            if (selectedTable == null) return;

            using HttpClient client = new();

            try
            {
                var json = await client.GetStringAsync($"{ApiBaseUrl}/tables");
                var apiTables = JsonConvert.DeserializeObject<List<Table>>(json);

                var updated = apiTables?.FirstOrDefault(t => t.Id == selectedTable.Id);
                if (updated != null)
                {
                    // Aktualizuj dane lokalnego stolika
                    selectedTable.Position = updated.Position;
                    selectedTable.Size = updated.Size;
                    selectedTable.Rotation = updated.Rotation;
                    try
                    {
                        var ordersJson = await client.GetStringAsync($"{ApiBaseUrl}/orders");
                        var apiOrders = JsonConvert.DeserializeObject<List<Order>>(ordersJson);
                        orders = apiOrders ?? new();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Błąd synchronizacji zamówień: " + ex.Message);
                    }
                    layoutPanel.Invoke(() => UpdateInfoPanel());
                }
            }
            catch (Exception ex)
            {
                // Ignoruj błędy sieciowe/synchronizacji, ale nie blokuj GUI
                Console.WriteLine("Błąd synchronizacji stolika: " + ex.Message);
            }
        }
    }
}
