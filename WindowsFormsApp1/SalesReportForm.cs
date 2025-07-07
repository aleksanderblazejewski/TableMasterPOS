using Newtonsoft.Json;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TableMasterPOS.Properties;

namespace WindowsFormsApp1
{
    public partial class SalesReportForm : Form
    {
        private DateTimePicker startDatePicker;
        private DateTimePicker endDatePicker;
        private Button generateButton;
        private Label resultLabel;
        private Button exportButton;
        private RadioButton radioGeneral;
        private RadioButton radioDetailed;

        private List<Order> ordersInRange = new();

        public SalesReportForm()
        {
            ExcelPackage.License.SetNonCommercialOrganization("TableMaster");

            Text = "Raport sprzedaży";
            Size = new Size(400, 350);

            Label lblStart = new Label() { Text = "Data od:", Location = new Point(20, 20), AutoSize = true };
            startDatePicker = new DateTimePicker() { Location = new Point(100, 20), Width = 200 };

            Label lblEnd = new Label() { Text = "Data do:", Location = new Point(20, 60), AutoSize = true };
            endDatePicker = new DateTimePicker() { Location = new Point(100, 60), Width = 200 };

            radioGeneral = new RadioButton() { Text = "Ogólny", Location = new Point(100, 100), Checked = true };
            radioDetailed = new RadioButton() { Text = "Szczegółowy", Location = new Point(180, 100) };

            generateButton = new Button() { Text = "Generuj", Location = new Point(20, 130) };
            generateButton.Click += GenerateButton_Click;

            resultLabel = new Label() { Location = new Point(20, 170), Size = new Size(340, 80), AutoSize = false };

            exportButton = new Button() { Text = "Eksportuj do Excela", Location = new Point(20, 260), Enabled = false };
            exportButton.Click += ExportButton_Click;

            Controls.AddRange(new Control[] { lblStart, startDatePicker, lblEnd, endDatePicker, radioGeneral, radioDetailed, generateButton, resultLabel, exportButton });
        }

        private void GenerateButton_Click(object sender, EventArgs e)
        {
            string archiveDir = "archiwum";
            if (!Directory.Exists(archiveDir))
            {
                MessageBox.Show("Brak folderu archiwum z danymi.");
                return;
            }

            ordersInRange.Clear();
            foreach (string file in Directory.GetFiles(archiveDir, "zamówienia-*.json"))
            {
                var json = File.ReadAllText(file);
                var orders = JsonConvert.DeserializeObject<List<Order>>(json);
                if (orders == null) continue;

                ordersInRange.AddRange(orders.Where(o => o.CreatedAt >= startDatePicker.Value.Date && o.CreatedAt <= endDatePicker.Value.Date.AddDays(1).AddSeconds(-1)));
            }

            if (ordersInRange.Count == 0)
            {
                resultLabel.Text = "Brak danych w podanym okresie.";
                exportButton.Enabled = false;
                return;
            }

            double total = (double)ordersInRange.Sum(o => o.TotalPrice);
            int days = (int)(endDatePicker.Value.Date - startDatePicker.Value.Date).TotalDays + 1;
            double avgPerDay = total / days;

            resultLabel.Text = $"Liczba zamówień: {ordersInRange.Count}\nŁączny utarg: {total:F2} zł\nŚredni dzienny: {avgPerDay:F2} zł";
            exportButton.Enabled = true;
        }

