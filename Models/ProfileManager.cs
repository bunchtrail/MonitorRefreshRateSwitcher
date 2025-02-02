using System;
using System.Collections.Generic;
using System.Linq;
using MonitorRefreshRateSwitcher.Config;
using MonitorRefreshRateSwitcher.Services;

namespace MonitorRefreshRateSwitcher.Models
{
    public class ProfileManager
    {
        private readonly AppConfig _config;
        private readonly HotkeyManager _hotkeyManager;
        private readonly NotificationManager _notificationManager;
        private Profile _currentProfile;

        public ProfileManager(AppConfig config, HotkeyManager hotkeyManager, NotificationManager notificationManager)
        {
            _config = config;
            _hotkeyManager = hotkeyManager;
            _notificationManager = notificationManager;
        }

        public void ApplyProfile(string profileName)
        {
            var profile = _config.Profiles.FirstOrDefault(p => p.Name == profileName);
            if (profile == null || !profile.IsEnabled)
            {
                _notificationManager.ShowNotification(
                    $"Профиль '{profileName}' не найден или отключен",
                    NotificationType.Warning);
                return;
            }

            try
            {
                // Применяем частоту обновления
                DisplaySettings.SetRefreshRate(profile.RefreshRate);

                // Обновляем горячие клавиши
                _hotkeyManager.UnregisterAll(IntPtr.Zero);
                foreach (var hotkey in profile.Hotkeys.Where(h => h.IsEnabled))
                {
                    _hotkeyManager.RegisterHotkey(
                        IntPtr.Zero,
                        hotkey.Key,
                        hotkey.Modifiers,
                        () => DisplaySettings.SetRefreshRate(hotkey.TargetRefreshRate));
                }

                _currentProfile = profile;
                _notificationManager.ShowNotification(
                    $"Профиль '{profileName}' успешно применен",
                    NotificationType.Success);
            }
            catch (Exception ex)
            {
                _notificationManager.ShowNotification(
                    $"Ошибка при применении профиля: {ex.Message}",
                    NotificationType.Error);
            }
        }

        public void CreateProfile(string name, int refreshRate, List<HotkeyConfig> hotkeys)
        {
            if (_config.Profiles.Any(p => p.Name == name))
            {
                _notificationManager.ShowNotification(
                    $"Профиль с именем '{name}' уже существует",
                    NotificationType.Warning);
                return;
            }

            var profile = new Profile
            {
                Name = name,
                RefreshRate = refreshRate,
                Hotkeys = hotkeys,
                IsEnabled = true
            };

            _config.Profiles.Add(profile);
            _config.Save();

            _notificationManager.ShowNotification(
                $"Профиль '{name}' успешно создан",
                NotificationType.Success);
        }

        public void DeleteProfile(string name)
        {
            var profile = _config.Profiles.FirstOrDefault(p => p.Name == name);
            if (profile == null)
            {
                _notificationManager.ShowNotification(
                    $"Профиль '{name}' не найден",
                    NotificationType.Warning);
                return;
            }

            _config.Profiles.Remove(profile);
            _config.Save();

            _notificationManager.ShowNotification(
                $"Профиль '{name}' успешно удален",
                NotificationType.Success);
        }

        public Profile GetCurrentProfile() => _currentProfile;

        public List<Profile> GetProfiles() => _config.Profiles;
    }
} 