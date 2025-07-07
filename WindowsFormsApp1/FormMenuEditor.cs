using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TableMasterPOS
{
    public partial class FormMenuEditor : Form
    {
        private const string ApiUrl = "https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net";
        private List<MenuItem> menuItems = new();


        public FormMenuEditor()
        {
            InitializeComponent();
            InitializeComponent2();
            Load += async (s, e) => await LoadMenuItems();
        }

        private ListBox menuListBox;
        private TextBox nameTextBox;
        private TextBox priceTextBox;
        private Button addButton;
        private Button deleteButton;
        private Button editButton;
        private ComboBox categoryComboBox;
        private int? selectedMenuItemId = null;

        private void InitializeComponent2()
        {
            this.menuListBox = new ListBox();
            this.nameTextBox = new TextBox();
            this.priceTextBox = new TextBox();
            this.addButton = new Button();

            this.SuspendLayout();

            // menuListBox
            this.menuListBox.Location = new System.Drawing.Point(12, 12);
            this.menuListBox.Size = new System.Drawing.Size(560, 530);

            // nameTextBox
            this.nameTextBox.Location = new System.Drawing.Point(10, 570);
            this.nameTextBox.MinimumSize = new Size(this.Width / 3 - 40, 30);
            this.nameTextBox.Font = new Font("Segoe UI", 13F);
            // priceTextBox
            this.priceTextBox.Location = new System.Drawing.Point(this.Width / 3 - 20, 570);
            this.priceTextBox.MinimumSize = new Size(this.Width/3-40, 30);
            this.priceTextBox.Font = new Font("Segoe UI", 13F);

            // Label dla nazwy
            Label nameLabel = new Label();
            nameLabel.Text = "Nazwa dania:";
            nameLabel.Location = new Point(nameTextBox.Location.X, nameTextBox.Location.Y - 25);
            nameLabel.Size = new Size(nameTextBox.MinimumSize.Width, 20);
            nameLabel.Font = new Font("Segoe UI", 10F);

            // Label dla ceny
            Label priceLabel = new Label();
            priceLabel.Text = "Cena:";
            priceLabel.Location = new Point(priceTextBox.Location.X, priceTextBox.Location.Y - 25);
            priceLabel.Size = new Size(priceTextBox.MinimumSize.Width, 20);
            priceLabel.Font = new Font("Segoe UI", 10F);

            // Dodaj do formularza
            this.Controls.Add(nameLabel);
            this.Controls.Add(priceLabel);

            // addButton
            this.addButton.Location = new System.Drawing.Point(12, 670);
            this.addButton.Size = new Size(130, 50);
            this.addButton.Text = "Dodaj";
            this.addButton.Click += new EventHandler(this.addButton_Click);
            editButton = new Button
            {
                Text = "Edytuj",
                Location = new Point(150, 670),
                Size = new Size(130, 50)
            };
            editButton.Click += editButton_Click;

            deleteButton = new Button
            {
                Text = "Usuń",
                Location = new Point(292, 670),
                Size = new Size(130, 50)
            };
            deleteButton.Click += deleteButton_Click;

            menuListBox.Items.Clear();
            foreach (var item in menuItems)
            {
                menuListBox.Items.Add($"{item.Id}: {item.Name} - {item.Price:F2} zł");
            }

            menuListBox.SelectedIndexChanged += (s, e) =>
            {
                if (menuListBox.SelectedIndex >= 0)
                {
                    var selected = menuItems[menuListBox.SelectedIndex];
                    nameTextBox.Text = selected.Name;
                    priceTextBox.Text = selected.Price.ToString("F2");
                    categoryComboBox.SelectedItem = selected.Category;
                    selectedMenuItemId = selected.Id;
                }
            };

            categoryComboBox = new ComboBox
            {
                Location = new Point(10, 620),
                Size = new Size(260, 40),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            categoryComboBox.Items.AddRange(new string[]
            {
                "Przystawki", "Zupy", "Dania Główne", "Desery", "Napoje", "Piwo", "Wino", "Alkohol"
            });
            categoryComboBox.SelectedIndex = 0;

            this.Controls.Add(categoryComboBox);


            this.Controls.Add(editButton);
            this.Controls.Add(deleteButton);

            // FormMenuEditor
            this.ClientSize = new System.Drawing.Size(300, 330);
            this.Controls.Add(this.menuListBox);
            this.Controls.Add(this.nameTextBox);
            this.Controls.Add(this.priceTextBox);
            this.Controls.Add(this.addButton);
            this.Text = "Edycja Menu";

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private async Task LoadMenuItems()
        {
            using HttpClient client = new();
            try
            {
                var json = await client.GetStringAsync($"{ApiUrl}/menu");
                menuItems = JsonConvert.DeserializeObject<List<MenuItem>>(json);
                menuListBox.Items.Clear();

                var sortedItems = menuItems
                    .OrderBy(m => m.Category)
                    .ThenBy(m => m.Name)
                    .ToList();

                foreach (var item in sortedItems)
                {
                    menuListBox.Items.Add($"{item.Id}: [{item.Category}] {item.Name} - {item.Price:F2} zł");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("Błąd pobierania menu: " + ex.Message);
            }
        }

        private async Task SaveMenuItem(string name, decimal price)
        {
            var newItem = new MenuItem { Name = name, Price = price };
            string json = JsonConvert.SerializeObject(newItem);

            using HttpClient client = new();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiUrl}/menu", content);

            if (response.IsSuccessStatusCode)
            {
                await LoadMenuItems(); // odśwież
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Błąd zapisu: " + response.StatusCode + "\n" + error);
            }
        }

        private async void addButton_Click(object sender, EventArgs e)
        {
            string name = nameTextBox.Text.Trim();
            string category = categoryComboBox.SelectedItem?.ToString();
            bool validPrice = decimal.TryParse(priceTextBox.Text, out decimal price);

            // Walidacja
            if (string.IsNullOrWhiteSpace(name) || !validPrice || string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Wprowadź poprawną nazwę, cenę i wybierz kategorię.");
                return;
            }

            if (name == "Nazwa potrawy" || priceTextBox.Text == "Cena potrawy")
            {
                MessageBox.Show("Uzupełnij dane.");
                return;
            }

            var newItem = new MenuItem
            {
                Name = name,
                Price = price,
                Category = category
            };

            string json = JsonConvert.SerializeObject(newItem);

            using HttpClient client = new();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiUrl}/menu", content);

            if (response.IsSuccessStatusCode)
            {
                await LoadMenuItems();
                nameTextBox.Text = "";
                priceTextBox.Text = "";
                categoryComboBox.SelectedIndex = 0;
                selectedMenuItemId = null;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Błąd dodawania dania:\n" + response.StatusCode + "\n" + error);
            }
        }

        private async void deleteButton_Click(object sender, EventArgs e)
        {
            if (selectedMenuItemId == null)
            {
                MessageBox.Show("Wybierz danie do usunięcia.");
                return;
            }

            using HttpClient client = new();
            var response = await client.DeleteAsync($"{ApiUrl}/menu/{selectedMenuItemId}");

            if (response.IsSuccessStatusCode)
            {
                await LoadMenuItems();
                selectedMenuItemId = null;
                nameTextBox.Text = "";
                priceTextBox.Text = "";
            }
            else
            {
                MessageBox.Show("Nie udało się usunąć dania.");
            }
        }
        private async void editButton_Click(object sender, EventArgs e)
        {
            if (selectedMenuItemId == null)
            {
                MessageBox.Show("Wybierz danie do edycji z listy.");
                return;
            }

            string name = nameTextBox.Text.Trim();
            string category = categoryComboBox.SelectedItem?.ToString();
            bool validPrice = decimal.TryParse(priceTextBox.Text, out decimal price);

            if (string.IsNullOrWhiteSpace(name) || !validPrice || string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Podaj poprawną nazwę, cenę i kategorię.");
                return;
            }

            var updatedItem = new MenuItem
            {
                Name = name,
                Price = price,
                Category = category
            };

            string json = JsonConvert.SerializeObject(updatedItem);

            using HttpClient client = new();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{ApiUrl}/menu/{selectedMenuItemId}", content);

            if (response.IsSuccessStatusCode)
            {
                await LoadMenuItems();
                nameTextBox.Text = "";
                priceTextBox.Text = "";
                categoryComboBox.SelectedIndex = 0;
                selectedMenuItemId = null;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                MessageBox.Show("Błąd edycji dania:\n" + response.StatusCode + "\n" + error);
            }
        }

    }


}
