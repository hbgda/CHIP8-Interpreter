using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CHIPInterpreter;


namespace CHIP8_UI
{ 
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        internal CHIP_8? chip;
        internal Display? display;

        internal CancellationTokenSource ctSource = new CancellationTokenSource();
        internal CancellationToken ct;

        Dictionary<Key, byte> KeyMap = new Dictionary<Key, byte>
        {
            { Key.D1, 0x1 },{ Key.D2, 0x2 },{ Key.D3, 0x3 },{ Key.D4, 0xC },
            { Key.Q, 0x4 }, { Key.W, 0x5 }, { Key.E, 0x6 }, { Key.R, 0xD },
            { Key.A, 0x7 }, { Key.S, 0x8 }, { Key.D, 0x9 }, { Key.F, 0xE },
            { Key.Z, 0xA }, { Key.X, 0x0 }, { Key.C, 0xB }, { Key.V, 0xF },
        };

        public MainWindow()
        {
            InitializeComponent();
            display = new Display(CHIP_Display);
            chip = new CHIP_8(display.DrawFrame, null);
            OpenROM();
        }


        void InitializeCHIP8(string rom)
        {
            chip?.Load(File.ReadAllBytes(rom));

            ctSource = new CancellationTokenSource();
            ct = ctSource.Token;
            Task.Run(Loop);
        }

        readonly Stopwatch sw = Stopwatch.StartNew();
        readonly TimeSpan _60hz = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        readonly TimeSpan _frameTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);
        TimeSpan last;
        Task Loop()
        {

            while (!ct.IsCancellationRequested)
            {
                TimeSpan current = sw.Elapsed;
                TimeSpan elapsed = current - last;

                while (elapsed >= _60hz)
                {
                    this.Dispatcher.Invoke(() => chip?.Tick60hz());
                    elapsed -= _60hz;
                    last += _60hz;
                }

                this.Dispatcher.Invoke(() => chip?.Tick());
                Thread.Sleep(_frameTime);
            }
            return null;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (KeyMap.ContainsKey(e.Key))
            {
                chip?.KeyPressed(KeyMap[e.Key]);
            }
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (KeyMap.ContainsKey(e.Key))
            {
                chip?.KeyReleased(KeyMap[e.Key]);
            }
        }

        // Open ROM file dialog
        internal void OpenROM()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".ch8";
            dialog.Filter = "CHIP-8 ROM (*.ch8,*.c8;*.rom)|*.ch8;*.c8;*.rom";

            if(dialog.ShowDialog() == true)
            {
                string file = dialog.FileName;
                InitializeCHIP8(file);
            }
            else
            {
                MessageBox.Show("No file selected.");
                Application.Current.Shutdown();
            }
        }
    }
}
