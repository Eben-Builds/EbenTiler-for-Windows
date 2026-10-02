using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>단축키 하나(보조키 조합 + 주 키)를 나타낸다.</summary>
    public sealed class Hotkey
    {
        public bool Ctrl;
        public bool Alt;
        public bool Shift;
        public bool Win;
        public Keys Key;

        public Hotkey()
        {
            Key = Keys.None;
        }

        public bool IsEmpty
        {
            get { return Key == Keys.None; }
        }

        /// <summary>보조키를 하나도 안 쓰면 다른 프로그램 입력을 통째로 가로채므로 막는다.</summary>
        public bool HasModifier
        {
            get { return Ctrl || Alt || Shift || Win; }
        }

        public uint Modifiers
        {
            get
            {
                uint value = 0;
                if (Ctrl) { value |= Native.MOD_CONTROL; }
                if (Alt) { value |= Native.MOD_ALT; }
                if (Shift) { value |= Native.MOD_SHIFT; }
                if (Win) { value |= Native.MOD_WIN; }
                return value;
            }
        }

        public override string ToString()
        {
            if (IsEmpty)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            if (Ctrl) { sb.Append("Ctrl+"); }
            if (Alt) { sb.Append("Alt+"); }
            if (Shift) { sb.Append("Shift+"); }
            if (Win) { sb.Append("Win+"); }
            sb.Append(Key.ToString());
            return sb.ToString();
        }

        /// <summary>설정 창에 보여줄 사람이 읽기 쉬운 형태.</summary>
        public string ToDisplayString()
        {
            if (IsEmpty)
            {
                return "(없음)";
            }

            StringBuilder sb = new StringBuilder();
            if (Ctrl) { sb.Append("Ctrl + "); }
            if (Alt) { sb.Append("Alt + "); }
            if (Shift) { sb.Append("Shift + "); }
            if (Win) { sb.Append("Win + "); }
            sb.Append(KeyDisplayName(Key));
            return sb.ToString();
        }

        private static readonly Dictionary<Keys, string> _keyNames = BuildKeyNames();

        private static Dictionary<Keys, string> BuildKeyNames()
        {
            Dictionary<Keys, string> map = new Dictionary<Keys, string>();
            map[Keys.Left] = "←";
            map[Keys.Right] = "→";
            map[Keys.Up] = "↑";
            map[Keys.Down] = "↓";
            map[Keys.Enter] = "Enter";
            map[Keys.Back] = "Backspace";
            map[Keys.Oemplus] = "=";
            map[Keys.OemMinus] = "-";
            map[Keys.Oemcomma] = ",";
            map[Keys.OemPeriod] = ".";
            map[Keys.Space] = "Space";
            map[Keys.OemQuestion] = "/";
            map[Keys.Oem1] = ";";
            map[Keys.Oem7] = "'";
            map[Keys.OemOpenBrackets] = "[";
            map[Keys.Oem6] = "]";
            map[Keys.Oem5] = "\\";
            map[Keys.Oemtilde] = "`";
            return map;
        }

        public static string KeyDisplayName(Keys key)
        {
            string value;
            if (_keyNames.TryGetValue(key, out value))
            {
                return value;
            }

            if (key >= Keys.D0 && key <= Keys.D9)
            {
                return ((char)('0' + (key - Keys.D0))).ToString();
            }
            if (key >= Keys.NumPad0 && key <= Keys.NumPad9)
            {
                return "숫자패드 " + ((char)('0' + (key - Keys.NumPad0)));
            }
            return key.ToString();
        }

        /// <summary>"Ctrl+Alt+Left" 형태의 문자열을 단축키로 되돌린다.</summary>
        public static Hotkey Parse(string text)
        {
            Hotkey hotkey = new Hotkey();
            if (string.IsNullOrEmpty(text))
            {
                return hotkey;
            }

            string[] parts = text.Split('+');
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i].Trim();
                if (part.Length == 0)
                {
                    continue;
                }

                if (string.Equals(part, "Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(part, "Control", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Ctrl = true;
                }
                else if (string.Equals(part, "Alt", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Alt = true;
                }
                else if (string.Equals(part, "Shift", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Shift = true;
                }
                else if (string.Equals(part, "Win", StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(part, "Windows", StringComparison.OrdinalIgnoreCase))
                {
                    hotkey.Win = true;
                }
                else
                {
                    try
                    {
                        hotkey.Key = (Keys)Enum.Parse(typeof(Keys), part, true);
                    }
                    catch (ArgumentException)
                    {
                        hotkey.Key = Keys.None;
                    }
                }
            }
            return hotkey;
        }

        /// <summary>설정 창에서 사용자가 실제로 누른 키 조합을 받아 단축키로 만든다.</summary>
        public static Hotkey FromKeyData(Keys keyData)
        {
            Hotkey hotkey = new Hotkey();
            hotkey.Ctrl = (keyData & Keys.Control) == Keys.Control;
            hotkey.Alt = (keyData & Keys.Alt) == Keys.Alt;
            hotkey.Shift = (keyData & Keys.Shift) == Keys.Shift;

            Keys code = keyData & Keys.KeyCode;
            if (code == Keys.ControlKey || code == Keys.Menu || code == Keys.ShiftKey ||
                code == Keys.LWin || code == Keys.RWin || code == Keys.None)
            {
                hotkey.Key = Keys.None;
            }
            else
            {
                hotkey.Key = code;
            }
            return hotkey;
        }

        public bool SameAs(Hotkey other)
        {
            if (other == null)
            {
                return false;
            }
            return Ctrl == other.Ctrl && Alt == other.Alt && Shift == other.Shift
                && Win == other.Win && Key == other.Key;
        }
    }
}
