using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace RectangleWindows
{
    /// <summary>Windows 시작 시 자동 실행 등록을 켜고 끈다. 현재 사용자 계정에만 적용된다.</summary>
    public static class Startup
    {
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "RectangleWindows";

        public static bool IsEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    object value = key.GetValue(ValueName);
                    return value != null;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void SetEnabled(bool enabled)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
                {
                    if (key == null)
                    {
                        return;
                    }

                    if (enabled)
                    {
                        key.SetValue(ValueName, "\"" + Application.ExecutablePath + "\"");
                    }
                    else
                    {
                        key.DeleteValue(ValueName, false);
                    }
                }
            }
            catch (Exception)
            {
                // 레지스트리 접근이 막혀 있으면 조용히 넘어간다. 기능 자체는 계속 쓸 수 있다.
            }
        }
    }
}
