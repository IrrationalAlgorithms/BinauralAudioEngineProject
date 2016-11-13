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
using Microsoft.Win32;
using SharpDX.IO;
using SharpDX.Multimedia;

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

            HeadPictureBackground();
            EllipseInitialization();
            HRTFInitialization(LeftHRTF);
            HRTFInitialization(RightHRTF);
        }

        private void HeadPictureBackground()
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

        private void EllipseInitialization()
        {
            _ellipse = new Ellipse();
            _ellipse.Width = 10;
            _ellipse.Height = 10;
            _ellipse.Fill = Brushes.Gray;
            _ellipse.Margin = new Thickness(2);
            _offsetX = GetXPercent(AngleSlider.Value);
            _offsetY = GetYPercent(ElevationSlider.Value);
            EllipseRedrow(_offsetY, _offsetX);
        }

        private void HRTFInitialization(Canvas hrtfWindow)
        {
            DrawingBrush myDrawingBrush = new DrawingBrush();
            GeometryDrawing myGeometryDrawing = new GeometryDrawing();
            myGeometryDrawing.Pen = new Pen(Brushes.DarkGray, 1);
            GeometryGroup rectangle = new GeometryGroup();
            rectangle.Children.Add(new LineGeometry(new Point(0,0), new Point(100,100)));
            rectangle.Children.Add(new RectangleGeometry(new Rect(new Point(0, 0), new Point(100, 100))));

            myGeometryDrawing.Geometry = rectangle;
            myDrawingBrush.Drawing = myGeometryDrawing;

            hrtfWindow.Background = myDrawingBrush;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
        }

        private void AngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _offsetX = GetXPercent(e.NewValue);
            EllipseRedrow(_offsetY, _offsetX);
            _generator.SetHorizontalPosition(e.NewValue);
            if (ApplyConvolution.IsChecked == true)
            {
                Sandbox.SetConvolution(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                label.Content = ($"{ElevationSlider.Value} ---- {AngleSlider.Value}");
            }
        }

        private double GetXPercent(double changeX) =>
             Math.Max((changeX + Math.Abs(AngleSlider.Minimum)) * 100 / (AngleSlider.Maximum + Math.Abs(AngleSlider.Minimum)) - _ellipse.Width / 1.5, 0);

        private void ElevationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _offsetY = GetYPercent(e.NewValue);
            EllipseRedrow(_offsetY, _offsetX);
            if (ApplyConvolution.IsChecked == true)
            {
                Sandbox.SetConvolution(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                label.Content = ($"{ElevationSlider.Value} ---- {AngleSlider.Value}");
            }
        }

        private double GetYPercent(double changeY) =>
            Math.Max((changeY + Math.Abs(ElevationSlider.Minimum)) * 100 / (ElevationSlider.Maximum + Math.Abs(ElevationSlider.Minimum)) - _ellipse.Height / 1.5, 0);

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

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Select an audio file (wma, mp3, ...etc.) or video file..." };
            if (dialog.ShowDialog() == true)
            {
                var fileStream = new NativeFileStream(dialog.FileName, NativeFileMode.Open, NativeFileAccess.Read);
                var bytes = fileStream.ReadByte();
                //audioPlayer = new AudioPlayer(xaudio2, fileStream);

                //FilePathTextBox.Text = dialog.FileName;
                //SoundProgressBar.Maximum = audioPlayer.Duration.TotalSeconds;
                //SoundProgressBar.Value = 0;

                //// Auto-play
                //audioPlayer.Play();
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            _generator.Pause();
        }

        private float ReadSoundBytes(string fileName)
        {
            //var fileStream = new NativeFileStream(fileName, NativeFileMode.Open, NativeFileAccess.Read);
            //var soundStream = new SoundStream(fileStream);
            //var dataStream = soundStream.ToDataStream();
            //while (dataStream.CanRead)
            //{
            //    dataStream.Read<float>();
            //}

            throw new NotImplementedException();
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {
            
            Sandbox.ApplyConvolutionChecked(true);
            Sandbox.SetConvolution(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
            label.Content = ($"{ElevationSlider.Value} ---- {AngleSlider.Value}");
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Sandbox.ApplyConvolutionChecked(false);
        }

        private NativeFileStream GetFileStream(Channel channel) => new NativeFileStream
            (ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value, (int)AngleSlider.Value, channel), NativeFileMode.Open, NativeFileAccess.Read);
    }
}
