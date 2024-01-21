using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace AW2.UI
{
    /// <summary>
    /// See http://stackoverflow.com/questions/734618/disabling-accessibility-shortcuts-in-net-application
    /// </summary>
    public class AccessibilityShortcuts
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SKEY
        {
            public uint cbSize;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct FILTERKEY
        {
            public uint cbSize;
            public uint dwFlags;
            public uint iWaitMSec;
            public uint iDelayMSec;
            public uint iRepeatMSec;
            public uint iBounceMSec;
        }

        private const uint SPI_GETFILTERKEYS = 0x0032;
        private const uint SPI_SETFILTERKEYS = 0x0033;
        private const uint SPI_GETTOGGLEKEYS = 0x0034;
        private const uint SPI_SETTOGGLEKEYS = 0x0035;
        private const uint SPI_GETSTICKYKEYS = 0x003A;
        private const uint SPI_SETSTICKYKEYS = 0x003B;

        private static bool StartupAccessibilitySet = false;
        private static SKEY StartupStickyKeys;
        private static SKEY StartupToggleKeys;
        private static FILTERKEY StartupFilterKeys;

        private const uint SKF_STICKYKEYSON = 0x00000001;
        private const uint TKF_TOGGLEKEYSON = 0x00000001;
        private const uint FKF_FILTERKEYSON = 0x00000001;
        private const uint SKF_HOTKEYACTIVE = 0x00000004;
        private const uint TKF_HOTKEYACTIVE = 0x00000004;
        private const uint FKF_HOTKEYACTIVE = 0x00000004;
        private const uint SKF_CONFIRMHOTKEY = 0x00000008;
        private const uint TKF_CONFIRMHOTKEY = 0x00000008;
        private const uint FKF_CONFIRMHOTKEY = 0x00000008;

        private const uint SKEY_SIZE = sizeof(uint) * 2;
        private const uint FKEY_SIZE = sizeof(uint) * 6;

        public static void ToggleAccessibilityShortcutKeys(bool returnToStarting)
        {
            if (!StartupAccessibilitySet)
            {
                StartupStickyKeys.cbSize = SKEY_SIZE;
                StartupToggleKeys.cbSize = SKEY_SIZE;
                StartupFilterKeys.cbSize = FKEY_SIZE;
                SystemParametersInfo(SPI_GETSTICKYKEYS, SKEY_SIZE, ref StartupStickyKeys, 0);
                SystemParametersInfo(SPI_GETTOGGLEKEYS, SKEY_SIZE, ref StartupToggleKeys, 0);
                SystemParametersInfo(SPI_GETFILTERKEYS, FKEY_SIZE, ref StartupFilterKeys, 0);
                StartupAccessibilitySet = true;
            }

            if (returnToStarting)
            {
                // Restore StickyKeys/etc to original state and enable Windows key
                SystemParametersInfo(SPI_SETSTICKYKEYS, SKEY_SIZE, ref StartupStickyKeys, 0);
                SystemParametersInfo(SPI_SETTOGGLEKEYS, SKEY_SIZE, ref StartupToggleKeys, 0);
                SystemParametersInfo(SPI_SETFILTERKEYS, FKEY_SIZE, ref StartupFilterKeys, 0);
            }
            else
            {
                // Disable StickyKeys/etc shortcuts but if the accessibility feature is on, 
                // then leave the settings alone as its probably being usefully used
                SKEY skOff = StartupStickyKeys;
                if ((skOff.dwFlags & SKF_STICKYKEYSON) == 0)
                {
                    // Disable the hotkey and the confirmation
                    skOff.dwFlags &= ~SKF_HOTKEYACTIVE;
                    skOff.dwFlags &= ~SKF_CONFIRMHOTKEY;

                    SystemParametersInfo(SPI_SETSTICKYKEYS, SKEY_SIZE, ref skOff, 0);
                }
                SKEY tkOff = StartupToggleKeys;
                if ((tkOff.dwFlags & TKF_TOGGLEKEYSON) == 0)
                {
                    // Disable the hotkey and the confirmation
                    tkOff.dwFlags &= ~TKF_HOTKEYACTIVE;
                    tkOff.dwFlags &= ~TKF_CONFIRMHOTKEY;

                    SystemParametersInfo(SPI_SETTOGGLEKEYS, SKEY_SIZE, ref tkOff, 0);
                }

                FILTERKEY fkOff = StartupFilterKeys;
                if ((fkOff.dwFlags & FKF_FILTERKEYSON) == 0)
                {
                    // Disable the hotkey and the confirmation
                    fkOff.dwFlags &= ~FKF_HOTKEYACTIVE;
                    fkOff.dwFlags &= ~FKF_CONFIRMHOTKEY;

                    SystemParametersInfo(SPI_SETFILTERKEYS, FKEY_SIZE, ref fkOff, 0);
                }
            }
        }

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = false)]
        private static extern bool SystemParametersInfo(uint action, uint param, ref SKEY vparam, uint init);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo", SetLastError = false)]
        private static extern bool SystemParametersInfo(uint action, uint param, ref FILTERKEY vparam, uint init);
    }
}
