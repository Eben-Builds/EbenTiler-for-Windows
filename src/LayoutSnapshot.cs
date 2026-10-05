using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    internal sealed class LayoutSnapshotData
    {
        public DateTime CapturedAtUtc { get; private set; }
        public List<WindowSnapshotEntry> Windows { get; private set; }

        public LayoutSnapshotData(DateTime capturedAtUtc, List<WindowSnapshotEntry> windows)
        {
            CapturedAtUtc = capturedAtUtc;
            Windows = windows ?? new List<WindowSnapshotEntry>();
        }
    }

    internal sealed class LayoutSnapshotSaveResult
    {
        public bool Saved { get; private set; }
        public bool SkippedEmpty { get; private set; }
        public int WindowCount { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }

        public LayoutSnapshotSaveResult(
            bool saved,
            bool skippedEmpty,
            int windowCount,
            string filePath,
            string error)
        {
            Saved = saved;
            SkippedEmpty = skippedEmpty;
            WindowCount = windowCount;
            FilePath = filePath;
            Error = error;
        }
    }

    internal sealed class LayoutSnapshotReadResult
    {
        public bool Loaded { get; private set; }
        public bool FileExists { get; private set; }
        public int DeclaredWindowCount { get; private set; }
        public int SkippedWindowCount { get; private set; }
        public LayoutSnapshotData Snapshot { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }

        public LayoutSnapshotReadResult(
            bool loaded,
            bool fileExists,
            int declaredWindowCount,
            int skippedWindowCount,
            LayoutSnapshotData snapshot,
            string filePath,
            string error)
        {
            Loaded = loaded;
            FileExists = fileExists;
            DeclaredWindowCount = declaredWindowCount;
            SkippedWindowCount = skippedWindowCount;
            Snapshot = snapshot;
            FilePath = filePath;
            Error = error;
        }
    }

    internal sealed class WindowSnapshotEntry
    {
        public string ProcessName { get; private set; }
        public string WindowClass { get; private set; }
        public int InstanceIndex { get; private set; }
        public string MonitorDeviceName { get; private set; }
        public Rectangle VisualRect { get; private set; }
        public int NormalizedX { get; private set; }
        public int NormalizedY { get; private set; }
        public int NormalizedWidth { get; private set; }
        public int NormalizedHeight { get; private set; }
        public bool Maximized { get; private set; }

        public WindowSnapshotEntry(
            string processName,
            string windowClass,
            int instanceIndex,
            string monitorDeviceName,
            Rectangle visualRect,
            int normalizedX,
            int normalizedY,
            int normalizedWidth,
            int normalizedHeight,
            bool maximized)
        {
            ProcessName = processName;
            WindowClass = windowClass;
            InstanceIndex = instanceIndex;
            MonitorDeviceName = monitorDeviceName;
            VisualRect = visualRect;
            NormalizedX = normalizedX;
            NormalizedY = normalizedY;
            NormalizedWidth = normalizedWidth;
            NormalizedHeight = normalizedHeight;
            Maximized = maximized;
        }
    }

    internal static class LayoutSnapshot
    {
        private const int CoordinateScale = 10000;
        private const int SnapshotFormatVersion = 1;

        public static string FilePath
        {
            get { return Path.Combine(Config.Directory, "quick-layout.ini"); }
        }

        /// <summary>
        /// 현재 관리 가능한 일반 앱 창을 메모리상의 snapshot 데이터로 변환한다.
        /// 파일 저장/복원은 하지 않는다.
        /// </summary>
        public static LayoutSnapshotData CaptureCurrent()
        {
            List<IntPtr> handles = WindowManager.EnumerateManageableWindows();
            List<WindowSnapshotEntry> entries = new List<WindowSnapshotEntry>();
            Dictionary<string, int> instanceCounts =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < handles.Count; i++)
            {
                IntPtr hwnd = handles[i];

                string processName;
                if (!TryGetProcessName(hwnd, out processName))
                {
                    continue;
                }

                string windowClass = GetWindowClass(hwnd);
                if (string.IsNullOrEmpty(windowClass))
                {
                    continue;
                }

                Rectangle visualRect = WindowManager.GetVisualRect(hwnd);
                if (visualRect.IsEmpty)
                {
                    continue;
                }

                Screen screen = Screen.FromHandle(hwnd);
                Rectangle workArea = screen.WorkingArea;
                if (workArea.Width <= 0 || workArea.Height <= 0)
                {
                    continue;
                }

                string instanceKey = processName + "\0" + windowClass;
                int instanceIndex;
                if (!instanceCounts.TryGetValue(instanceKey, out instanceIndex))
                {
                    instanceIndex = 0;
                }
                instanceCounts[instanceKey] = instanceIndex + 1;

                entries.Add(new WindowSnapshotEntry(
                    processName,
                    windowClass,
                    instanceIndex,
                    screen.DeviceName,
                    visualRect,
                    Normalize(visualRect.Left - workArea.Left, workArea.Width),
                    Normalize(visualRect.Top - workArea.Top, workArea.Height),
                    Normalize(visualRect.Width, workArea.Width),
                    Normalize(visualRect.Height, workArea.Height),
                    Native.IsZoomed(hwnd)));
            }

            return new LayoutSnapshotData(DateTime.UtcNow, entries);
        }

        /// <summary>
        /// 현재 snapshot을 %APPDATA%\Tessdeck\quick-layout.ini 에 안전하게 저장한다.
        /// 0개 창이면 기존 snapshot을 절대 덮어쓰지 않는다.
        /// </summary>
        public static LayoutSnapshotSaveResult SaveCurrent()
        {
            LayoutSnapshotData snapshot = CaptureCurrent();
            string path = FilePath;

            if (snapshot.Windows.Count == 0)
            {
                return new LayoutSnapshotSaveResult(false, true, 0, path, null);
            }

            string directory = Path.GetDirectoryName(path);
            string tempPath = null;

            try
            {
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                tempPath = Path.Combine(
                    directory,
                    "quick-layout." + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)
                        + "." + Guid.NewGuid().ToString("N") + ".tmp");

                string serialized = Serialize(snapshot);
                File.WriteAllText(tempPath, serialized, new UTF8Encoding(false));

                if (File.Exists(path))
                {
                    File.Replace(tempPath, path, null, true);
                }
                else
                {
                    File.Move(tempPath, path);
                }

                tempPath = null;
                return new LayoutSnapshotSaveResult(
                    true, false, snapshot.Windows.Count, path, null);
            }
            catch (IOException ex)
            {
                return new LayoutSnapshotSaveResult(
                    false, false, snapshot.Windows.Count, path, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return new LayoutSnapshotSaveResult(
                    false, false, snapshot.Windows.Count, path, ex.Message);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempPath))
                {
                    try
                    {
                        if (File.Exists(tempPath)) File.Delete(tempPath);
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        /// <summary>
        /// 저장된 quick-layout.ini 를 읽어 검증된 메모리 snapshot으로 변환한다.
        /// 이 메서드는 창을 이동하거나 복원하지 않는다.
        /// </summary>
        public static LayoutSnapshotReadResult ReadSaved()
        {
            return ReadFromFile(FilePath);
        }

        internal static LayoutSnapshotReadResult ReadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return new LayoutSnapshotReadResult(
                    false, false, 0, 0, null, path, "Snapshot path is empty.");
            }

            if (!File.Exists(path))
            {
                return new LayoutSnapshotReadResult(
                    false, false, 0, 0, null, path, null);
            }

            try
            {
                string[] lines = File.ReadAllLines(path, Encoding.UTF8);
                Dictionary<string, Dictionary<string, string>> sections = ParseIniSections(lines);

                Dictionary<string, string> snapshotSection;
                if (!sections.TryGetValue("Snapshot", out snapshotSection))
                {
                    return ReadFailure(path, "Snapshot section is missing.");
                }

                int version;
                if (!TryGetInt(snapshotSection, "Version", out version)
                    || version != SnapshotFormatVersion)
                {
                    return ReadFailure(path, "Unsupported or invalid snapshot version.");
                }

                DateTime capturedAtUtc;
                string capturedText;
                if (!snapshotSection.TryGetValue("CapturedUtc", out capturedText)
                    || !DateTime.TryParse(
                        capturedText,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out capturedAtUtc))
                {
                    return ReadFailure(path, "Snapshot capture time is invalid.");
                }

                int declaredCount;
                if (!TryGetInt(snapshotSection, "WindowCount", out declaredCount)
                    || declaredCount < 0
                    || declaredCount > 1000)
                {
                    return ReadFailure(path, "Snapshot window count is invalid.");
                }

                List<WindowSnapshotEntry> entries = new List<WindowSnapshotEntry>();
                int skipped = 0;

                for (int i = 0; i < declaredCount; i++)
                {
                    Dictionary<string, string> windowSection;
                    if (!sections.TryGetValue(
                        "Window" + i.ToString(CultureInfo.InvariantCulture),
                        out windowSection))
                    {
                        skipped++;
                        continue;
                    }

                    WindowSnapshotEntry entry;
                    if (!TryParseWindowEntry(windowSection, out entry))
                    {
                        skipped++;
                        continue;
                    }

                    entries.Add(entry);
                }

                LayoutSnapshotData snapshot = new LayoutSnapshotData(capturedAtUtc, entries);
                return new LayoutSnapshotReadResult(
                    true, true, declaredCount, skipped, snapshot, path, null);
            }
            catch (IOException ex)
            {
                return ReadFailure(path, ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return ReadFailure(path, ex.Message);
            }
        }

        private static LayoutSnapshotReadResult ReadFailure(string path, string error)
        {
            return new LayoutSnapshotReadResult(
                false, true, 0, 0, null, path, error);
        }

        private static Dictionary<string, Dictionary<string, string>> ParseIniSections(string[] lines)
        {
            Dictionary<string, Dictionary<string, string>> sections =
                new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            Dictionary<string, string> current = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]") && line.Length > 2)
                {
                    string sectionName = line.Substring(1, line.Length - 2).Trim();
                    if (sectionName.Length == 0)
                    {
                        current = null;
                        continue;
                    }

                    if (!sections.TryGetValue(sectionName, out current))
                    {
                        current = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        sections[sectionName] = current;
                    }
                    continue;
                }

                if (current == null)
                {
                    continue;
                }

                int equals = line.IndexOf('=');
                if (equals <= 0)
                {
                    continue;
                }

                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                if (key.Length > 0)
                {
                    current[key] = value;
                }
            }

            return sections;
        }

        private static bool TryParseWindowEntry(
            Dictionary<string, string> section,
            out WindowSnapshotEntry entry)
        {
            entry = null;

            string processName;
            string windowClass;
            string monitor;
            if (!TryGetNonEmpty(section, "Process", out processName)
                || !TryGetNonEmpty(section, "Class", out windowClass)
                || !TryGetNonEmpty(section, "Monitor", out monitor))
            {
                return false;
            }

            int instance;
            int x;
            int y;
            int width;
            int height;
            if (!TryGetInt(section, "Instance", out instance)
                || instance < 0
                || instance > 10000
                || !TryGetCoordinate(section, "X", out x, true)
                || !TryGetCoordinate(section, "Y", out y, true)
                || !TryGetCoordinate(section, "Width", out width, false)
                || !TryGetCoordinate(section, "Height", out height, false))
            {
                return false;
            }

            bool maximized;
            string maximizedText;
            if (!section.TryGetValue("Maximized", out maximizedText)
                || !bool.TryParse(maximizedText, out maximized))
            {
                return false;
            }

            entry = new WindowSnapshotEntry(
                processName,
                NormalizeWindowClassKey(windowClass),
                instance,
                monitor,
                Rectangle.Empty,
                x,
                y,
                width,
                height,
                maximized);
            return true;
        }

        private static bool TryGetCoordinate(
            Dictionary<string, string> section,
            string key,
            out int value,
            bool allowZero)
        {
            if (!TryGetInt(section, key, out value))
            {
                return false;
            }

            if (value < 0 || value > CoordinateScale)
            {
                return false;
            }

            if (!allowZero && value == 0)
            {
                return false;
            }

            return true;
        }

        private static bool TryGetInt(
            Dictionary<string, string> section,
            string key,
            out int value)
        {
            value = 0;
            string text;
            return section.TryGetValue(key, out text)
                && int.TryParse(
                    text,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out value);
        }

        private static bool TryGetNonEmpty(
            Dictionary<string, string> section,
            string key,
            out string value)
        {
            value = null;
            string text;
            if (!section.TryGetValue(key, out text))
            {
                return false;
            }

            text = text.Trim();
            if (text.Length == 0 || text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
            {
                return false;
            }

            value = text;
            return true;
        }

        private static string Serialize(LayoutSnapshotData snapshot)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("; Tessdeck Quick Layout Snapshot");
            sb.AppendLine("; Local-only window geometry. No window titles, URLs, file paths, or command lines.");
            sb.AppendLine();
            sb.AppendLine("[Snapshot]");
            sb.AppendLine("Version=" + SnapshotFormatVersion.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("CapturedUtc=" + snapshot.CapturedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            sb.AppendLine("WindowCount=" + snapshot.Windows.Count.ToString(CultureInfo.InvariantCulture));

            for (int i = 0; i < snapshot.Windows.Count; i++)
            {
                WindowSnapshotEntry entry = snapshot.Windows[i];
                sb.AppendLine();
                sb.AppendLine("[Window" + i.ToString(CultureInfo.InvariantCulture) + "]");
                sb.AppendLine("Process=" + SafeIniValue(entry.ProcessName));
                sb.AppendLine("Class=" + SafeIniValue(entry.WindowClass));
                sb.AppendLine("Instance=" + entry.InstanceIndex.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Monitor=" + SafeIniValue(entry.MonitorDeviceName));
                sb.AppendLine("X=" + entry.NormalizedX.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Y=" + entry.NormalizedY.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Width=" + entry.NormalizedWidth.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Height=" + entry.NormalizedHeight.ToString(CultureInfo.InvariantCulture));
                sb.AppendLine("Maximized=" + (entry.Maximized ? "true" : "false"));
            }

            return sb.ToString();
        }

        private static string SafeIniValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOf('\r') >= 0 || value.IndexOf('\n') >= 0)
            {
                throw new InvalidDataException("Snapshot text value contains an invalid line break.");
            }

            return value;
        }

        private static bool TryGetProcessName(IntPtr hwnd, out string processName)
        {
            processName = null;

            uint processId;
            if (Native.GetWindowThreadProcessId(hwnd, out processId) == 0 || processId == 0)
            {
                return false;
            }

            try
            {
                using (Process process = Process.GetProcessById((int)processId))
                {
                    processName = process.ProcessName;
                    return !string.IsNullOrEmpty(processName);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private static string GetWindowClass(IntPtr hwnd)
        {
            StringBuilder className = new StringBuilder(256);
            int length = Native.GetClassName(hwnd, className, className.Capacity);
            if (length <= 0)
            {
                return string.Empty;
            }

            return NormalizeWindowClassKey(className.ToString());
        }

        /// <summary>
        /// WPF HwndWrapper 클래스의 실행마다 달라지는 런타임 식별자를 제거한다.
        /// 다른 일반 Win32/Chromium/Terminal 클래스명은 그대로 유지한다.
        /// </summary>
        internal static string NormalizeWindowClassKey(string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return string.Empty;
            }

            const string prefix = "HwndWrapper[";
            if (!className.StartsWith(prefix, StringComparison.Ordinal)
                || !className.EndsWith("]", StringComparison.Ordinal))
            {
                return className;
            }

            int separator = className.IndexOf(";;", prefix.Length, StringComparison.Ordinal);
            if (separator < 0)
            {
                return className;
            }

            string stableName = className.Substring(prefix.Length, separator - prefix.Length);
            if (string.IsNullOrEmpty(stableName))
            {
                return className;
            }

            return prefix + stableName + "]";
        }

        private static int Normalize(int value, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            long scaled = (long)Math.Round((double)value * CoordinateScale / total);
            if (scaled < 0) return 0;
            if (scaled > CoordinateScale) return CoordinateScale;
            return (int)scaled;
        }
    }
}
