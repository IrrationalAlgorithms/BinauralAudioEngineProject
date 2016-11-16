using SoundGenerator;
using System;
using System.IO;

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
using System.Windows.Forms.DataVisualization.Charting;
using SharpDX.IO;
using SharpDX;
using SharpDX.XAudio2;
using SharpDX.Multimedia;
using SharpDX.MediaFoundation;

namespace Mixer
{
    /// <summary>
    /// Interaction logic for HRTFWindow.xaml
    /// </summary>
    public partial class HRTFWindow : Window
    {
        private double _offsetX;
        private double _offsetY;
        private Ellipse _ellipse;
        //private SineGenerator _generator;
        private AudioPlayer _audioPlayer;
        private Stream _fileStream;
        private MasteringVoice _masteringVoice;
        private XAudio2 _xaudio2;
        private object _lockAudio = new object();
        private bool _isConvolutionOn;
        List<double> leftHRTF_X;

        public HRTFWindow()
        {
            InitializeComponent();
            //_generator = new SineGenerator();
            leftHRTF_X = new List<double>();

            _isConvolutionOn = false;
            InitializeXAudio2();
            HeadPictureBackground();
            EllipseInitialization();
            ChartInit();
        }

        private void ChartInit()
        {
            // Все графики находятся в пределах области построения ChartArea, создадим ее
            chartR.ChartAreas.Add(new ChartArea("Default"));

            // Добавим линию, и назначим ее в ранее созданную область "Default"
            chartR.Series.Add(new Series("Series1"));
            chartR.Series["Series1"].ChartArea = "Default";
            chartR.Series["Series1"].ChartType = SeriesChartType.Line;

            chartR.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chartR.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chartR.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chartR.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

            // Все графики находятся в пределах области построения ChartArea, создадим ее
            chartL.ChartAreas.Add(new ChartArea("Default"));

            // Добавим линию, и назначим ее в ранее созданную область "Default"
            chartL.Series.Add(new Series("Series1"));
            chartL.Series["Series1"].ChartArea = "Default";
            chartL.Series["Series1"].ChartType = SeriesChartType.Line;

            chartL.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
            chartL.ChartAreas[0].AxisX.MinorGrid.Enabled = false;
            chartL.ChartAreas[0].AxisY.MajorGrid.Enabled = false;
            chartL.ChartAreas[0].AxisY.MinorGrid.Enabled = false;

            chartL.ChartAreas[0].AxisY.Maximum = 100;
            chartL.ChartAreas[0].AxisY.Minimum = -100;
            chartR.ChartAreas[0].AxisY.Maximum = 100;
            chartR.ChartAreas[0].AxisY.Minimum = -100;
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

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _audioPlayer.Stop();
        }

        private void AngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _offsetX = GetXPercent(e.NewValue);
            EllipseRedrow(_offsetY, _offsetX);
            //_generator.SetHorizontalPosition(e.NewValue);
            if (_audioPlayer != null)
            {
                _audioPlayer.SetHorizontalPosition((int)AngleSlider.Value);
            }
            if (ApplyConvolution.IsChecked == true)
            {
                _audioPlayer.SetConvolutionFunctions(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                Sandbox.SetConvolution(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
                    (int)AngleSlider.Value, Channel.Left), Channel.Left);
                ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
                  (int)AngleSlider.Value, Channel.Right), Channel.Right);
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
                ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
                  (int)AngleSlider.Value, Channel.Left), Channel.Left);
                ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
                  (int)AngleSlider.Value, Channel.Right), Channel.Right);
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


        private void Load_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Select an wav file..." };
            if (dialog.ShowDialog() == true)
            {
                _fileStream = new NativeFileStream(dialog.FileName, NativeFileMode.Open, NativeFileAccess.Read, NativeFileShare.Read);

                lock (_lockAudio)
                {
                    //if (_audioPlayer != null)
                    //{
                    //    _audioPlayer.Close();
                    //    _audioPlayer = null;
                    //}


                    _audioPlayer = new AudioPlayer(_xaudio2, _fileStream);
                    _audioPlayer.SetConvolutionMode(_isConvolutionOn);
                    _audioPlayer.SetHorizontalPosition((int)AngleSlider.Value);
                    if (_isConvolutionOn)
                    {
                        _audioPlayer.SetConvolutionFunctions(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                    }
                }
            }
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            lock (_lockAudio)
            {
                if (_audioPlayer != null)
                {
                    if (_audioPlayer.State == PlayerState.Playing)
                    {
                        _audioPlayer.Pause();
                    }
                }
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            lock (_lockAudio)
            {
                if (_audioPlayer != null)
                {
                    if (_audioPlayer.State == PlayerState.Paused ||
                        _audioPlayer.State == PlayerState.Stopped)
                    {
                        _audioPlayer.Play();
                    }
                }
            }
        }

        private void ReadSoundBytes(string fileName, Channel channel)
        {
            var fileStream = new NativeFileStream(fileName, NativeFileMode.Open, NativeFileAccess.Read);

            var soundStream = new SoundStream(fileStream);
            var dataStream = soundStream.ToDataStream();
            int k = 1;
            List<short> test = new List<short>();
            List<float> test2 = new List<float>();
            List<float> test3 = new List<float>();
            test.Add(0);
            test.Add(0);
            if (channel == Channel.Right)
                chartR.Series[0].Points.Clear();
            else
                chartL.Series[0].Points.Clear();

            while (dataStream.Length / sizeof(short) > k)
            {
                byte[] bytess = new byte[4];
                test[1] = dataStream.Read<short>();
                
                test2.Add(test[1]);
                var y = (100 * test[1]) /short.MaxValue;
                if (channel == Channel.Right)
                    chartR.Series[0].Points.AddY(y);
                else
                    chartL.Series[0].Points.AddY(y);
                
                k++;
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {

            //Sandbox.ApplyConvolutionChecked(true);
            //Sandbox.SetConvolution(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
            _isConvolutionOn = true;
            if (_audioPlayer != null)
            {
                _audioPlayer.SetConvolutionFunctions(GetFileStream(Channel.Left), GetFileStream(Channel.Right));
                _audioPlayer.SetConvolutionMode(_isConvolutionOn);
            }
            label.Content = ($"{ElevationSlider.Value} ---- {AngleSlider.Value}");
            ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
                (int)AngleSlider.Value, Channel.Left), Channel.Left);
            ReadSoundBytes(ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value,
              (int)AngleSlider.Value, Channel.Right), Channel.Right);
        }

        private void checkBox_Unchecked(object sender, RoutedEventArgs e)
        {
            _isConvolutionOn = false;
            if(_audioPlayer != null)
            {
                _audioPlayer.SetConvolutionMode(_isConvolutionOn);
            }
            Sandbox.ApplyConvolutionChecked(false);
        }

        private NativeFileStream GetFileStream(Channel channel) => new NativeFileStream
            (ConvolutionResourseReader.GetSoundPath((int)ElevationSlider.Value, (int)AngleSlider.Value, channel), NativeFileMode.Open, NativeFileAccess.Read);

        protected override void OnClosed(EventArgs e)
        {
            Utilities.Dispose(ref _masteringVoice);
            Utilities.Dispose(ref _xaudio2);
            base.OnClosed(e);

        }

        private void InitializeXAudio2()
        {
            // This is mandatory when using any of SharpDX.MediaFoundation classes
            MediaManager.Startup();

            // Starts The XAudio2 engine
            _xaudio2 = new XAudio2();
            _xaudio2.StartEngine();
            _masteringVoice = new MasteringVoice(_xaudio2);
        }
    }
}