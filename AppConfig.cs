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
    }

    public class AppConfig
    {
        public List<HotkeyConfig> Hotkeys { get; set; } = new();
        public bool MinimizeToTray { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
        public bool StartWithWindows { get; set; } = false;

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
                    new HotkeyConfig { Key = Key.D1, Modifiers = ModifierKeys.Alt | ModifierKeys.Control, TargetRefreshRate = 60 },
                    new HotkeyConfig { Key = Key.D2, Modifiers = ModifierKeys.Alt | ModifierKeys.Control, TargetRefreshRate = 144 }
                },
                MinimizeToTray = true,
                StartMinimized = true,
                StartWithWindows = false
            };
        }
    }
} 