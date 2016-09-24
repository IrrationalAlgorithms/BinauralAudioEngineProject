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
    public partial class MainWindow : Window
    {
<<<<<<< HEAD
        private SoundGenerator soundGenerator;
=======
        SoundGenerator.SineGenerator _generator;
>>>>>>> ec471e4a2ae1c6c78d026cc46aaffe7b28999730

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
            CommandBindings.Add(new CommandBinding(dowmCommand, downCommandEvent));
            dowmCommand.InputGestures.Add(new KeyGesture(Key.Up));
            CommandBindings.Add(new CommandBinding(dowmCommand, upCommandEvent));
        }

        private void upCommandEvent(object sender, RoutedEventArgs e)
        {
            //handler code goes here. 
            var task = _generator.AddValue();
            task.Start();
        }

        private void downCommandEvent(object sender, RoutedEventArgs e)
        {
            //handler code goes here. 
            var task = _generator.Generate();
            task.Start();
        }
        private void startStop(object sender, RoutedEventArgs e)
        {
            new SoundGenerator.SineGenerator().Generate();
        }

    }
}
