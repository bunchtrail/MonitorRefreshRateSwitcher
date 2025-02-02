using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MonitorRefreshRateSwitcher
{
    public partial class ProfileDialog : Window
    {
        private readonly Profile _profile;
        private readonly List<int> _availableRefreshRates;

        public Profile Profile => _profile;

        public ProfileDialog(List<int> availableRefreshRates, Profile profile = null)
        {
            InitializeComponent();
            
            _availableRefreshRates = availableRefreshRates;
            _profile = profile ?? new Profile();

            LoadRefreshRates();
            LoadProfile();
        }

        private void LoadRefreshRates()
        {
            RefreshRateComboBox.ItemsSource = _availableRefreshRates;
            if (_profile.RefreshRate > 0)
            {
                RefreshRateComboBox.SelectedItem = _profile.RefreshRate;
            }
            else if (RefreshRateComboBox.Items.Count > 0)
            {
                RefreshRateComboBox.SelectedIndex = 0;
            }
        }

        private void LoadProfile()
        {
            ProfileNameTextBox.Text = _profile.Name;
            UpdateHotkeyList();
        }

        private void UpdateHotkeyList()
        {
            HotkeyListView.ItemsSource = null;
            HotkeyListView.ItemsSource = _profile.Hotkeys.Select(h => new
            {
                Клавиши = $"{h.Modifiers}+{h.Key}",
                Частота = $"{h.TargetRefreshRate} Гц",
                h.IsEnabled,
                Config = h
            });
        }

        private void AddHotkey_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new HotkeyDialog();
            if (dialog.ShowDialog() == true)
            {
                _profile.Hotkeys.Add(dialog.HotkeyConfig);
                UpdateHotkeyList();
            }
        }

        private void EditHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var item = (dynamic)((FrameworkElement)button.Parent).DataContext;
            var hotkeyConfig = (HotkeyConfig)item.Config;
            var index = _profile.Hotkeys.IndexOf(hotkeyConfig);

            if (index >= 0)
            {
                var dialog = new HotkeyDialog(hotkeyConfig);
                if (dialog.ShowDialog() == true)
                {
                    _profile.Hotkeys[index] = dialog.HotkeyConfig;
                    UpdateHotkeyList();
                }
            }
        }

        private void RemoveHotkey_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var item = (dynamic)((FrameworkElement)button.Parent).DataContext;
            var hotkeyConfig = (HotkeyConfig)item.Config;
            
            _profile.Hotkeys.Remove(hotkeyConfig);
            UpdateHotkeyList();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProfileNameTextBox.Text))
            {
                System.Windows.MessageBox.Show(
                    "Введите название профиля",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (RefreshRateComboBox.SelectedItem == null)
            {
                System.Windows.MessageBox.Show(
                    "Выберите частоту обновления",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            _profile.Name = ProfileNameTextBox.Text;
            _profile.RefreshRate = (int)RefreshRateComboBox.SelectedItem;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 