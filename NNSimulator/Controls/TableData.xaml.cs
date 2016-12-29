using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;

using Encog.ML.Data;
using Encog.ML.Data.Basic;

using CultureInfo = System.Globalization.CultureInfo;
using System.Text.RegularExpressions;

namespace NNSimulator.Controls
{
    public partial class TableData : UserControl
    {
        public event EventHandler DataChanged;
        public event EventHandler RowSelectionChanged;

        IList<IMLDataPair> Data;

        public DataTable dataTable { get; private set; } = new DataTable();
        public int InputCount { get; private set; } = 1;
        public int OutputCount { get; private set; } = 1;

        public TableData()
        {
            InitializeComponent();
        }

        //ToDo: возможно лучше использовать binding, если это возможно
        public void UpdateData(MLP netEditor)
        {
            dataTable.Reset();

            for (int i = 0; i < netEditor.InputCount; i++)
                dataTable.Columns.Add("X" + (i + 1));
            for (int i = 0; i < netEditor.OutputCount; i++)
                dataTable.Columns.Add("Y" + (i + 1));

            InputCount = netEditor.InputCount;
            OutputCount = netEditor.OutputCount;
            dataGrid.ItemsSource = dataTable.AsDataView();
        }

        public IList<IMLDataPair> GetData()
        {
            Data = new List<IMLDataPair>();
            foreach (DataRow row in dataTable.AsEnumerable())
            {
                var convRow = row.ItemArray.
                    Select(x => Convert.ToDouble(x.ToString(), CultureInfo.InvariantCulture));
                IMLData X = new BasicMLData(convRow.Take(InputCount).ToArray());
                IMLData Y = new BasicMLData(convRow.Skip(InputCount).ToArray());
                Data.Add(new BasicMLDataPair(X, Y));
            }

            return Data;
        }

        private void btnAddRow_Click(Object sender, RoutedEventArgs e)
        {
            var row = dataTable.NewRow();
            for (int i = 0; i < dataTable.Columns.Count; i++)
                row[i] = 0;
            dataTable.Rows.Add(row);
            dataGrid.ItemsSource = dataTable.AsDataView();
        }

        private void btnRemoveRow_Click(Object sender, RoutedEventArgs e)
        {
            if (dataTable.Rows.Count > 0)
            {
                dataTable.Rows.RemoveAt(dataTable.Rows.Count - 1);
                dataGrid.ItemsSource = dataTable.AsDataView();
            }
        }

        private void btnOpenCSV_Click(Object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV (*.csv)|*.csv";

            if (openFileDialog.ShowDialog() == false)
                return;

            InputCount = 0;
            OutputCount = 0;

            string[][] rows = File.ReadAllLines(openFileDialog.FileName)
                .Select(r => r.Replace(@"""", "").Split(new char[] { ';' })).ToArray();

            foreach (string s in rows[0])
                if (s.ToLower().StartsWith("y"))
                    OutputCount += 1;
            InputCount = rows[0].Length - OutputCount;

            dataTable.Reset();

            for (int i = 0; i < InputCount; i++)
                dataTable.Columns.Add("X" + (i + 1));
            for (int i = 0; i < OutputCount; i++)
                dataTable.Columns.Add("Y" + (i + 1));

            for (int i = 1; i < rows.Length; i++)
                dataTable.Rows.Add(rows[i]);

            dataGrid.ItemsSource = dataTable.AsDataView();

            DataChanged(this, EventArgs.Empty);
        }

        //ToDo: Переписать
        private void btnSave_Click(Object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV (*.csv)|*.csv";
            string dataToSave = "";

            if (saveFileDialog.ShowDialog() == false)
                return;

            foreach (DataColumn col in dataTable.Columns)
                dataToSave += String.Format("{0};", col.ColumnName);
            dataToSave = dataToSave.Remove(dataToSave.Length - 1);
            foreach (DataRow row in dataTable.Rows)
                dataToSave += "\n" + String.Join(";", row.ItemArray);

            File.WriteAllText(saveFileDialog.FileName, dataToSave);
        }

        private void dataGrid_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            if (dataGrid.SelectedIndex != -1)
                RowSelectionChanged(this, EventArgs.Empty);
        }

        private void dataGrid_PreviewTextInput(Object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = new Regex("[^0-9][.]+").IsMatch(e.Text);
        }
    }
}