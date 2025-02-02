using System;
using System.ComponentModel;
using System.Linq;
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
        private readonly NotificationManager _notificationManager;
        private readonly ProfileManager _profileManager;
        private AppConfig _config;
        private bool _isClosing;

        public MainWindow()
        {
            InitializeComponent();
            
            _hotkeyManager = new HotkeyManager();
            _config = AppConfig.Load();
            _notifyIcon = CreateNotifyIcon();
            _notificationManager = new NotificationManager(_notifyIcon, this, StatusBar, _config.NotificationSettings);
            _profileManager = new ProfileManager(_config, _hotkeyManager, _notificationManager);

            LoadSettings();
            LoadRefreshRates();
            LoadProfiles();
            UpdateFavoriteRates();

            if (_config.StartMinimized)
            {
                WindowState = WindowState.Minimized;
                if (_config.MinimizeToTray)
                    Hide();
            }

            if (!string.IsNullOrEmpty(_config.StartupProfile))
            {
                _profileManager.ApplyProfile(_config.StartupProfile);
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
            
            if (_config.TraySettings.ShowFavorites)
            {
                contextMenu.Items.Add("-");
                foreach (var rate in _config.TraySettings.FavoriteRefreshRates)
                {
                    contextMenu.Items.Add($"{rate} Гц", null, (s, e) => 
                    {
                        DisplaySettings.SetRefreshRate(rate);
                        _notificationManager.ShowNotification($"Частота обновления изменена на {rate} Гц");
                    });
                }
            }

            if (_config.TraySettings.ShowProfiles)
            {
                contextMenu.Items.Add("-");
                foreach (var profile in _config.Profiles.Where(p => p.IsEnabled))
                {
                    contextMenu.Items.Add($"Профиль: {profile.Name}", null, (s, e) => 
                        _profileManager.ApplyProfile(profile.Name));
                }
            }

            contextMenu.Items.Add("-");
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
            ShowToastsCheckBox.IsChecked = _config.NotificationSettings.ShowToasts;
            ShowStatusBarCheckBox.IsChecked = _config.NotificationSettings.ShowStatusBar;
            ToastDurationTextBox.Text = _config.NotificationSettings.ToastDuration.ToString();
            ShowFavoritesCheckBox.IsChecked = _config.TraySettings.ShowFavorites;
            ShowProfilesCheckBox.IsChecked = _config.TraySettings.ShowProfiles;
            ShowHotkeysCheckBox.IsChecked = _config.TraySettings.ShowHotkeys;

            StartupProfileComboBox.ItemsSource = _config.Profiles;
            if (!string.IsNullOrEmpty(_config.StartupProfile))
            {
                StartupProfileComboBox.SelectedItem = _config.Profiles
                    .FirstOrDefault(p => p.Name == _config.StartupProfile);
            }

            SourceInitialized += (s, e) =>
            {
                var handle = new WindowInteropHelper(this).Handle;
                HwndSource.FromHwnd(handle)?.AddHook(WndProc);

                foreach (var hotkey in _config.Hotkeys.Where(h => h.IsEnabled))
                {
                    RegisterHotkey(hotkey);
                }

                UpdateHotkeyList();
            };
        }

        private void LoadProfiles()
        {
            ProfilesListView.ItemsSource = null;
            ProfilesListView.ItemsSource = _config.Profiles;
        }

        private void UpdateFavoriteRates()
        {
            FavoriteRatesListBox.ItemsSource = null;
            FavoriteRatesListBox.ItemsSource = _config.TraySettings.FavoriteRefreshRates
                .Select(r => $"{r} Гц");

            AddFavoriteRateComboBox.ItemsSource = DisplaySettings.GetAvailableRefreshRates()
                .Except(_config.TraySettings.FavoriteRefreshRates);
        }

        private void RegisterHotkey(HotkeyConfig config)
        {
            var handle = new WindowInteropHelper(this).Handle;
            _hotkeyManager.RegisterHotkey(handle, config.Key, config.Modifiers,
                () => 
                {
                    DisplaySettings.SetRefreshRate(config.TargetRefreshRate);
                    _notificationManager.ShowNotification(
                        $"Частота обновления изменена на {config.TargetRefreshRate} Гц");
                });
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (_hotkeyManager.ProcessHotkey(hwnd, msg, wParam, lParam))
                handled = true;
            return IntPtr.Zero;
        }

        private void UpdateHotkeyList()
        {
            HotkeyListView.ItemsSource = _config.Hotkeys.Select(h => new HotkeyViewModel(h, this)).ToList();
        }

        public class HotkeyViewModel : INotifyPropertyChanged
        {
            private readonly HotkeyConfig _config;
            private readonly MainWindow _mainWindow;

            public string Клавиши => $"{_config.Modifiers}+{_config.Key}";
            public string Частота => $"{_config.TargetRefreshRate} Гц";
            
            public bool IsEnabled
            {
                get => _config.IsEnabled;
                set
                {
                    if (_config.IsEnabled != value)
                    {
                        _config.IsEnabled = value;
                        _mainWindow._config.Save();
                        
                        var handle = new WindowInteropHelper(_mainWindow).Handle;
                        _mainWindow._hotkeyManager.UnregisterAll(handle);
                        foreach (var hotkey in _mainWindow._config.Hotkeys.Where(h => h.IsEnabled))
                        {
                            _mainWindow.RegisterHotkey(hotkey);
                        }
                        
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                }
            }

            public HotkeyConfig Config => _config;

            public HotkeyViewModel(HotkeyConfig config, MainWindow mainWindow)
            {
                _config = config;
                _mainWindow = mainWindow;
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void EditHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var viewModel = (HotkeyViewModel)((FrameworkElement)button.Parent).DataContext;
            var index = _config.Hotkeys.IndexOf(viewModel.Config);

            if (index >= 0)
            {
                var dialog = new HotkeyDialog(viewModel.Config);
                if (dialog.ShowDialog() == true)
                {
                    var handle = new WindowInteropHelper(this).Handle;
                    UnregisterHotkey(handle, viewModel.Config);

                    _config.Hotkeys[index] = dialog.HotkeyConfig;
                    RegisterHotkey(dialog.HotkeyConfig);
                    UpdateHotkeyList();
                    _config.Save();

                    _notificationManager.ShowNotification("Горячая клавиша обновлена");
                }
            }
        }

        private void UnregisterHotkey(IntPtr handle, HotkeyConfig config)
        {
            _hotkeyManager.UnregisterAll(handle);
            foreach (var hotkey in _config.Hotkeys.Where(h => h.IsEnabled))
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

                _notificationManager.ShowNotification("Горячая клавиша добавлена");
            }
        }

        private void RemoveHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var viewModel = (HotkeyViewModel)((FrameworkElement)button.Parent).DataContext;
            
            _config.Hotkeys.Remove(viewModel.Config);
            UpdateHotkeyList();
            _config.Save();

            var handle = new WindowInteropHelper(this).Handle;
            _hotkeyManager.UnregisterAll(handle);

            foreach (var hotkey in _config.Hotkeys.Where(h => h.IsEnabled))
            {
                RegisterHotkey(hotkey);
            }

            _notificationManager.ShowNotification("Горячая клавиша удалена");
        }

        private void Settings_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            _config.MinimizeToTray = MinimizeToTrayCheckBox.IsChecked ?? false;
            _config.StartMinimized = StartMinimizedCheckBox.IsChecked ?? false;
            _config.StartWithWindows = StartWithWindowsCheckBox.IsChecked ?? false;

            UpdateStartWithWindows(_config.StartWithWindows);
            _config.Save();

            _notificationManager.ShowNotification("Настройки сохранены");
        }

        private void NotificationSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            _config.NotificationSettings.ShowToasts = ShowToastsCheckBox.IsChecked ?? true;
            _config.NotificationSettings.ShowStatusBar = ShowStatusBarCheckBox.IsChecked ?? true;
            _config.Save();

            _notificationManager.ShowNotification("Настройки уведомлений обновлены");
        }

        private void ToastDuration_Changed(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            if (int.TryParse(ToastDurationTextBox.Text, out int duration))
            {
                _config.NotificationSettings.ToastDuration = duration;
                _config.Save();
            }
        }

        private void TraySettings_Changed(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;

            _config.TraySettings.ShowFavorites = ShowFavoritesCheckBox.IsChecked ?? true;
            _config.TraySettings.ShowProfiles = ShowProfilesCheckBox.IsChecked ?? true;
            _config.TraySettings.ShowHotkeys = ShowHotkeysCheckBox.IsChecked ?? true;
            _config.Save();

            _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
            _notificationManager.ShowNotification("Настройки трея обновлены");
        }

        private void AddFavoriteRate_Click(object sender, RoutedEventArgs e)
        {
            if (AddFavoriteRateComboBox.SelectedItem != null)
            {
                var rate = (int)AddFavoriteRateComboBox.SelectedItem;
                _config.TraySettings.FavoriteRefreshRates.Add(rate);
                _config.Save();

                UpdateFavoriteRates();
                _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
                _notificationManager.ShowNotification($"Частота {rate} Гц добавлена в избранное");
            }
        }

        private void RemoveFavoriteRate_Click(object sender, RoutedEventArgs e)
        {
            if (FavoriteRatesListBox.SelectedItem != null)
            {
                var rateStr = (string)FavoriteRatesListBox.SelectedItem;
                var rate = int.Parse(rateStr.Replace(" Гц", ""));
                _config.TraySettings.FavoriteRefreshRates.Remove(rate);
                _config.Save();

                UpdateFavoriteRates();
                _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
                _notificationManager.ShowNotification($"Частота {rate} Гц удалена из избранного");
            }
        }

        private void CreateProfile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new ProfileDialog(DisplaySettings.GetAvailableRefreshRates().ToList());
            if (dialog.ShowDialog() == true)
            {
                _config.Profiles.Add(dialog.Profile);
                _config.Save();

                LoadProfiles();
                _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
                _notificationManager.ShowNotification($"Профиль '{dialog.Profile.Name}' создан");
            }
        }

        private void EditProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var profile = (Profile)((FrameworkElement)button.Parent).DataContext;
            var index = _config.Profiles.IndexOf(profile);

            if (index >= 0)
            {
                var dialog = new ProfileDialog(
                    DisplaySettings.GetAvailableRefreshRates().ToList(), 
                    profile);
                if (dialog.ShowDialog() == true)
                {
                    _config.Profiles[index] = dialog.Profile;
                    _config.Save();

                    LoadProfiles();
                    _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
                    _notificationManager.ShowNotification($"Профиль '{dialog.Profile.Name}' обновлен");
                }
            }
        }

        private void DeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var profile = (Profile)((FrameworkElement)button.Parent).DataContext;
            
            if (MessageBox.Show(
                $"Вы уверены, что хотите удалить профиль '{profile.Name}'?",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _config.Profiles.Remove(profile);
                _config.Save();

                LoadProfiles();
                _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
                _notificationManager.ShowNotification($"Профиль '{profile.Name}' удален");
            }
        }

        private void ProfileEnabled_Changed(object sender, RoutedEventArgs e)
        {
            _config.Save();
            _notifyIcon.ContextMenuStrip = CreateNotifyIcon().ContextMenuStrip;
        }

        private void StartupProfile_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            var profile = (Profile)StartupProfileComboBox.SelectedItem;
            _config.StartupProfile = profile?.Name ?? "";
            _config.Save();

            _notificationManager.ShowNotification(
                profile != null
                    ? $"Профиль '{profile.Name}' будет применяться при запуске"
                    : "Профиль при запуске отключен");
        }

        private void ApplyProfile_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var profile = (Profile)((FrameworkElement)button.Parent).DataContext;
            _profileManager.ApplyProfile(profile.Name);
        }

        private void ApplyRefreshRate_Click(object sender, RoutedEventArgs e)
        {
            if (RefreshRatesComboBox.SelectedItem != null)
            {
                var rate = (int)RefreshRatesComboBox.SelectedItem;
                DisplaySettings.SetRefreshRate(rate);
                _notificationManager.ShowNotification($"Частота обновления изменена на {rate} Гц");
            }
        }

        private void UpdateStartWithWindows(bool enable)
        {
            try
            {
                string startupPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutPath = System.IO.Path.Combine(startupPath, "MonitorRefreshRateSwitcher.lnk");
                string executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                if (enable)
                {
                    // Создаем ярлык через COM-объект
                    Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic shell = Activator.CreateInstance(shellType);
                    var shortcut = shell.CreateShortcut(shortcutPath);
                    
                    shortcut.TargetPath = executablePath;
                    shortcut.WorkingDirectory = System.IO.Path.GetDirectoryName(executablePath);
                    shortcut.Description = "Переключатель частоты обновления монитора";
                    shortcut.Save();

                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
                }
                else
                {
                    // Удаляем ярлык, если он существует
                    if (System.IO.File.Exists(shortcutPath))
                    {
                        System.IO.File.Delete(shortcutPath);
                    }
                }

                _config.StartWithWindows = enable;
            }
            catch (UnauthorizedAccessException)
            {
                _notificationManager.ShowNotification(
                    "Нет прав доступа к папке автозапуска",
                    NotificationType.Error);
                StartWithWindowsCheckBox.IsChecked = !enable;
            }
            catch (Exception ex)
            {
                _notificationManager.ShowNotification(
                    $"Не удалось обновить настройки автозапуска: {ex.Message}",
                    NotificationType.Error);
                StartWithWindowsCheckBox.IsChecked = !enable;
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
            var rates = DisplaySettings.GetAvailableRefreshRates();
            RefreshRatesComboBox.ItemsSource = rates;
            
            if (rates.Any())
            {
                RefreshRatesComboBox.SelectedItem = DisplaySettings.GetCurrentRefreshRate();
            }

            AddFavoriteRateComboBox.ItemsSource = rates;
        }

        private void RefreshRatesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || RefreshRatesComboBox.SelectedItem == null) return;

            var rate = (int)RefreshRatesComboBox.SelectedItem;
            DisplaySettings.SetRefreshRate(rate);
            _notificationManager.ShowNotification($"Частота обновления изменена на {rate} Гц");
        }
    }
}