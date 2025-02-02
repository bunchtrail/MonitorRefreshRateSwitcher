using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Forms = System.Windows.Forms;

namespace MonitorRefreshRateSwitcher
{
    public enum NotificationType
    {
        Success,
        Warning,
        Error
    }

    public class NotificationManager
    {
        private readonly Forms.NotifyIcon _notifyIcon;
        private readonly Window _mainWindow;
        private readonly TextBlock _statusBar;
        private readonly NotificationSettings _settings;

        public NotificationManager(Forms.NotifyIcon notifyIcon, Window mainWindow, TextBlock statusBar, NotificationSettings settings)
        {
            _notifyIcon = notifyIcon;
            _mainWindow = mainWindow;
            _statusBar = statusBar;
            _settings = settings;
        }

        public void ShowNotification(string message, NotificationType type = NotificationType.Success)
        {
            if (!_settings.IsEnabled) return;

            if (_settings.ShowToasts)
            {
                var title = type switch
                {
                    NotificationType.Success => "Успешно",
                    NotificationType.Warning => "Предупреждение",
                    NotificationType.Error => "Ошибка",
                    _ => "Уведомление"
                };

                _notifyIcon.ShowBalloonTip(
                    _settings.ToastDuration,
                    title,
                    message,
                    type switch
                    {
                        NotificationType.Success => Forms.ToolTipIcon.Info,
                        NotificationType.Warning => Forms.ToolTipIcon.Warning,
                        NotificationType.Error => Forms.ToolTipIcon.Error,
                        _ => Forms.ToolTipIcon.None
                    }
                );
            }

            if (_settings.ShowStatusBar)
            {
                UpdateStatusBar(message, type);
            }
        }

        private void UpdateStatusBar(string message, NotificationType type)
        {
            if (_statusBar == null) return;

            _statusBar.Text = message;
            _statusBar.Foreground = type switch
            {
                NotificationType.Success => new SolidColorBrush(Colors.Green),
                NotificationType.Warning => new SolidColorBrush(Colors.Orange),
                NotificationType.Error => new SolidColorBrush(Colors.Red),
                _ => new SolidColorBrush(Colors.Black)
            };

            var animation = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(_settings.ToastDuration),
                BeginTime = TimeSpan.FromMilliseconds(2000)
            };

            animation.Completed += (s, e) => _statusBar.Text = "";
            _statusBar.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }
} 