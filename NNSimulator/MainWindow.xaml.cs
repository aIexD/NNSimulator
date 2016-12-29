using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

using Encog.ML.Data;
using System.Threading;
using NNSimulator.Controls;

namespace NNSimulator
{
    public partial class MainWindow : Window
    {
        public string[] nnType { get; } = { "MLP, n - Входов, m - Выходов", "MLP, 1 Вход, 1 Выход", "Self Organazing Map" };
        CancellationTokenSource cancelSource = new CancellationTokenSource();
        bool IsPressed = false;
        Point anchorPoint;
        TranslateTransform transform;
        Action action;

        TableData tableData;
        PlotData plotData;

        SOM som;
        MLP mlp;

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Start_Click(Object sender, MouseButtonEventArgs e)
        {
            if (!IsPressed)
            {
                Start.Children.Clear();
                Start.Children.Add(this.FindResource("BtnPauseState") as Grid);
                IsPressed = true;
                cancelSource = new CancellationTokenSource();

                switch (cbType.SelectedIndex)
                {
                    case 0:
                        mlp.SetNetwork(tableData.GetData());
                        await Task.Run(() => mlp.Learn(cancelSource.Token, action));
                        break;
                    case 1:
                        mlp.SetNetwork(plotData.GetData());
                        await Task.Run(() => mlp.Learn(cancelSource.Token, action));
                        break;
                    case 2:
                        await Task.Run(() => som.Learn(cancelSource.Token, action));
                        break;
                }
            }
            cancelSource.Cancel();
            IsPressed = false;
            Start.Children.Clear();
            Start.Children.Add(this.FindResource("BtnStartState") as Grid);
        }

        //ToDo: Переместить события в XAML
        private void cbType_SelectionChanged(Object sender, SelectionChangedEventArgs e)
        {
            cancelSource.Cancel();
            canv.Children.Clear();
            tableData = null;
            plotData = null;
            mlp = null;
            som = null;

            switch (cbType.SelectedIndex)
            {
                case 0:
                    mlp = new MLP();
                    mlp.Template = FindResource("UCTemplate") as ControlTemplate;
                    mlp.Style = FindResource("mlpStyle") as Style;
                    mlp.StructureChanged += MLP_StructureChanged;
                    canv.Children.Add(mlp);

                    tableData = new TableData();
                    tableData.Template = FindResource("UCTemplate") as ControlTemplate;
                    tableData.Style = FindResource("tableStyle") as Style;
                    tableData.DataChanged += TableControl_DataChanged;
                    tableData.RowSelectionChanged += TableControl_RowSelectionChanged;
                    canv.Children.Add(tableData);
                    tableData.UpdateData(mlp);

                    action = () =>
                    {
                        if (cancelSource.IsCancellationRequested)
                            return;

                        txtError.Text = Math.Round(mlp.Error, 3).ToString();
                        txtIteration.Text = mlp.Iteration.ToString();
                    };
                    break;
                case 1:
                    plotData = new PlotData();
                    plotData.Template = FindResource("UCTemplate") as ControlTemplate;
                    plotData.Style = FindResource("plotStyle") as Style;
                    plotData.DataChanged += PlotData_DataChanged;
                    canv.Children.Add(plotData);

                    mlp = new MLP();
                    mlp.IsApproximator = true;
                    mlp.Template = FindResource("UCTemplate") as ControlTemplate;
                    mlp.Style = FindResource("mlpStyle") as Style;
                    mlp.StructureChanged += MLP_StructureChanged;
                    canv.Children.Add(mlp);

                    action = () =>
                    {
                        if (cancelSource.IsCancellationRequested)
                            return;

                        txtError.Text = Math.Round(mlp.Error, 3).ToString();
                        txtIteration.Text = mlp.Iteration.ToString();
                        List<Point> points = new List<Point>();
                        foreach (IMLDataPair pair in mlp.TrainingSet)
                        {
                            IMLData result = mlp.Network.Compute(pair.Input);
                            points.Add(new Point(pair.Input[0], result[0]));
                        }
                        plotData.Draw(points);
                    };
                    break;
                case 2:
                    som = new SOM();
                    som.Template = FindResource("UCTemplate") as ControlTemplate;
                    som.Style = FindResource("somStyle") as Style;
                    som.DataChanged += SOM_DataChanged;
                    canv.Children.Add(som);

                    action = () =>
                    {
                        if (cancelSource.IsCancellationRequested)
                            return;

                        txtError.Text = Math.Round(som.Error, 3).ToString();
                        txtIteration.Text = som.Iteration.ToString();
                        som.DrawMap();
                    };
                    break;
            }
        }

        private void MLP_StructureChanged(Object sender, EventArgs e)
        {
            cancelSource.Cancel();
            if (tableData != null && (mlp.InputCount != tableData.InputCount || mlp.OutputCount != tableData.OutputCount))
                tableData.UpdateData(mlp);
            mlp.ResetNetwork();
        }

        private void TableControl_DataChanged(Object sender, EventArgs e)
        {
            cancelSource.Cancel();
            mlp.SetNeurons(tableData.InputCount, tableData.OutputCount);
            mlp.ResetNetwork();
        }

        private void TableControl_RowSelectionChanged(Object sender, EventArgs e)
        {
            if (mlp == null || mlp.Network == null || mlp.Network.LayerCount != mlp.Layers.Count)
                return;

            var rowIndex = tableData.dataGrid.SelectedIndex;

            mlp.Network.Compute(tableData.GetData()[rowIndex].Input);

            for (int i = 0; i < mlp.Layers.Count; i++)
            {
                for (int j = 0; j < mlp.Layers[i].Neurons.Count; j++)
                {
                    Neuron neuron = new Neuron();
                    neuron.Content = Math.Round(mlp.Network.GetLayerOutput(i, j), 2);
                    mlp.Layers[i].Neurons.RemoveAt(j);
                    mlp.Layers[i].Neurons.Insert(j, neuron);
                }
            }
        }

        private void SOM_DataChanged(Object sender, EventArgs e)
        {
            cancelSource.Cancel();
        }

        private void PlotData_DataChanged(Object sender, EventArgs e)
        {
            cancelSource.Cancel();
            mlp.ResetNetwork();
        }

        private void Header_MouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var element = sender as FrameworkElement;
                var grid = element.Parent as FrameworkElement;

                transform = new TranslateTransform();
                transform.X = grid.RenderTransform.Value.OffsetX;
                transform.Y = grid.RenderTransform.Value.OffsetY;

                anchorPoint = e.GetPosition(canv);
                element.CaptureMouse();
                e.Handled = true;
            }
        }

        private void Header_MouseMove(Object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (e.LeftButton == MouseButtonState.Pressed && element.IsMouseCaptured)
            {
                var grid = element.Parent as FrameworkElement;
                var p = e.GetPosition(canv);

                transform.X += p.X - anchorPoint.X;
                transform.Y += p.Y - anchorPoint.Y;
                grid.RenderTransform = transform;

                anchorPoint = p;
            }
        }

        private void Header_MouseUp(Object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            element.ReleaseMouseCapture();
        }
    }
}
