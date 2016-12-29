using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;

using Encog.ML.Data;
using Encog.ML.Data.Basic;

namespace NNSimulator.Controls
{
    public partial class PlotData : UserControl
    {
        public event EventHandler DataChanged;

        const int magicNum = 100;

        IList<IMLDataPair> Data;

        public PlotData()
        {
            InitializeComponent();
        }

        public IList<IMLDataPair> GetData()
        {
            Data = new List<IMLDataPair>();

            foreach (Point p in plln.Points)
            {
                IMLData X = new BasicMLData(new[] { p.X / magicNum });
                IMLData Y = new BasicMLData(new[] { p.Y / magicNum });
                Data.Add(new BasicMLDataPair(X, Y));
            }

            return Data;
        }

        private void Surface_MouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(Surface);
                plln.Points.Add(p);
            }
        }

        private void Surface_MouseMove(Object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point p = e.GetPosition(Surface);
                plln.Points.Add(p);
            }
        }

        //Алгоритм Дугласа-Пекера
        private IList<Point> ReducePoints(IList<Point> points, double epsilon)
        {
            var indexes = new List<int>(2);
            indexes.Add(0);
            indexes.Add(points.Count - 1);

            ReducePoints(points, epsilon, indexes, 0, points.Count - 1);

            return indexes.OrderBy(i => i).Select(i => points[i]).ToList();
        }

        private void ReducePoints(IList<Point> points, double epsilon, List<int> indexes, int first, int last)
        {
            if (first + 1 < last)
            {
                var dx = points[first].X - points[last].X;
                var dy = points[first].Y - points[last].Y;
                var length = (points.First() - points.Last()).Length;
                var maxDistance = 0d;
                var farthest = 0;

                for (var index = first + 1; index < last; index++)
                {
                    var dxi = points[first].X - points[index].X;
                    var dyi = points[first].Y - points[index].Y;

                    var distance = Math.Abs(dx * dyi - dxi * dy) / length;

                    if (distance > maxDistance)
                    {
                        maxDistance = distance;
                        farthest = index;
                    }
                }

                if (maxDistance > epsilon)
                {
                    indexes.Add(farthest);
                    ReducePoints(points, epsilon, indexes, first, farthest);
                    ReducePoints(points, epsilon, indexes, farthest, last);
                }
            }
        }

        private void Surface_MouseUp(Object sender, MouseButtonEventArgs e)
        {
            plln.Points = new PointCollection(ReducePoints(plln.Points, 0.03));
            DataChanged(this, EventArgs.Empty);
        }

        private void btnClear_Click(Object sender, RoutedEventArgs e)
        {
            DataChanged(this, EventArgs.Empty);
            plln.Points.Clear();
            plln2.Points.Clear();
        }

        public void Draw(IEnumerable<Point> points)
        {
            plln2.Points.Clear();
            points = points.Select(x => new Point(x.X * magicNum, x.Y * magicNum));
            plln2.Points = new PointCollection((points.OrderBy(i => i.X)));
        }
    }
}
