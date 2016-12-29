using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Threading;
using System.Windows.Threading;

using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.Neural.Networks;
using Encog.Neural.Networks.Layers;
using Encog.Engine.Network.Activation;
using Encog.ML.Train;
using Encog.Neural.Networks.Training.Propagation.Resilient;

namespace NNSimulator.Controls
{
    public partial class MLP : UserControl
    {
        public event EventHandler StructureChanged;

        IMLTrain train;

        public BasicNetwork Network { get; private set; }
        public IMLDataSet TrainingSet { get; private set; }
        public int Iteration { get; private set; } = 0;
        public double Error { get { return train.Error; } }

        public ObservableCollection<Layer> Layers { get; private set; } = new ObservableCollection<Layer>();

        public int InputCount { get { return Layers[0].Neurons.Count; } }
        public int OutputCount { get { return Layers.Last().Neurons.Count; } }

        public bool IsApproximator { get; set; } = false;

        public MLP()
        {
            InitializeComponent();
            SetNeurons();
        }

        public void Learn(CancellationToken token, Action action)
        {
            if (TrainingSet == null)
                return;

            do
            {
                if (token.IsCancellationRequested)
                    return;

                train.Iteration();
                Iteration++;
                Application.Current.Dispatcher.Invoke(action, DispatcherPriority.Background);
            } while (train.Error > 0.001 && Iteration < 50000);
        }

        public void SetNetwork(IList<IMLDataPair> data)
        {
            if (Iteration >= 50000 || (Iteration > 0 && train.Error <= 0.001))
                ResetNetwork();

            if (Network != null)
                return;

            Network = new BasicNetwork();
            Network.AddLayer(new BasicLayer(null, true, InputCount));

            for (int i = 1; i < Layers.Count - 1; i++)
                Network.AddLayer(new BasicLayer(new ActivationLOG(), true, Layers[i].Neurons.Count));

            Network.AddLayer(new BasicLayer(new ActivationLOG(), false, OutputCount));
            Network.Structure.FinalizeStructure();
            Network.Reset();

            TrainingSet = new BasicMLDataSet(data);
            train = new ResilientPropagation(Network, TrainingSet);

        }

        public void ResetNetwork()
        {
            Iteration = 0;
            Network = null;
        }

        public void SetNeurons(int input = 1, int output = 1)
        {
            if (Layers.Count == 0)
            {
                for (int i = 0; i < 2; i++)
                {
                    Layer layer = new Layer();
                    layer.Neurons.Add(new Neuron());
                    Layers.Add(layer);
                }
            }
            else
            {
                Layers.Remove(Layers.Last());
                Layers.RemoveAt(0);
                Layer layer1 = new Layer();
                Layer layer2 = new Layer();
                for (int i = 0; i < input; i++)
                    layer1.Neurons.Add(new Neuron());
                for (int i = 0; i < output; i++)
                    layer2.Neurons.Add(new Neuron());
                Layers.Insert(0, layer1);
                Layers.Add(layer2);
            }
        }

        private void btnAddLayer_Click(Object sender, RoutedEventArgs e)
        {
            if (Layers.Count < 7)
            {
                Layer layer = new Layer();
                layer.Neurons.Add(new Neuron());
                Layers.Insert(Layers.Count - 1, layer);
                StructureChanged(this, EventArgs.Empty);
            }
        }

        private void btnRemoveLayer_Click(Object sender, RoutedEventArgs e)
        {
            if (Layers.Count > 2)
            {
                Layers.RemoveAt(Layers.Count - 2);
                StructureChanged(this, EventArgs.Empty);
            }
        }

        private void btnAddNeuron_Click(Object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int index = icL.Items.IndexOf(btn.DataContext);

            if ((index == 0 || index == Layers.Count - 1) && IsApproximator)
                return;

            if (Layers[index].Neurons.Count < 9)
            {
                Layers[index].Neurons.Add(new Neuron());
                StructureChanged(this, EventArgs.Empty);
            }
        }

        private void btnRemoveNeuron_Click(Object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            int index = icL.Items.IndexOf(btn.DataContext);
            if (Layers[index].Neurons.Count > 1)
            {
                Layers[index].Neurons.Remove(Layers[index].Neurons.Last());
                StructureChanged(this, EventArgs.Empty);
            }
        }
    }

    public class Layer
    {
        public ObservableCollection<Neuron> Neurons { get; private set; } = new ObservableCollection<Neuron>();
    }

    public class Neuron
    {
        public double Content { get; set; } = 0;
    }

    public class MathConverter : IValueConverter
    {
        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            return (int)value - 2;
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
