using System;
using System.Runtime.InteropServices;

namespace MonitorRefreshRateSwitcher.Models
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct DEVMODE
    {
        private const int CCHDEVICENAME = 32;
        private const int CCHFORMNAME = 32;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHDEVICENAME)]
        public string dmDeviceName;
        public short dmSpecVersion;
        public short dmDriverVersion;
        public short dmSize;
        public short dmDriverExtra;
        public int dmFields;

        public int dmPositionX;
        public int dmPositionY;
        public int dmDisplayOrientation;
        public int dmDisplayFixedOutput;

        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCHFORMNAME)]
        public string dmFormName;
        public short dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public int dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    public class DisplaySettings
    {
        public const int ENUM_CURRENT_SETTINGS = -1;
        public const int CDS_UPDATEREGISTRY = 0x00000001;
        public const int CDS_TEST = 0x00000002;
        public const int DISP_CHANGE_SUCCESSFUL = 0;
        public const int DISP_CHANGE_RESTART = 1;
        public const int DISP_CHANGE_FAILED = -1;

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern bool EnumDisplaySettings(
            string lpszDeviceName,
            int iModeNum,
            ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        public static extern int ChangeDisplaySettingsEx(
            string lpszDeviceName,
            ref DEVMODE lpDevMode,
            IntPtr hwnd,
            int dwflags,
            IntPtr lParam);

        public static bool SetRefreshRate(int targetFrequency)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
            {
                return false;
            }

            int modeIndex = 0;
            bool modeFound = false;
            DEVMODE dmTemp = new DEVMODE();
            dmTemp.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            while (EnumDisplaySettings(null, modeIndex, ref dmTemp))
            {
                if (dmTemp.dmPelsWidth == dm.dmPelsWidth &&
                    dmTemp.dmPelsHeight == dm.dmPelsHeight &&
                    dmTemp.dmDisplayFrequency == targetFrequency)
                {
                    modeFound = true;
                    break;
                }
                modeIndex++;
            }

            if (!modeFound)
            {
                return false;
            }

            dm.dmDisplayFrequency = targetFrequency;
            dm.dmFields = 0x400000; // DM_DISPLAYFREQUENCY flag

            int result = ChangeDisplaySettingsEx(null, ref dm, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
            if (result == DISP_CHANGE_FAILED)
            {
                return false;
            }

            result = ChangeDisplaySettingsEx(null, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
            return result == DISP_CHANGE_SUCCESSFUL;
        }

        public static int[] GetAvailableRefreshRates()
        {
            var refreshRates = new System.Collections.Generic.HashSet<int>();
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            int modeIndex = 0;
            while (EnumDisplaySettings(null, modeIndex, ref dm))
            {
                refreshRates.Add(dm.dmDisplayFrequency);
                modeIndex++;
            }

            var result = new int[refreshRates.Count];
            refreshRates.CopyTo(result);
            Array.Sort(result);
            return result;
        }

        public static int GetCurrentRefreshRate()
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref dm))
            {
                return dm.dmDisplayFrequency;
            }

            return 60; // возвращаем значение по умолчанию в случае ошибки
        }
    }
} 