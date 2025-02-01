using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Input;

namespace MonitorRefreshRateSwitcher
{
    public class HotkeyManager
    {
        private const int WM_HOTKEY = 0x0312;
        private Dictionary<int, Action> _hotkeyActions = new();
        private int _currentId = 0;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public bool RegisterHotkey(IntPtr handle, Key key, ModifierKeys modifiers, Action action)
        {
            try
            {
                _currentId++;
                uint modifierFlags = 0;

                if (modifiers.HasFlag(ModifierKeys.Alt))
                    modifierFlags |= 0x0001;
                if (modifiers.HasFlag(ModifierKeys.Control))
                    modifierFlags |= 0x0002;
                if (modifiers.HasFlag(ModifierKeys.Shift))
                    modifierFlags |= 0x0004;
                if (modifiers.HasFlag(ModifierKeys.Windows))
                    modifierFlags |= 0x0008;

                if (RegisterHotKey(handle, _currentId, modifierFlags, (uint)KeyInterop.VirtualKeyFromKey(key)))
                {
                    _hotkeyActions[_currentId] = action;
                    return true;
                }
            }
            catch
            {
                // Логирование ошибки регистрации горячей клавиши
            }

            return false;
        }

        public void UnregisterAll(IntPtr handle)
        {
            foreach (var id in _hotkeyActions.Keys)
            {
                UnregisterHotKey(handle, id);
            }
            _hotkeyActions.Clear();
        }

        public bool ProcessHotkey(IntPtr handle, int msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_HOTKEY && _hotkeyActions.TryGetValue(wParam.ToInt32(), out var action))
            {
                action?.Invoke();
                return true;
            }
            return false;
        }
    }
} 