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
using Microsoft.DirectX.DirectSound;
using Microsoft.DirectX.Direct3D;
using System.Windows.Interop;
using System.Windows.Forms;

namespace Mixer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window { 

        private SoundGenerator.SineGenerator _generator;

        public MainWindow()
        {
            InitializeComponent();
            _generator = new SoundGenerator.SineGenerator();
            Loaded += new RoutedEventHandler(Window_Loaded);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RoutedCommand dowmCommand = new RoutedCommand();
            dowmCommand.InputGestures.Add(new KeyGesture(Key.Down));
            CommandBindings.Add(new CommandBinding(dowmCommand, rateUp));
            dowmCommand.InputGestures.Add(new KeyGesture(Key.Up));
            CommandBindings.Add(new CommandBinding(dowmCommand, rateDown));
        }

        private void rateUp(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
             _generator.AddValue(10);

        }
        private void rateDown(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
           _generator.AddValue(-10);
            
        }

        private void ampUp(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValueAmp(+10);
        }

        private void ampDown(object sender, RoutedEventArgs e)
        {
            //handler code goes here.
            _generator.AddValueAmp(-10);
        }

        private async void startStop(object sender, RoutedEventArgs e)
        {
            var btn = (System.Windows.Controls.Button)sender;
            if (btn.Content.ToString() == "Start")
            {
                btn.Content = "Stop";
                _generator.Play();
            }
            else
            {
                btn.Content = "Start";
                _generator.Pause();
            }
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var log = _generator.PrintLog();
            foreach (var item in log)
                listBox.Items.Add(item);
        }
    }
}
