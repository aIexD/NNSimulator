using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using Microsoft.Win32;
using Drawing = System.Drawing;

using System.Threading;
using System.Windows.Threading;

using Encog.ML.Data;
using Encog.ML.Data.Basic;
using Encog.MathUtil;
using Encog.Neural.SOM;
using Encog.Neural.SOM.Training.Neighborhood;
using Encog.MathUtil.RBF;

namespace NNSimulator.Controls
{
    public partial class SOM : UserControl
    {
        public event EventHandler DataChanged;

        const int CELL_SIZE = 5;
        const int WIDTH = 90;
        const int HEIGHT = 90;

        SOMNetwork network;
        BasicTrainSOM train;
        IList<IMLData> trainingSet;
        INeighborhoodFunction RBFunc;

        Drawing.Image image;
        Drawing.Bitmap bitmap;

        public int Iteration { get; private set; }
        public double Error { get { return train.Error; } }

        public SOM()
        {
            InitializeComponent();
        }

        public void Learn(CancellationToken token, Action action)
        {
            if (network == null)
                return;

            do
            {
                if (token.IsCancellationRequested)
                    return;

                Iteration++;
                var idx = (int)(ThreadSafeRandom.NextDouble() * trainingSet.Count);
                IMLData data = trainingSet[idx];
                train.TrainPattern(data);
                train.AutoDecay();
                Application.Current.Dispatcher.Invoke(action, DispatcherPriority.Background);
            } while (Iteration < 500);

            Iteration = 0;
        }

        private void btnOpen_Click(Object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JPG(*.jpg)|*.jpg|PNG(*.png)|*.png";

            if (openFileDialog.ShowDialog() == false)
                return;

            image = ResizeImage(Drawing.Image.FromFile(openFileDialog.FileName));
            bitmap = new Drawing.Bitmap(image);

            trainingSet = new List<IMLData>();

            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    var basicData = new BasicMLData(3);
                    basicData.Data[0] = bitmap.GetPixel(i, j).R;
                    basicData.Data[1] = bitmap.GetPixel(i, j).G;
                    basicData.Data[2] = bitmap.GetPixel(i, j).B;
                    trainingSet.Add(basicData);
                }
            }

            SetNetwork();
        }

        private void btnRandom_Click(Object sender, RoutedEventArgs e)
        {
            Random rand = new Random();
            trainingSet = new List<IMLData>();

            for (int i = 0; i < 30; i++)
            {
                var basicData = new BasicMLData(3);
                basicData.Data[0] = rand.Next(0, 255);
                basicData.Data[1] = rand.Next(0, 255);
                basicData.Data[2] = rand.Next(0, 255);
                trainingSet.Add(basicData);
            }

            SetNetwork();
        }

        private void SetNetwork()
        {
            if (trainingSet == null)
                return;

            Iteration = 0;
            network = new SOMNetwork(3, WIDTH * HEIGHT);
            RBFunc = new NeighborhoodRBF(RBFEnum.Gaussian, WIDTH, HEIGHT);
            train = new BasicTrainSOM(network, 0.01, null, RBFunc);
            train.SetAutoDecay(100, 0.8, 0.003, 30, 5);
            canv.Children.Clear();
            canv.Background = Brushes.Black;

            DataChanged(this, EventArgs.Empty);
        }

        public void DrawMap()
        {
            canv.Children.Clear();
            WriteableBitmap wb = new WriteableBitmap(450, 450, 96, 96, PixelFormats.Bgr32, null);
            Image img = new Image();
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
            img.Source = wb;
            img.Stretch = Stretch.Fill;
            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    int index = (y * WIDTH) + x;
                    byte r = (byte)network.Weights[index, 0];
                    byte g = (byte)network.Weights[index, 1];
                    byte b = (byte)network.Weights[index, 2];
                    wb.FillRectangle(x * CELL_SIZE, y * CELL_SIZE,
                        x * CELL_SIZE + CELL_SIZE, y * CELL_SIZE + CELL_SIZE, Color.FromRgb(r, g, b));
                }
            }
            canv.Children.Add(img);
        }

        public Drawing.Image ResizeImage(Drawing.Image img)
        {
            int resizedW = Math.Min(250, img.Width);
            int resizedH = Math.Min(250, img.Height);

            Drawing.Bitmap bmp = new Drawing.Bitmap(resizedW, resizedH);
            Drawing.Graphics graphic = Drawing.Graphics.FromImage((Drawing.Image)bmp);
            graphic.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphic.DrawImage(img, 0, 0, resizedW, resizedH);
            graphic.Dispose();
            return (Drawing.Image)bmp;
        }
    }
}
