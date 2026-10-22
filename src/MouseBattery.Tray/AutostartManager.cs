using System;
using Microsoft.Win32;
using System.Windows.Forms;

namespace MouseBattery.Tray
{
    internal static class AutostartManager
    {
        private const string RunKeyPath =
            @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string SettingsKeyPath =
            @"Software\VxeMouseBatteryTray";
        private const string RunValueName = "VxeMouseBatteryTray";
        private const string ConfiguredValueName = "AutostartConfigured";

        internal static bool IsEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
            {
                return key != null &&
                    key.GetValue(RunValueName) != null;
            }
        }

        internal static void ApplyDefaultOnFirstRun()
        {
            bool configured = false;

            using (RegistryKey settings = Registry.CurrentUser.OpenSubKey(
                SettingsKeyPath))
            {
                if (settings != null)
                {
                    configured =
                        Convert.ToInt32(
                            settings.GetValue(
                                ConfiguredValueName,
                                0)) != 0;
                }
            }

            if (!configured)
            {
                SetEnabled(true);
            }
            else if (IsEnabled())
            {
                // Update the stored path when the executable is moved.
                SetEnabled(true);
            }
        }

        internal static void SetEnabled(bool enabled)
        {
            using (RegistryKey runKey = Registry.CurrentUser.CreateSubKey(
                RunKeyPath))
            {
                if (enabled)
                {
                    string command = string.Format(
                        "\"{0}\" --autostart",
                        Application.ExecutablePath);
                    runKey.SetValue(RunValueName, command);
                }
                else
                {
                    runKey.DeleteValue(RunValueName, false);
                }
            }

            using (RegistryKey settings = Registry.CurrentUser.CreateSubKey(
                SettingsKeyPath))
            {
                settings.SetValue(
                    ConfiguredValueName,
                    1,
                    RegistryValueKind.DWord);
            }
        }
    }
}

