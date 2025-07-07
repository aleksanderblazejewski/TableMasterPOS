// EditModeManager.cs
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Label = System.Windows.Forms.Label;

namespace WindowsFormsApp1.Managers
{
    public class EditModeManager
    {
        private enum EditActionMode
        {
            None,
            Select,
            AddTable,
            DeleteTable,
            RotateTable
        }

        private List<Table> tables;
        private Panel layoutPanel;
        private Point? originalPositionOnDragStart = null;
        private Panel infoPanel;
        private Func<int> getNextId;
        private Form parentForm;
        private EditActionMode currentAction = EditActionMode.Select;


        private readonly Func<Table> getSelectedTable;
        private readonly Action<Table> setSelectedTable;
        private bool isDragging = false;
        private Point dragOffset;

        private Label idLabel;
        private TextBox tableIdTextBox;
        private Button applyButton;
        private Button addButton;
        private Button deleteButton;
        private Button rotateButton;
        private Button selectButton;

        public EditModeManager(List<Table> tables, Panel layoutPanel, Panel infoPanel, Form parentForm, Func<Table> getSelectedTable, Action<Table> setSelectedTable, Func<int> getNextId)
        {
            this.tables = tables;
            this.layoutPanel = layoutPanel;
            this.infoPanel = infoPanel;
            this.parentForm = parentForm;
            this.getNextId = getNextId;
            this.getSelectedTable = getSelectedTable;
this.setSelectedTable = setSelectedTable;


            InitializeControls();
        }


