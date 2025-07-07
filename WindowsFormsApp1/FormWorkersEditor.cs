using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

public class FormWorkersEditor : Form
{
    private const string ApiUrl = "https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net";
    private List<Worker> workers = new();
    private int? selectedWorkerId = null;

    private ListBox workersListBox;
    private TextBox firstNameTextBox, lastNameTextBox, phoneTextBox, addressTextBox, loginTextBox, passwordTextBox;
    private DateTimePicker birthDatePicker;
    private Button addButton, editButton, deleteButton;

    public FormWorkersEditor()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadWorkers();
    }

    private void InitializeComponent()
    {
        this.Text = "Zarządzanie pracownikami";
        this.Size = new Size(600, 750);
        this.StartPosition = FormStartPosition.CenterScreen;

        workersListBox = new ListBox() { Location = new Point(10, 10), Size = new Size(560, 200) };
        workersListBox.SelectedIndexChanged += WorkersListBox_SelectedIndexChanged;

        Controls.Add(workersListBox);
        int top = 220, left = 10, labelW = 120, inputW = 420;

        void AddLabeled(string label, out TextBox box)
        {
            Controls.Add(new Label { Text = label, Location = new Point(left, top + 5), Size = new Size(labelW, 20) });
            box = new TextBox { Location = new Point(left + labelW, top), Size = new Size(inputW, 30) };
            Controls.Add(box);
            top += 40;
        }

        AddLabeled("Imię:", out firstNameTextBox);
        AddLabeled("Nazwisko:", out lastNameTextBox);
        AddLabeled("Telefon:", out phoneTextBox);
        Controls.Add(new Label { Text = "Data urodzenia:", Location = new Point(left, top + 5), Size = new Size(labelW, 20) });
        birthDatePicker = new DateTimePicker { Location = new Point(left + labelW, top), Size = new Size(inputW, 30), Format = DateTimePickerFormat.Short };
        Controls.Add(birthDatePicker);
        top += 40;
        AddLabeled("Adres:", out addressTextBox);
        AddLabeled("Login:", out loginTextBox);
        AddLabeled("Hasło:", out passwordTextBox);
        passwordTextBox.UseSystemPasswordChar = true;

        addButton = new Button { Text = "Dodaj", Location = new Point(10, top), Size = new Size(180, 40) };
        editButton = new Button { Text = "Edytuj", Location = new Point(200, top), Size = new Size(180, 40) };
        deleteButton = new Button { Text = "Usuń", Location = new Point(390, top), Size = new Size(180, 40) };

        addButton.Click += AddButton_Click;
        editButton.Click += EditButton_Click;
        deleteButton.Click += DeleteButton_Click;

        Controls.AddRange(new Control[] { addButton, editButton, deleteButton });
    }

    private async Task LoadWorkers()
    {
        using HttpClient client = new();
        try
        {
            var json = await client.GetStringAsync($"{ApiUrl}/waiters");
            workers = JsonConvert.DeserializeObject<List<Worker>>(json) ?? new();
            workersListBox.Items.Clear();

            foreach (var w in workers)
            {
                string display = $"{w.Id}: {w.FirstName} {w.LastName} ({w.Login})";
                workersListBox.Items.Add(display);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Błąd pobierania pracowników:\n" + ex.Message);
        }
    }


    private async void AddButton_Click(object sender, EventArgs e)
    {
        var worker = new Worker
        {
            FirstName = firstNameTextBox.Text,
            LastName = lastNameTextBox.Text,
            PhoneNumber = phoneTextBox.Text,
            BirthDate = birthDatePicker.Value.ToString("yyyy-MM-dd"),
            Address = addressTextBox.Text,
            Login = loginTextBox.Text,
            Password = HashPassword(passwordTextBox.Text)

        };

        using var client = new HttpClient();
        var json = JsonConvert.SerializeObject(worker);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var res = await client.PostAsync($"{ApiUrl}/waiters", content);

        if (res.IsSuccessStatusCode)
        {
            await LoadWorkers();
            ClearInputs();
        }
        else
        {
            MessageBox.Show("Błąd: " + await res.Content.ReadAsStringAsync());
        }
    }


    private async void EditButton_Click(object sender, EventArgs e)
    {
        if (selectedWorkerId == null)
        {
            MessageBox.Show("Wybierz pracownika do edycji.");
            return;
        }

        var originalWorker = workers.FirstOrDefault(w => w.Id == selectedWorkerId);
        if (originalWorker == null)
        {
            MessageBox.Show("Nie znaleziono pracownika.");
            return;
        }

        var worker = new Worker
        {
            Id = selectedWorkerId.Value,
            FirstName = firstNameTextBox.Text,
            LastName = lastNameTextBox.Text,
            PhoneNumber = phoneTextBox.Text,
            BirthDate = birthDatePicker.Value.ToString("yyyy-MM-dd"),
            Address = addressTextBox.Text,
            Login = loginTextBox.Text,
            Password = originalWorker.Password // <- zachowaj stare hasło
        };

        using var client = new HttpClient();
        var json = JsonConvert.SerializeObject(worker);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var res = await client.PutAsync($"{ApiUrl}/waiters/{selectedWorkerId}", content);

        if (res.IsSuccessStatusCode)
        {
            await LoadWorkers();
            ClearInputs();
        }
        else
        {
            MessageBox.Show("Błąd edycji: " + await res.Content.ReadAsStringAsync());
        }
    }



    private async void DeleteButton_Click(object sender, EventArgs e)
    {
        if (selectedWorkerId == null)
        {
            MessageBox.Show("Wybierz pracownika do usunięcia.");
            return;
        }

        var confirm = MessageBox.Show("Czy na pewno chcesz usunąć tego pracownika?", "Potwierdzenie", MessageBoxButtons.YesNo);
        if (confirm != DialogResult.Yes)
            return;

        using var client = new HttpClient();
        var res = await client.DeleteAsync($"{ApiUrl}/waiters/{selectedWorkerId}");

        if (res.IsSuccessStatusCode)
        {
            await LoadWorkers();
            ClearInputs();
        }
        else
        {
            MessageBox.Show("Błąd usuwania: " + await res.Content.ReadAsStringAsync());
        }
    }
    private void WorkersListBox_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (workersListBox.SelectedIndex >= 0 && workersListBox.SelectedIndex < workers.Count)
        {
            var selected = workers[workersListBox.SelectedIndex];
            selectedWorkerId = selected.Id;

            firstNameTextBox.Text = selected.FirstName;
            lastNameTextBox.Text = selected.LastName;
            phoneTextBox.Text = selected.PhoneNumber;
            addressTextBox.Text = selected.Address;
            loginTextBox.Text = selected.Login;
            passwordTextBox.Text = selected.Password;
            if (DateTime.TryParse(selected.BirthDate, out DateTime date))
            {
                birthDatePicker.Value = date;
            }
        }
    }
    private void ClearInputs()
    {
        firstNameTextBox.Text = "";
        lastNameTextBox.Text = "";
        phoneTextBox.Text = "";
        birthDatePicker.Value = DateTime.Today;
        addressTextBox.Text = "";
        loginTextBox.Text = "";
        passwordTextBox.Text = "";
        selectedWorkerId = null;
    }
    private string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        byte[] hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

}
