using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Drawing;
using System.Net.Mime;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Notifications.Wpf;
using System.Windows.Forms;
using Microsoft.Win32;
using Application = System.Windows.Forms.Application;

namespace EyesProtectApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly NotificationManager _notificationManager = new NotificationManager();
        private DispatcherTimer _timer;
        private DispatcherTimer _remainingTimer;
        private DateTime _startTime;
        private TimeSpan _remaining;
        NotifyIcon _notifyIcon = new NotifyIcon();
        SoundPlayer _player;

        public TimeSpan Remaining
        {
            get => _remaining;
            set
            {
                _remaining = value;
                OnPropertyChanged(nameof(Remaining));
            }
        }

        private void SetStartup()
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey
                ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (CbStartup.IsChecked != null && CbStartup.IsChecked.Value)
                rk.SetValue("EyesProtectApp", Application.ExecutablePath);
            else
                rk.DeleteValue("EyesProtectApp", false);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            
            _player = new SoundPlayer(FileStore.Resources.Resource.notif);
            _player.Load();

            _notifyIcon.Icon = FileStore.Resources.Resource.eye;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "EyesProtectApp";
            _notifyIcon.DoubleClick +=
                delegate (object sender, EventArgs args)
                {
                    this.Show();
                    this.WindowState = WindowState.Normal;
                };
            _notifyIcon.MouseDown += _notifyIcon_MouseDown;
            Closing += OnWindowClosing;

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            StartTimer(15);
            Closing += OnClosing;
            this.Hide();
        }

        private void _notifyIcon_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                System.Windows.Controls.ContextMenu menu = (System.Windows.Controls.ContextMenu)this.FindResource("NotifierContextMenu");
                menu.IsOpen = true;
            }
        }

        private void OnWindowClosing(object sender, CancelEventArgs e)
        {
            this.Hide();
            e.Cancel = true;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == System.Windows.WindowState.Minimized)
                this.Hide();
            base.OnStateChanged(e);
        }

        private void Timer_Elapsed(object sender, EventArgs e)
        {
            _startTime = DateTime.Now;
            _notificationManager.Show("Look away");
            _player.Play();
        }

        public static double ConvertMinutesToMilliseconds(double minutes)
        {
            return TimeSpan.FromMinutes(minutes).TotalMilliseconds;
        }

        private void StopTimer()
        {
            _timer?.Stop();
            _remainingTimer?.Stop();
        }

        private void StartTimer(int interval)
        {
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMinutes(interval);
            _timer.Tick += Timer_Elapsed;
            _remainingTimer = new DispatcherTimer();
            _remainingTimer.Interval = TimeSpan.FromMilliseconds(500);
            _remainingTimer.Tick += (sender, args) =>
            {
                var time = DateTime.Now - _startTime;
                var currentMilis = time.TotalMilliseconds;
                var intervalMilis = TimeSpan.FromMinutes(interval).TotalMilliseconds;
                //var remainingMillis = intervalMilis - currentMilis;
                Remaining = TimeSpan.FromMinutes(interval).Subtract(time);
                LbRemaining.Content = Remaining.ToString(@"mm\:ss");
            };
            _timer.Start();
            _remainingTimer.Start();
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            StopTimer();
        }

        private void BtStart_OnClick(object sender, RoutedEventArgs e)
        {
            if(!_timer.IsEnabled)
            {
                if (String.IsNullOrEmpty(TbInterval.Text))
                    System.Windows.MessageBox.Show("Empty interval", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    var isNumeric = int.TryParse(TbInterval.Text, out int interval);
                    if (isNumeric)
                    {
                        StartTimer(interval);
                        LbStatus.Content = "Running";
                    }
                    else
                        System.Windows.MessageBox.Show("Error parsing interval", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtStop_OnClick(object sender, RoutedEventArgs e)
        {
            if (_timer.IsEnabled)
            {
                StopTimer();
                LbStatus.Content = "Stopped";
            }
        }
        
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        private void MenuItem_Close(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void ChkStartUp_OnChecked(object sender, RoutedEventArgs e)
        {
            this.SetStartup();
        }
    }
}
