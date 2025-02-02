using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Input;

namespace MonitorRefreshRateSwitcher
{
    public class HotkeyConfig
    {
        public Key Key { get; set; }
        public ModifierKeys Modifiers { get; set; }
        public int TargetRefreshRate { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public class Profile
    {
        public string Name { get; set; } = "";
        public int RefreshRate { get; set; }
        public List<HotkeyConfig> Hotkeys { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
    }

    public class NotificationSettings
    {
        public bool ShowToasts { get; set; } = true;
        public bool ShowStatusBar { get; set; } = true;
        public int ToastDuration { get; set; } = 3000;
        public bool IsEnabled { get; set; } = true;
    }

    public class TraySettings
    {
        public bool ShowFavorites { get; set; } = true;
        public List<int> FavoriteRefreshRates { get; set; } = new();
        public bool ShowProfiles { get; set; } = true;
        public bool ShowHotkeys { get; set; } = true;
        public bool IsEnabled { get; set; } = true;
    }

    public class AppConfig
    {
        public List<HotkeyConfig> Hotkeys { get; set; } = new();
        public List<Profile> Profiles { get; set; } = new();
        public NotificationSettings NotificationSettings { get; set; } = new();
        public TraySettings TraySettings { get; set; } = new();
        public bool MinimizeToTray { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;
        public string StartupProfile { get; set; } = "";
        public bool IsEnabled { get; set; } = true;

        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MonitorRefreshRateSwitcher",
            "config.json");

        public static AppConfig Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AppConfig>(json) ?? CreateDefault();
                }
            }
            catch (Exception)
            {
                // В случае ошибки загрузки возвращаем конфигурацию по умолчанию
            }

            return CreateDefault();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception)
            {
                // Логирование ошибки сохранения конфигурации
            }
        }

        private static AppConfig CreateDefault()
        {
            return new AppConfig
            {
                Hotkeys = new List<HotkeyConfig>
                {
                    new HotkeyConfig { Key = Key.D1, Modifiers = ModifierKeys.Alt | ModifierKeys.Control, TargetRefreshRate = 60, IsEnabled = true },
                    new HotkeyConfig { Key = Key.D2, Modifiers = ModifierKeys.Alt | ModifierKeys.Control, TargetRefreshRate = 144, IsEnabled = true }
                },
                Profiles = new List<Profile>
                {
                    new Profile 
                    { 
                        Name = "Игровой",
                        RefreshRate = 144,
                        Hotkeys = new List<HotkeyConfig>()
                    },
                    new Profile 
                    { 
                        Name = "Офисный",
                        RefreshRate = 60,
                        Hotkeys = new List<HotkeyConfig>()
                    }
                },
                NotificationSettings = new NotificationSettings(),
                TraySettings = new TraySettings 
                { 
                    FavoriteRefreshRates = new List<int> { 60, 144 } 
                },
                MinimizeToTray = true,
                StartMinimized = true,
                StartWithWindows = false,
                IsEnabled = true
            };
        }
    }
} 