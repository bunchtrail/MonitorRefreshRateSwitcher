using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Forms = System.Windows.Forms;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using Button = System.Windows.Controls.Button;

namespace MonitorRefreshRateSwitcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly HotkeyManager _hotkeyManager;
        private readonly Forms.NotifyIcon _notifyIcon;
        private AppConfig _config;
        private bool _isClosing;

        public MainWindow()
        {
            InitializeComponent();
            
            _hotkeyManager = new HotkeyManager();
            _config = AppConfig.Load();
            _notifyIcon = CreateNotifyIcon();

            LoadSettings();
            LoadRefreshRates();

            if (_config.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                if (_config.MinimizeToTray)
                    Hide();
            }
        }

        private Forms.NotifyIcon CreateNotifyIcon()
        {
            var notifyIcon = new Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "Переключатель частоты обновления монитора",
                Visible = true
            };

            var contextMenu = new Forms.ContextMenuStrip();
            contextMenu.Items.Add("Открыть", null, (s, e) => ShowMainWindow());
            contextMenu.Items.Add("-"); // Разделитель
            contextMenu.Items.Add("60 Гц", null, (s, e) => DisplaySettings.SetRefreshRate(60));
            contextMenu.Items.Add("144 Гц", null, (s, e) => DisplaySettings.SetRefreshRate(144));
            contextMenu.Items.Add("-"); // Разделитель
            contextMenu.Items.Add("Выход", null, (s, e) => { _isClosing = true; Close(); });

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => ShowMainWindow();

            return notifyIcon;
        }

        private void ShowMainWindow()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void LoadSettings()
        {
            MinimizeToTrayCheckBox.IsChecked = _config.MinimizeToTray;
            StartMinimizedCheckBox.IsChecked = _config.StartMinimized;
            StartWithWindowsCheckBox.IsChecked = _config.StartWithWindows;

            SourceInitialized += (s, e) =>
            {
                var handle = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WndProc);

                foreach (var hotkey in _config.Hotkeys)
                {
                    RegisterHotkey(hotkey);
                }

                UpdateHotkeyList();
            };
        }

        private void RegisterHotkey(HotkeyConfig config)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _hotkeyManager.RegisterHotkey(handle, config.Key, config.Modifiers,
                () => DisplaySettings.SetRefreshRate(config.TargetRefreshRate));
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_hotkeyManager.ProcessHotkey(hwnd, msg, wParam, lParam))
                handled = true;
            return IntPtr.Zero;
        }

        private void UpdateHotkeyList()
        {
            HotkeyListView.Items.Clear();
            foreach (var hotkey in _config.Hotkeys)
            {
                HotkeyListView.Items.Add(new
                {
                    Клавиши = $"{hotkey.Modifiers}+{hotkey.Key}",
                    Частота = $"{hotkey.TargetRefreshRate} Гц",
                    Config = hotkey
                });
            }
        }

        private void EditHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var item = (dynamic)((FrameworkElement)button.Parent).DataContext;
            var hotkeyConfig = (HotkeyConfig)item.Config;
            var index = _config.Hotkeys.IndexOf(hotkeyConfig);

            if (index >= 0)
            {
                var dialog = new HotkeyDialog(hotkeyConfig);
                if (dialog.ShowDialog() == true)
                {
                    var handle = new WindowInteropHelper(this).Handle;
                    UnregisterHotkey(handle, hotkeyConfig);

                    _config.Hotkeys[index] = dialog.HotkeyConfig;
                    RegisterHotkey(dialog.HotkeyConfig);
                    UpdateHotkeyList();
                    _config.Save();
                }
            }
        }

        private void UnregisterHotkey(IntPtr handle, HotkeyConfig config)
        {
            _hotkeyManager.UnregisterAll(handle);
            foreach (var hotkey in _config.Hotkeys)
            {
                if (hotkey != config)
                {
                    RegisterHotkey(hotkey);
                }
            }
        }

        private void AddHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyDialog();
            if (dialog.ShowDialog() == true)
            {
                _config.Hotkeys.Add(dialog.HotkeyConfig);
                RegisterHotkey(dialog.HotkeyConfig);
                UpdateHotkeyList();
                _config.Save();
            }
        }

        private void RemoveHotkey_Click(object sender, RoutedEventArgs e)
        {
            if (HotkeyListView.SelectedIndex >= 0)
            {
                _config.Hotkeys.RemoveAt(HotkeyListView.SelectedIndex);
                UpdateHotkeyList();
                _config.Save();

                var handle = new WindowInteropHelper(this).Handle;
                _hotkeyManager.UnregisterAll(handle);

                foreach (var hotkey in _config.Hotkeys)
                {
                    RegisterHotkey(hotkey);
                }
            }
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            _config.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
            _config.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
            _config.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;

            UpdateStartWithWindows(_config.StartWithWindows);
            _config.Save();
        }

        private void UpdateStartWithWindows(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

                if (key != null)
                {
                    if (enable)
                    {
                        key.SetValue("MonitorRefreshRateSwitcher", 
                            System.Reflection.Assembly.GetExecutingAssembly().Location);
                    }
                    else
                    {
                        key.DeleteValue("MonitorRefreshRateSwitcher", false);
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Не удалось обновить настройки автозапуска",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && _config.MinimizeToTray)
            {
                Hide();
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!_isClosing)
            {
                e.Cancel = true;
                WindowState = WindowState.Minimized;
                if (_config.MinimizeToTray)
                    Hide();
                return;
            }

            var handle = new WindowInteropHelper(this).Handle;
            _hotkeyManager.UnregisterAll(handle);
            _notifyIcon.Dispose();
        }

        private void LoadRefreshRates()
        {
            try
            {
                var refreshRates = DisplaySettings.GetAvailableRefreshRates();
                RefreshRatesComboBox.ItemsSource = refreshRates;
                
                if (refreshRates.Length > 0)
                {
                    RefreshRatesComboBox.SelectedIndex = 0;
                    StatusTextBlock.Text = "Доступные частоты обновления загружены успешно.";
                }
                else
                {
                    StatusTextBlock.Text = "Не удалось найти доступные частоты обновления.";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"Ошибка при загрузке частот обновления: {ex.Message}";
            }
        }

        private void RefreshRatesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RefreshRatesComboBox.SelectedItem is int selectedRate)
            {
                try
                {
                    if (DisplaySettings.SetRefreshRate(selectedRate))
                    {
                        StatusTextBlock.Text = $"Частота обновления успешно изменена на {selectedRate} Гц";
                    }
                    else
                    {
                        StatusTextBlock.Text = $"Не удалось установить частоту {selectedRate} Гц";
                    }
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"Ошибка при изменении частоты обновления: {ex.Message}";
                }
            }
        }
    }
}