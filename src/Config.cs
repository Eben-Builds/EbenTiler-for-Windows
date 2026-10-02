using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace EbenTilerWindows
{
    /// <summary>단축키와 옵션을 %APPDATA%\EbenTiler\config.ini 에 읽고 쓴다.</summary>
    public sealed class Config
    {
        public Dictionary<SnapAction, Hotkey> Hotkeys;
        public int Gap;
        public bool CycleHalves;

        public Config()
        {
            Hotkeys = new Dictionary<SnapAction, Hotkey>();
            Gap = 0;
            CycleHalves = true;
        }

        public static string Directory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EbenTiler");
            }
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
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return config;
        }

        public void Save()
        {
            try
            {
                if (!System.IO.Directory.Exists(Directory)) System.IO.Directory.CreateDirectory(Directory);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("; EbenTiler for Windows 설정 파일");
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

                sb.AppendLine();
                sb.AppendLine("[Options]");
                sb.AppendLine("Gap=" + Gap.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("CycleHalves=" + (CycleHalves ? "true" : "false"));

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

        public void CopyFrom(Config other)
        {
            if (other == null) return;
            Gap = other.Gap;
            CycleHalves = other.CycleHalves;
            Hotkeys = other.Hotkeys;
        }

        public Config Clone()
        {
            Config copy = new Config();
            copy.Gap = Gap;
            copy.CycleHalves = CycleHalves;
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
