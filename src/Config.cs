using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EbenTilerWindows
{
    /// <summary>단축키와 옵션을 %APPDATA%\Tessdeck\config.ini 에 읽고 쓴다.</summary>
    public sealed class Config
    {
        public Dictionary<SnapAction, Hotkey> Hotkeys;
        public Hotkey QuickLayoutSaveHotkey;
        public Hotkey QuickLayoutRestoreHotkey;
        public int Gap;
        public bool CycleHalves;
        public int CycleRatio1;
        public int CycleRatio2;
        public int CycleRatio3;
        public bool ShowWelcomeGuide;

        public Config()
        {
            Hotkeys = new Dictionary<SnapAction, Hotkey>();
            QuickLayoutSaveHotkey = new Hotkey();
            QuickLayoutRestoreHotkey = new Hotkey();
            Gap = 0;
            CycleHalves = true;
            CycleRatio1 = 50;
            CycleRatio2 = 33;
            CycleRatio3 = 67;
            ShowWelcomeGuide = true;
        }

        public static string Directory
        {
            get
            {
                string baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string directory = Path.Combine(baseDirectory, "Tessdeck");
                CopyLegacySettingsIfNeeded(baseDirectory, directory);
                return directory;
            }
        }

        private static void CopyLegacySettingsIfNeeded(string baseDirectory, string directory)
        {
            string legacyDirectory = Path.Combine(baseDirectory, "EbenTiler");
            if (!System.IO.Directory.Exists(legacyDirectory)) return;

            try
            {
                if (!System.IO.Directory.Exists(directory)) System.IO.Directory.CreateDirectory(directory);

                string[] files = new string[] { "config.ini", "update-state.ini", "update-badge.ini" };
                for (int i = 0; i < files.Length; i++)
                {
                    string source = Path.Combine(legacyDirectory, files[i]);
                    string destination = Path.Combine(directory, files[i]);
                    if (File.Exists(source) && !File.Exists(destination))
                    {
                        File.Copy(source, destination, false);
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public static string FilePath
        {
            get { return Path.Combine(Directory, "config.ini"); }
        }

        public static Config CreateDefault()
        {
            Config config = new Config();
            Dictionary<SnapAction, string> defaults = SnapActions.DefaultHotkeys();
            foreach (KeyValuePair<SnapAction, string> pair in defaults)
            {
                config.Hotkeys[pair.Key] = Hotkey.Parse(pair.Value);
            }
            return config;
        }

        public static Config Load()
        {
            Config config = CreateDefault();
            string path = FilePath;
            if (!File.Exists(path)) return config;

            // 기존 버전에서 이미 사용 중이던 사람에게 업데이트 후 가이드를 갑자기 띄우지 않는다.
            // 새 설정 키가 파일에 명시된 경우에만 아래 파싱에서 값을 덮어쓴다.
            config.ShowWelcomeGuide = false;

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("[")) continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();

                    if (string.Equals(key, "QuickLayoutSave", StringComparison.OrdinalIgnoreCase))
                    {
                        config.QuickLayoutSaveHotkey = Hotkey.Parse(value);
                        continue;
                    }

                    if (string.Equals(key, "QuickLayoutRestore", StringComparison.OrdinalIgnoreCase))
                    {
                        config.QuickLayoutRestoreHotkey = Hotkey.Parse(value);
                        continue;
                    }

                    SnapAction action;
                    if (SnapActions.TryParse(key, out action))
                    {
                        config.Hotkeys[action] = Hotkey.Parse(value);
                        continue;
                    }

                    if (string.Equals(key, "Gap", StringComparison.OrdinalIgnoreCase))
                    {
                        int gap;
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out gap))
                            config.Gap = Math.Max(0, Math.Min(100, gap));
                    }
                    else if (string.Equals(key, "CycleHalves", StringComparison.OrdinalIgnoreCase))
                    {
                        config.CycleHalves = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
                    }
                    else if (string.Equals(key, "CycleRatio1", StringComparison.OrdinalIgnoreCase))
                    {
                        config.CycleRatio1 = ParseCycleRatio(value, config.CycleRatio1);
                    }
                    else if (string.Equals(key, "CycleRatio2", StringComparison.OrdinalIgnoreCase))
                    {
                        config.CycleRatio2 = ParseCycleRatio(value, config.CycleRatio2);
                    }
                    else if (string.Equals(key, "CycleRatio3", StringComparison.OrdinalIgnoreCase))
                    {
                        config.CycleRatio3 = ParseCycleRatio(value, config.CycleRatio3);
                    }
                    else if (string.Equals(key, "ShowWelcomeGuide", StringComparison.OrdinalIgnoreCase))
                    {
                        config.ShowWelcomeGuide = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return config;
        }

        private static int ParseCycleRatio(string value, int fallback)
        {
            int ratio;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ratio))
            {
                return fallback;
            }
            return Math.Max(20, Math.Min(80, ratio));
        }

        public void Save()
        {
            try
            {
                if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; Tessdeck for Windows 설정 파일");
                sb.AppendLine("; 단축키 형식 예시: Ctrl+Alt+Left, Ctrl+Alt+Shift+U, Win+Alt+Enter");
                sb.AppendLine("; 값을 비워 두면 그 기능의 단축키는 등록하지 않는다.");
                sb.AppendLine();
                sb.AppendLine("[Hotkeys]");

                SnapAction[] ordered = SnapActions.Ordered;
                for (int i = 0; i < ordered.Length; i++)
                {
                    SnapAction action = ordered[i];
                    Hotkey hotkey;
                    string value = "";
                    if (Hotkeys.TryGetValue(action, out hotkey) && hotkey != null) value = hotkey.ToString();
                    sb.AppendLine(action.ToString() + "=" + value);
                }

                sb.AppendLine("QuickLayoutSave=" + HotkeyText(QuickLayoutSaveHotkey));
                sb.AppendLine("QuickLayoutRestore=" + HotkeyText(QuickLayoutRestoreHotkey));

                sb.AppendLine();
                sb.AppendLine("[Options]");
                sb.AppendLine("Gap=" + Gap.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CycleHalves=" + (CycleHalves ? "true" : "false"));
                sb.AppendLine("CycleRatio1=" + Math.Max(20, Math.Min(80, CycleRatio1)).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CycleRatio2=" + Math.Max(20, Math.Min(80, CycleRatio2)).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CycleRatio3=" + Math.Max(20, Math.Min(80, CycleRatio3)).ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("ShowWelcomeGuide=" + (ShowWelcomeGuide ? "true" : "false"));

                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        public Hotkey Get(SnapAction action)
        {
            Hotkey hotkey;
            if (Hotkeys.TryGetValue(action, out hotkey) && hotkey != null) return hotkey;
            return new Hotkey();
        }

        private static string HotkeyText(Hotkey hotkey)
        {
            return hotkey != null && !hotkey.IsEmpty ? hotkey.ToString() : "";
        }

        private static Hotkey CloneHotkey(Hotkey source)
        {
            Hotkey target = new Hotkey();
            if (source != null)
            {
                target.Ctrl = source.Ctrl;
                target.Alt = source.Alt;
                target.Shift = source.Shift;
                target.Win = source.Win;
                target.Key = source.Key;
            }
            return target;
        }

        public void CopyFrom(Config other)
        {
            if (other == null) return;
            Gap = other.Gap;
            CycleHalves = other.CycleHalves;
            CycleRatio1 = other.CycleRatio1;
            CycleRatio2 = other.CycleRatio2;
            CycleRatio3 = other.CycleRatio3;
            ShowWelcomeGuide = other.ShowWelcomeGuide;
            Hotkeys = other.Hotkeys;
            QuickLayoutSaveHotkey = CloneHotkey(other.QuickLayoutSaveHotkey);
            QuickLayoutRestoreHotkey = CloneHotkey(other.QuickLayoutRestoreHotkey);
        }

        public Config Clone()
        {
            Config copy = new Config();
            copy.Gap = Gap;
            copy.CycleHalves = CycleHalves;
            copy.CycleRatio1 = CycleRatio1;
            copy.CycleRatio2 = CycleRatio2;
            copy.CycleRatio3 = CycleRatio3;
            copy.ShowWelcomeGuide = ShowWelcomeGuide;
            copy.QuickLayoutSaveHotkey = CloneHotkey(QuickLayoutSaveHotkey);
            copy.QuickLayoutRestoreHotkey = CloneHotkey(QuickLayoutRestoreHotkey);
            foreach (KeyValuePair<SnapAction, Hotkey> pair in Hotkeys)
            {
                Hotkey source = pair.Value;
                Hotkey target = new Hotkey();
                if (source != null)
                {
                    target.Ctrl = source.Ctrl;
                    target.Alt = source.Alt;
                    target.Shift = source.Shift;
                    target.Win = source.Win;
                    target.Key = source.Key;
                }
                copy.Hotkeys[pair.Key] = target;
            }
            return copy;
        }
    }
}