        private void ExportButton_Click(object sender, EventArgs e)
        {
            string tempImagePath = Path.Combine(Path.GetTempPath(), "logo_temp.png");
            Resources.Logo.Save(tempImagePath, System.Drawing.Imaging.ImageFormat.Png);

            string reportDir = "raporty";
            Directory.CreateDirectory(reportDir);
            string filePath;

            using (var package = new ExcelPackage())
            {
                var sheet = package.Workbook.Worksheets.Add("Raport");
                var pic = sheet.Drawings.AddPicture("Logo", tempImagePath);
                pic.SetPosition(0, 0, 0, 0);
                pic.SetSize(150, 150);

                int startRow = 10;

                if (radioGeneral.Checked)
                {
                    filePath = Path.Combine(reportDir, $"Raport_ogólny_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    sheet.Cells[startRow, 1].Value = "Data";
                    sheet.Cells[startRow, 2].Value = "Liczba zamówień";
                    sheet.Cells[startRow, 3].Value = "Utarg";

                    using (var range = sheet.Cells[startRow, 1, startRow, 3])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    var grouped = ordersInRange.GroupBy(o => o.CreatedAt.Date).OrderBy(g => g.Key).ToList();
                    for (int i = 0; i < grouped.Count; i++)
                    {
                        var date = grouped[i].Key;
                        var total = grouped[i].Sum(o => o.TotalPrice);
                        var count = grouped[i].Count();

                        sheet.Cells[i + startRow + 1, 1].Value = date.ToString("yyyy-MM-dd");
                        sheet.Cells[i + startRow + 1, 2].Value = count;
                        sheet.Cells[i + startRow + 1, 3].Value = total;
                    }

                    var chart = sheet.Drawings.AddChart("UtargChart", eChartType.Line) as ExcelLineChart;
                    chart.Title.Text = "Utarg dzienny";
                    chart.SetPosition(grouped.Count + startRow + 3, 0, 0, 0);
                    chart.SetSize(600, 300);
                    chart.Series.Add(sheet.Cells[$"C{startRow + 2}:C{startRow + grouped.Count + 1}"], sheet.Cells[$"A{startRow + 2}:A{startRow + grouped.Count + 1}"]);
                    chart.Legend.Remove();

                    int summaryRow = startRow + grouped.Count + 10;
                    sheet.Cells[summaryRow, 1].Value = "Podsumowanie";
                    sheet.Cells[summaryRow + 1, 1].Value = "Łączna liczba zamówień:";
                    sheet.Cells[summaryRow + 1, 2].Value = ordersInRange.Count;
                    sheet.Cells[summaryRow + 2, 1].Value = "Łączny utarg:";
                    sheet.Cells[summaryRow + 2, 2].Value = ordersInRange.Sum(o => o.TotalPrice);
                }
                else
                {
                    filePath = Path.Combine(reportDir, $"Raport_szczegółowy_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                    sheet.Cells[startRow, 1].Value = "Data";
                    sheet.Cells[startRow, 2].Value = "Godzina";
                    sheet.Cells[startRow, 3].Value = "ID Zamówienia";
                    sheet.Cells[startRow, 4].Value = "Stolik";
                    sheet.Cells[startRow, 5].Value = "Suma zamówienia";

                    using (var range = sheet.Cells[startRow, 1, startRow, 5])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }

                    var sorted = ordersInRange.OrderBy(o => o.CreatedAt).ToList();
                    for (int i = 0; i < sorted.Count; i++)
                    {
                        var order = sorted[i];
                        int row = i + startRow + 1;
                        sheet.Cells[row, 1].Value = order.CreatedAt.ToString("yyyy-MM-dd");
                        sheet.Cells[row, 2].Value = order.CreatedAt.ToString("HH:mm:ss");
                        sheet.Cells[row, 3].Value = order.Id;
                        sheet.Cells[row, 4].Value = order.TableId;
                        sheet.Cells[row, 5].Value = order.TotalPrice;

                        if (i % 2 == 1)
                        {
                            using (var range = sheet.Cells[row, 1, row, 5])
                            {
                                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(240, 240, 240));
                            }
                        }
                    }

                    var chart = sheet.Drawings.AddChart("UtargChart", eChartType.Line) as ExcelLineChart;
                    chart.Title.Text = "Utarg dzienny";
                    chart.SetPosition(sorted.Count + startRow + 3, 0, 0, 0);
                    chart.SetSize(600, 300);
                    chart.Series.Add(sheet.Cells[$"E{startRow + 2}:E{startRow + sorted.Count + 1}"], sheet.Cells[$"A{startRow + 2}:A{startRow + sorted.Count + 1}"]);
                    chart.Legend.Remove();

                    int summaryRow = startRow + sorted.Count + 10;
                    sheet.Cells[summaryRow, 1].Value = "Podsumowanie";
                    sheet.Cells[summaryRow + 1, 1].Value = "Łączna liczba zamówień:";
                    sheet.Cells[summaryRow + 1, 2].Value = ordersInRange.Count;
                    sheet.Cells[summaryRow + 2, 1].Value = "Łączny utarg:";
                    sheet.Cells[summaryRow + 2, 2].Value = ordersInRange.Sum(o => o.TotalPrice);
                }

                sheet.Cells.AutoFitColumns();
                package.SaveAs(new FileInfo(filePath));
            }

            if (File.Exists(tempImagePath))
                File.Delete(tempImagePath);

            MessageBox.Show("Zapisano raport: " + filePath);
            System.Diagnostics.Process.Start("explorer.exe", filePath);
        }
    }
}
