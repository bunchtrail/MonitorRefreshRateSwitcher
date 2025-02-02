using System.Windows;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MonitorRefreshRateSwitcher.Models;
using MonitorRefreshRateSwitcher.Services;
using MonitorRefreshRateSwitcher.Config;

namespace MonitorRefreshRateSwitcher.Views
{
    public partial class HotkeyDialog : Window
    {
        private Key _selectedKey;
        private ModifierKeys _selectedModifiers;

        public HotkeyConfig HotkeyConfig { get; private set; }

        public HotkeyDialog()
        {
            InitializeComponent();
            LoadRefreshRates();
        }

        public HotkeyDialog(HotkeyConfig existingConfig) : this()
        {
            _selectedKey = existingConfig.Key;
            _selectedModifiers = existingConfig.Modifiers;
            UpdateHotkeyText();

            // Выбираем существующую частоту обновления
            var rates = (int[])RefreshRateComboBox.ItemsSource;
            for (int i = 0; i < rates.Length; i++)
            {
                if (rates[i] == existingConfig.TargetRefreshRate)
                {
                    RefreshRateComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void LoadRefreshRates()
        {
            var rates = DisplaySettings.GetAvailableRefreshRates();
            RefreshRateComboBox.ItemsSource = rates;
            if (rates.Length > 0)
                RefreshRateComboBox.SelectedIndex = 0;
        }

        private void HotkeyTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;

            // Получаем нажатые модификаторы
            _selectedModifiers = Keyboard.Modifiers;

            // Получаем основную клавишу
            if ((e.Key != Key.LeftCtrl) && (e.Key != Key.RightCtrl) &&
                (e.Key != Key.LeftAlt) && (e.Key != Key.RightAlt) &&
                (e.Key != Key.LeftShift) && (e.Key != Key.RightShift) &&
                (e.Key != Key.LWin) && (e.Key != Key.RWin) &&
                (e.Key != Key.System))
            {
                _selectedKey = e.Key;
            }

            // Обновляем текст в текстовом поле
            UpdateHotkeyText();
        }

        private void UpdateHotkeyText()
        {
            var text = "";

            if (_selectedModifiers.HasFlag(ModifierKeys.Control))
                text += "Ctrl + ";
            if (_selectedModifiers.HasFlag(ModifierKeys.Alt))
                text += "Alt + ";
            if (_selectedModifiers.HasFlag(ModifierKeys.Shift))
                text += "Shift + ";
            if (_selectedModifiers.HasFlag(ModifierKeys.Windows))
                text += "Win + ";

            if (_selectedKey != Key.None)
                text += _selectedKey.ToString();

            HotkeyTextBox.Text = text.TrimEnd(' ', '+');
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKey == Key.None || _selectedModifiers == ModifierKeys.None)
            {
                MessageBox.Show("Пожалуйста, укажите комбинацию клавиш",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (RefreshRateComboBox.SelectedItem == null)
            {
                MessageBox.Show("Пожалуйста, выберите частоту обновления",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            HotkeyConfig = new HotkeyConfig
            {
                Key = _selectedKey,
                Modifiers = _selectedModifiers,
                TargetRefreshRate = (int)RefreshRateComboBox.SelectedItem
            };

            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
} 