using SoundGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mixer
{
    /// <summary>
    /// Interaction logic for HRTFWindow.xaml
    /// </summary>
    public partial class HRTFWindow : Window
    {
        double _offsetX;
        double _offsetY;
        Ellipse _ellipse;
        SineGenerator _generator;

        public HRTFWindow()
        {
            InitializeComponent();

            _generator = new SineGenerator();

            canvasBackgroundExample();
            _ellipse = new Ellipse();
            _ellipse.Width = 10;
            _ellipse.Height = 10;
            _ellipse.Fill = Brushes.DarkGray;
            _ellipse.Margin = new Thickness(2);
            
            _offsetX = GetXPercent(HorizontalSlider.Value);
            _offsetY = GetYPercent(VerticalSlider.Value);
            EllipseRedrow(_offsetY, _offsetX);
        }

        private void canvasBackgroundExample()
        {
            DrawingBrush myDrawingBrush = new DrawingBrush();
            GeometryDrawing myGeometryDrawing = new GeometryDrawing();
            myGeometryDrawing.Pen = new Pen(Brushes.DarkGray, 1);
            GeometryGroup rectangle = new GeometryGroup();
            rectangle.Children.Add(new RectangleGeometry(new Rect(new Point(0, 0), new Point(100, 100))));

            myGeometryDrawing.Geometry = rectangle;
            myDrawingBrush.Drawing = myGeometryDrawing;
            
            HeadPicture.Background = myDrawingBrush;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {

        }

        private void HorizontalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _offsetX = GetXPercent(e.NewValue);
            EllipseRedrow(_offsetY, _offsetX);
            _generator.SetHorizontalPosition(e.NewValue);
        }

        private double GetXPercent(double changeX) =>
             Math.Max((changeX + Math.Abs(HorizontalSlider.Minimum)) * 100 / (HorizontalSlider.Maximum + Math.Abs(HorizontalSlider.Minimum)) - _ellipse.Width / 1.5, 0);

        private void VerticalSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _offsetY = GetYPercent(e.NewValue);
            EllipseRedrow(_offsetY, _offsetX);
        }

        private double GetYPercent(double changeY) =>
            Math.Max((changeY + Math.Abs(VerticalSlider.Minimum)) * 100 / (VerticalSlider.Maximum + Math.Abs(VerticalSlider.Minimum)) - _ellipse.Height / 1.5, 0);

        private void EllipseRedrow(double bottomPercent, double leftProcent)
        {
            HeadPicture.Children.Remove(_ellipse);
            var botProp = HeadPicture.Height * bottomPercent / 100;
            var leftProp = HeadPicture.Width * leftProcent / 100;
            _ellipse.SetValue(Canvas.BottomProperty, botProp);
            _ellipse.SetValue(Canvas.LeftProperty, leftProp);
            HeadPicture.Children.Add(_ellipse);
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            _generator.AddValue(10);
            _generator.Play();
        }
    }
}