        private void InitializeControls()
        {

            selectButton = new Button { Text = "Wybierz", Location = new Point(230, 10), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            selectButton.Click += (s, e) => currentAction = EditActionMode.Select;

            addButton = new Button { Text = "Dodaj stolik", Location = new Point(340, 10), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            addButton.Click += (s, e) => currentAction = EditActionMode.AddTable;

            deleteButton = new Button { Text = "Usuń stolik", Location = new Point(450, 10), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            deleteButton.Click += (s, e) => currentAction = EditActionMode.DeleteTable;

            rotateButton = new Button { Text = "Obróć stolik", Location = new Point(560, 10), Size = new Size(110, 30), FlatStyle = FlatStyle.Flat };
            rotateButton.Click += (s, e) => currentAction = EditActionMode.RotateTable;

            idLabel = new Label
            {
                Text = "ID stolika:",
                Location = new Point(10, 10),
                Size = new Size(160, 20)
            };

            tableIdTextBox = new TextBox
            {
                Location = new Point(10, 50),
                Size = new Size(160, 25)
            };

            applyButton = new Button
            {
                Text = "Zatwierdź",
                Size = new Size(160, 30)

            };

            applyButton.Location = new Point(10, infoPanel.Height - applyButton.Height - 10);

            applyButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            applyButton.Click += async (s, e) =>
            {
                var table = getSelectedTable();
                if (table != null && int.TryParse(tableIdTextBox.Text, out int newId))
                {
                    if (!tables.Exists(t => t.Id == newId))
                    {
                        int oldId = table.Id;
                        table.Id = newId;

                        await SaveTableToApi(table);
                        if (newId != oldId)
                            await DeleteTableFromApi(oldId);

                        layoutPanel.Invalidate();
                    }
                }
            };

        }

        public async Task HandleMouseDownAsync(MouseEventArgs e)
        {
            switch (currentAction)
            {
                case EditActionMode.AddTable:
                    {
                        var id = getNextId();
                        var position = e.Location;
                        var size = new Size(90, 60);
                        var newTable = new Table(id, position, size);
                        if (tables.Any(t => t.Id == newTable.Id))
                        {
                            MessageBox.Show("Stolik o takim ID już istnieje.");
                            return;
                        }

                        if (!newTable.IsCollidingWithAny(tables))
                        {
                            tables.Add(newTable);
                            setSelectedTable(newTable);
                            layoutPanel.Invalidate();

                            await SaveTableToApi(newTable); 
                        }
                        else
                        {
                            MessageBox.Show("Kolizja z innym stolikiem.");
                        }
                        break;
                    }

                case EditActionMode.DeleteTable:
                    {
                        foreach (var table in tables)
                        {
                            Rectangle rect = new Rectangle(table.Position, table.Size);
                            if (rect.Contains(e.Location))
                            {
                                tables.Remove(table);
                                setSelectedTable(null);
                                layoutPanel.Invalidate();
                                await DeleteTableFromApi(table.Id);
                                break;
                            }
                        }
                        break;
                    }

                case EditActionMode.RotateTable:
                    {
                        foreach (var table in tables)
                        {
                            Rectangle rect = new Rectangle(table.Position, table.Size);
                            if (rect.Contains(e.Location))
                            {
                                var newSize = new Size(table.Size.Height, table.Size.Width);
                                var testTable = new Table(table.Id, table.Position, newSize);
                                if (!testTable.IsCollidingWithAny(tables, table))
                                {
                                    table.Size = newSize;
                                    table.Rotation = (table.Rotation + 45) % 360;
                                    await SaveTableToApi(table);
                                    layoutPanel.Invalidate();
                                }
                                else
                                {
                                    MessageBox.Show("Nie można obrócić – kolizja.");
                                }
                                break;
                            }
                        }
                        break;
                    }

                case EditActionMode.Select:
                    {
                        foreach (var table in tables)
                        {
                            Rectangle rect = new Rectangle(table.Position, table.Size);
                            if (rect.Contains(e.Location))
                            {
                                setSelectedTable(table);
                                dragOffset = new Point(e.X - table.Position.X, e.Y - table.Position.Y);
                                originalPositionOnDragStart = table.Position;
                                isDragging = true;
                                UpdateInfoPanel();
                                layoutPanel.Invalidate();
                                return;
                            }
                        }
                        setSelectedTable(null);
                        UpdateInfoPanel();
                        layoutPanel.Invalidate();
                        break;
                    }
            }
        }


        public void HandleMouseMove(MouseEventArgs e)
        {
            var table = getSelectedTable();
            if (isDragging && table != null)
            {
                var newPosition = new Point(e.X - dragOffset.X, e.Y - dragOffset.Y);
                var testTable = new Table(table.Id, newPosition, table.Size);
                if (!testTable.IsCollidingWithAny(tables, table))
                {
                    table.Position = newPosition;
                    layoutPanel.Invalidate();
                }
            }
        }

        private async Task SaveTableToApi(Table table)
        {
            using HttpClient client = new();
            var json = JsonConvert.SerializeObject(table);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net/tables", content); // <-- Uzupełnij swój URL
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Błąd zapisu stolika: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public async Task HandleMouseUpAsync(MouseEventArgs e)
        {
            isDragging = false;
            var table = getSelectedTable();

            if (table != null && originalPositionOnDragStart.HasValue)
            {
                if (table.Position != originalPositionOnDragStart.Value)
                {
                    await SaveTableToApi(table);
                }

                originalPositionOnDragStart = null; 
            }
        }

        public void Enable()
        {
            currentAction = EditActionMode.Select;

            infoPanel.Visible = true;
            infoPanel.Controls.Clear();

            infoPanel.Controls.AddRange(new Control[]
{
                idLabel,
                tableIdTextBox,
                applyButton
            });

            parentForm.Controls.AddRange(new Control[]
            {
                selectButton, addButton, deleteButton, rotateButton
            });
        }



        public void Disable()
        {
            isDragging = false;

            parentForm.Controls.Remove(selectButton);
            parentForm.Controls.Remove(addButton);
            parentForm.Controls.Remove(deleteButton);
            parentForm.Controls.Remove(rotateButton);
        }

        private void UpdateInfoPanel()
        {
            var table = getSelectedTable();
            if (table != null)
            {
                tableIdTextBox.Text = table.Id.ToString();
            }
            else
            {
                tableIdTextBox.Text = "";
            }
        }
        public void Draw(Graphics g)
        {
            TableRenderer.DrawTables(g, tables, getSelectedTable());
        }

        private async Task DeleteTableFromApi(int tableId)
        {
            using HttpClient client = new();
            var response = await client.DeleteAsync($"https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net/tables/{tableId}");

            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show($"Błąd usuwania stolika: {response.StatusCode}", "API Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }
}
