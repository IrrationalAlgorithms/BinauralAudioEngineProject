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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Forms;
using System.ComponentModel;

namespace Mixer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private SoundGenerator.SineGenerator _generator;
        private SoundGenerator.WavWorker _wavWorker;

        private string _mode;
        public string Mode
        {
            get { return _mode; }
            set
            {
                _mode = value;
                OnPropertyChanged("Mode");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public MainWindow()
        {
            InitializeComponent();
            _generator = new SoundGenerator.SineGenerator();
            _wavWorker = new SoundGenerator.WavWorker();
            Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Mode = "Start";
            InitializeComponent();
            DataContext = this;
        }

        protected override void OnClosed(EventArgs e)
        {
            _generator.Close();
            base.OnClosed(e);
        }


        private void RateUp(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValue(10);

        }
        private void RateDown(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValue(-10);

        }

        private void AmpUp(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValueAmp(+10);
        }

        private void AmpDown(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValueAmp(-10);
        }

        private void StartStop(object sender, RoutedEventArgs e)
        {

            if (Mode == "Start")
            {
                Mode = "Stop";
                _generator.Play();
            }
            else
            {
                Mode = "Start";
                _generator.Pause();
            }
        }

        private void Print(object sender, RoutedEventArgs e)
        {
            var log = _generator.PrintLog();
            foreach (var item in log)
                listBox.Items.Add(item);
        }

        private void PlaySound1(object sender, RoutedEventArgs e)
        {
            _wavWorker.PlayWav(SoundGenerator.WavSample.test);
        }

        private void PlaySound2(object sender, RoutedEventArgs e)
        {
            _wavWorker.PlayWav(SoundGenerator.WavSample.alesis);
        }
    }
}
