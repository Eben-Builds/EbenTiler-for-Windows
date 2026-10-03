using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace EbenTilerWindows
{
    /// <summary>
    /// 새 릴리스가 발견됐다는 상태를 로컬에 보관한다.
    /// 앱 버전이 해당 릴리스 이상이 되면 자동으로 지운다.
    /// </summary>
    internal static class UpdateBadgeState
    {
        public static event Action<string> Changed;

        private static string FilePath
        {
            get { return Path.Combine(Config.Directory, "update-badge.ini"); }
        }

        public static string GetPendingTag()
        {
            string tagName = ReadTag();
            Version available;
            if (!TryParseVersion(tagName, out available))
            {
                if (!string.IsNullOrWhiteSpace(tagName)) Clear();
                return null;
            }

            Version current = Assembly.GetExecutingAssembly().GetName().Version;
            if (current != null && available.CompareTo(current) <= 0)
            {
                Clear();
                return null;
            }

            return tagName;
        }

        public static void SetPending(string tagName)
        {
            Version available;
            if (!TryParseVersion(tagName, out available)) return;

            Version current = Assembly.GetExecutingAssembly().GetName().Version;
            if (current != null && available.CompareTo(current) <= 0)
            {
                Clear();
                return;
            }

            try
            {
                if (!Directory.Exists(Config.Directory)) Directory.CreateDirectory(Config.Directory);
                File.WriteAllText(
                    FilePath,
                    "; EbenTiler pending update badge\r\nAvailableTag=" + tagName + "\r\n",
                    new UTF8Encoding(false));
            }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }

            RaiseChanged(tagName);
        }

        public static void Clear()
        {
            bool existed = false;
            try
            {
                existed = File.Exists(FilePath);
                if (existed) File.Delete(FilePath);
            }
            catch (IOException) { return; }
            catch (UnauthorizedAccessException) { return; }

            if (existed) RaiseChanged(null);
        }

        private static string ReadTag()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                string[] lines = File.ReadAllLines(FilePath, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    if (!string.Equals(line.Substring(0, eq).Trim(), "AvailableTag", StringComparison.OrdinalIgnoreCase)) continue;
                    return line.Substring(eq + 1).Trim();
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
            return null;
        }

        private static bool TryParseVersion(string tagName, out Version version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(tagName)) return false;
            string value = tagName.Trim();
            if (value.StartsWith("v", StringComparison.OrdinalIgnoreCase)) value = value.Substring(1);
            int dash = value.IndexOf('-');
            if (dash >= 0) value = value.Substring(0, dash);
            return Version.TryParse(value, out version);
        }

        private static void RaiseChanged(string tagName)
        {
            Action<string> handler = Changed;
            if (handler != null) handler(tagName);
        }
    }
}
