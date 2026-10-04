using System;
using System.Security;
using System.Windows.Forms;
using Microsoft.Win32;

namespace EbenTilerWindows
{
    /// <summary>Windows 시작 시 자동 실행 등록을 켜고 끈다. 현재 사용자 계정에만 적용된다.</summary>
    public static class Startup
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "Tessdeck";
        private const string LegacyValueName = "EbenTiler";

        private static string ExpectedValue
        {
            get { return "\"" + Application.ExecutablePath + "\""; }
        }

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null) return false;
                    object value = key.GetValue(ValueName);
                    string stored = value as string;
                    return !string.IsNullOrEmpty(stored)
                        && string.Equals(stored, ExpectedValue, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
        }

        public static bool SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath))
                {
                    if (key == null) return false;

                    if (enabled)
                    {
                        key.SetValue(ValueName, ExpectedValue, RegistryValueKind.String);
                        key.DeleteValue(LegacyValueName, false);
                        string stored = key.GetValue(ValueName) as string;
                        return string.Equals(stored, ExpectedValue, StringComparison.OrdinalIgnoreCase);
                    }

                    key.DeleteValue(ValueName, false);
                    key.DeleteValue(LegacyValueName, false);
                    return key.GetValue(ValueName) == null && key.GetValue(LegacyValueName) == null;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (SecurityException)
            {
                return false;
            }
        }
    }
}
