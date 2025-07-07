using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TableMasterPOS
{
    public partial class FormTablesEditor : Form
    {
        private const string ApiUrl = "https://pos-api-hmavg4e4execcuhp.polandcentral-01.azurewebsites.net";

        private List<TableGroup> groups = new();
        private List<Worker> workers = new();
        private List<Table> tables = new();

        private ListBox groupsListBox;
        private TextBox groupNameTextBox;
        private CheckedListBox waiterSelector;
        private CheckedListBox tableSelector;
        private Button addButton, saveButton, deleteButton;

        public FormTablesEditor(List<Table> initialTables)
        {
            InitializeComponent();
            InitializeComponent2();
            this.tables = initialTables;
        }

        private void InitializeComponent2()
        {
            this.Text = "Edycja grup stolików";
            this.Size = new Size(600, 700);

            groupsListBox = new ListBox { Location = new Point(10, 10), Size = new Size(250, 600) };
            groupsListBox.SelectedIndexChanged += GroupsListBox_SelectedIndexChanged;

            groupNameTextBox = new TextBox { Location = new Point(270, 10), Size = new Size(300, 30) };
            waiterSelector = new CheckedListBox { Location = new Point(270, 50), Size = new Size(300, 200) };
            tableSelector = new CheckedListBox { Location = new Point(270, 260), Size = new Size(300, 200) };

            addButton = new Button { Text = "Dodaj grupę", Location = new Point(270, 470), Size = new Size(90, 30) };
            addButton.Click += AddButton_Click;

            saveButton = new Button { Text = "Zapisz", Location = new Point(370, 470), Size = new Size(90, 30) };
            saveButton.Click += SaveButton_Click;

            deleteButton = new Button { Text = "Usuń", Location = new Point(480, 470), Size = new Size(90, 30) };
            deleteButton.Click += DeleteButton_Click;

            this.Controls.AddRange(new Control[] {
                groupsListBox, groupNameTextBox, waiterSelector,
                tableSelector, addButton, saveButton, deleteButton
            });

            _ = LoadData();
        }

        private async Task LoadData()
        {
            using var client = new HttpClient();

            var workersJson = await client.GetStringAsync($"{ApiUrl}/waiters");
            workers = JsonConvert.DeserializeObject<List<Worker>>(workersJson) ?? new();
            waiterSelector.Items.Clear();
            foreach (var w in workers)
                waiterSelector.Items.Add($"{w.FirstName} {w.LastName}", false);

            var groupsJson = await client.GetStringAsync($"{ApiUrl}/tablegroups");
            groups = JsonConvert.DeserializeObject<List<TableGroup>>(groupsJson) ?? new();
            RefreshGroupList();

            var tablesJson = await client.GetStringAsync($"{ApiUrl}/tables");
            tables = JsonConvert.DeserializeObject<List<Table>>(tablesJson) ?? new();
            tableSelector.Items.Clear();
            foreach (var t in tables)
                tableSelector.Items.Add($"Stolik {t.Id}", false);
        }

        private void RefreshGroupList()
        {
            groupsListBox.Items.Clear();
            foreach (var g in groups)
                groupsListBox.Items.Add(g.Name);
        }

        private void GroupsListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (groupsListBox.SelectedIndex == -1) return;

            var selected = groups[groupsListBox.SelectedIndex];
            groupNameTextBox.Text = selected.Name;

            for (int i = 0; i < waiterSelector.Items.Count; i++)
                waiterSelector.SetItemChecked(i, selected.AssignedWaiterIds.Contains(workers[i].Id));

            for (int i = 0; i < tableSelector.Items.Count; i++)
                tableSelector.SetItemChecked(i, selected.AssignedTableIds.Contains(tables[i].Id));
        }

        private async void AddButton_Click(object sender, EventArgs e)
        {
            var newGroup = new TableGroup
            {
                Name = groupNameTextBox.Text,
                AssignedWaiterIds = GetSelectedIds(waiterSelector, workers.Select(w => w.Id).ToList()),
                AssignedTableIds = GetSelectedIds(tableSelector, tables.Select(t => t.Id).ToList())
            };

            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(newGroup);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{ApiUrl}/tablegroups", content);

            if (response.IsSuccessStatusCode)
            {
                await LoadData();
                groupNameTextBox.Text = "";
            }
            else
            {
                MessageBox.Show("Błąd dodawania grupy");
            }
        }

        private async void SaveButton_Click(object sender, EventArgs e)
        {
            if (groupsListBox.SelectedIndex == -1) return;

            var selected = groups[groupsListBox.SelectedIndex];
            selected.Name = groupNameTextBox.Text;
            selected.AssignedWaiterIds = GetSelectedIds(waiterSelector, workers.Select(w => w.Id).ToList());
            selected.AssignedTableIds = GetSelectedIds(tableSelector, tables.Select(t => t.Id).ToList());

            using var client = new HttpClient();
            var json = JsonConvert.SerializeObject(selected);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"{ApiUrl}/tablegroups/{selected.Id}", content);

            if (response.IsSuccessStatusCode)
                await LoadData();
            else
                MessageBox.Show("Błąd zapisu zmian");
        }

        private async void DeleteButton_Click(object sender, EventArgs e)
        {
            if (groupsListBox.SelectedIndex == -1) return;

            var selected = groups[groupsListBox.SelectedIndex];

            using var client = new HttpClient();
            var response = await client.DeleteAsync($"{ApiUrl}/tablegroups/{selected.Id}");

            if (response.IsSuccessStatusCode)
            {
                await LoadData();
                groupNameTextBox.Text = "";
                waiterSelector.ClearSelected();
            }
            else
            {
                MessageBox.Show("Błąd usuwania grupy");
            }
        }

        private List<int> GetSelectedIds(CheckedListBox box, List<int> idMap)
        {
            var result = new List<int>();
            for (int i = 0; i < box.Items.Count; i++)
                if (box.GetItemChecked(i))
                    result.Add(idMap[i]);
            return result;
        }
    }
}
