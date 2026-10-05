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

    internal sealed class CurrentWindowSnapshot
    {
        public IntPtr Handle { get; private set; }
        public WindowSnapshotEntry Entry { get; private set; }

        public CurrentWindowSnapshot(IntPtr handle, WindowSnapshotEntry entry)
        {
            Handle = handle;
            Entry = entry;
        }
    }

    internal sealed class LayoutSnapshotMatchedWindow
    {
        public WindowSnapshotEntry Saved { get; private set; }
        public CurrentWindowSnapshot Current { get; private set; }

        public LayoutSnapshotMatchedWindow(
            WindowSnapshotEntry saved,
            CurrentWindowSnapshot current)
        {
            Saved = saved;
            Current = current;
        }
    }

    internal sealed class LayoutSnapshotMatchResult
    {
        public bool Ready { get; private set; }
        public bool FileExists { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }
        public int SkippedSavedWindowCount { get; private set; }
        public int SavedWindowCount { get; private set; }
        public int CurrentWindowCount { get; private set; }
        public List<LayoutSnapshotMatchedWindow> Matched { get; private set; }
        public List<WindowSnapshotEntry> Missing { get; private set; }
        public List<CurrentWindowSnapshot> CurrentOnly { get; private set; }

        public LayoutSnapshotMatchResult(
            bool ready,
            bool fileExists,
            string filePath,
            string error,
            int skippedSavedWindowCount,
            int savedWindowCount,
            int currentWindowCount,
            List<LayoutSnapshotMatchedWindow> matched,
            List<WindowSnapshotEntry> missing,
            List<CurrentWindowSnapshot> currentOnly)
        {
            Ready = ready;
            FileExists = fileExists;
            FilePath = filePath;
            Error = error;
            SkippedSavedWindowCount = skippedSavedWindowCount;
            SavedWindowCount = savedWindowCount;
            CurrentWindowCount = currentWindowCount;
            Matched = matched ?? new List<LayoutSnapshotMatchedWindow>();
            Missing = missing ?? new List<WindowSnapshotEntry>();
            CurrentOnly = currentOnly ?? new List<CurrentWindowSnapshot>();
        }
    }

    internal sealed class LayoutRestorePlanItem
    {
        public LayoutSnapshotMatchedWindow Match { get; private set; }
        public string TargetMonitorDeviceName { get; private set; }
        public Rectangle TargetWorkArea { get; private set; }
        public Rectangle TargetRect { get; private set; }

        public LayoutRestorePlanItem(
            LayoutSnapshotMatchedWindow match,
            string targetMonitorDeviceName,
            Rectangle targetWorkArea,
            Rectangle targetRect)
        {
            Match = match;
            TargetMonitorDeviceName = targetMonitorDeviceName;
            TargetWorkArea = targetWorkArea;
            TargetRect = targetRect;
        }
    }

    internal sealed class LayoutRestorePlanSkippedItem
    {
        public LayoutSnapshotMatchedWindow Match { get; private set; }
        public string Reason { get; private set; }

        public LayoutRestorePlanSkippedItem(
            LayoutSnapshotMatchedWindow match,
            string reason)
        {
            Match = match;
            Reason = reason;
        }
    }

    internal sealed class LayoutRestorePlanResult
    {
        public bool Ready { get; private set; }
        public bool FileExists { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }
        public int SavedWindowCount { get; private set; }
        public int CurrentWindowCount { get; private set; }
        public List<LayoutRestorePlanItem> Planned { get; private set; }
        public List<WindowSnapshotEntry> MissingWindows { get; private set; }
        public List<LayoutRestorePlanSkippedItem> Skipped { get; private set; }
        public List<CurrentWindowSnapshot> CurrentOnly { get; private set; }

        public LayoutRestorePlanResult(
            bool ready,
            bool fileExists,
            string filePath,
            string error,
            int savedWindowCount,
            int currentWindowCount,
            List<LayoutRestorePlanItem> planned,
            List<WindowSnapshotEntry> missingWindows,
            List<LayoutRestorePlanSkippedItem> skipped,
            List<CurrentWindowSnapshot> currentOnly)
        {
            Ready = ready;
            FileExists = fileExists;
            FilePath = filePath;
            Error = error;
            SavedWindowCount = savedWindowCount;
            CurrentWindowCount = currentWindowCount;
            Planned = planned ?? new List<LayoutRestorePlanItem>();
            MissingWindows = missingWindows ?? new List<WindowSnapshotEntry>();
            Skipped = skipped ?? new List<LayoutRestorePlanSkippedItem>();
            CurrentOnly = currentOnly ?? new List<CurrentWindowSnapshot>();
        }
    }

    internal sealed class LayoutRestoreAppliedItem
    {
        public LayoutRestorePlanItem Plan { get; private set; }
        public Rectangle ActualRect { get; private set; }
        public bool Maximized { get; private set; }

        public LayoutRestoreAppliedItem(
            LayoutRestorePlanItem plan,
            Rectangle actualRect,
            bool maximized)
        {
            Plan = plan;
            ActualRect = actualRect;
            Maximized = maximized;
        }
    }

    internal sealed class LayoutRestoreFailedItem
    {
        public LayoutRestorePlanItem Plan { get; private set; }
        public string Reason { get; private set; }

        public LayoutRestoreFailedItem(
            LayoutRestorePlanItem plan,
            string reason)
        {
            Plan = plan;
            Reason = reason;
        }
    }

    internal sealed class LayoutRestoreResult
    {
        public bool Ready { get; private set; }
        public bool FileExists { get; private set; }
        public string FilePath { get; private set; }
        public string Error { get; private set; }
        public int SavedWindowCount { get; private set; }
        public int CurrentWindowCount { get; private set; }
        public List<LayoutRestoreAppliedItem> Applied { get; private set; }
        public List<LayoutRestoreFailedItem> Failed { get; private set; }
        public List<WindowSnapshotEntry> MissingWindows { get; private set; }
        public List<LayoutRestorePlanSkippedItem> Skipped { get; private set; }
        public List<CurrentWindowSnapshot> CurrentOnly { get; private set; }

        public LayoutRestoreResult(
            bool ready,
            bool fileExists,
            string filePath,
            string error,
            int savedWindowCount,
            int currentWindowCount,
            List<LayoutRestoreAppliedItem> applied,
            List<LayoutRestoreFailedItem> failed,
            List<WindowSnapshotEntry> missingWindows,
            List<LayoutRestorePlanSkippedItem> skipped,
            List<CurrentWindowSnapshot> currentOnly)
        {
            Ready = ready;
            FileExists = fileExists;
            FilePath = filePath;
            Error = error;
            SavedWindowCount = savedWindowCount;
            CurrentWindowCount = currentWindowCount;
            Applied = applied ?? new List<LayoutRestoreAppliedItem>();
            Failed = failed ?? new List<LayoutRestoreFailedItem>();
            MissingWindows = missingWindows ?? new List<WindowSnapshotEntry>();
            Skipped = skipped ?? new List<LayoutRestorePlanSkippedItem>();
            CurrentOnly = currentOnly ?? new List<CurrentWindowSnapshot>();
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
            List<CurrentWindowSnapshot> current = CaptureCurrentWindows();
            List<WindowSnapshotEntry> entries = new List<WindowSnapshotEntry>();

            for (int i = 0; i < current.Count; i++)
            {
                entries.Add(current[i].Entry);
            }

            return new LayoutSnapshotData(DateTime.UtcNow, entries);
        }

        private static List<CurrentWindowSnapshot> CaptureCurrentWindows()
        {
            List<IntPtr> handles = WindowManager.EnumerateManageableWindows();
            List<CurrentWindowSnapshot> current = new List<CurrentWindowSnapshot>();
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

                WindowSnapshotEntry entry = new WindowSnapshotEntry(
                    processName,
                    windowClass,
                    instanceIndex,
                    screen.DeviceName,
                    visualRect,
                    Normalize(visualRect.Left - workArea.Left, workArea.Width),
                    Normalize(visualRect.Top - workArea.Top, workArea.Height),
                    Normalize(visualRect.Width, workArea.Width),
                    Normalize(visualRect.Height, workArea.Height),
                    Native.IsZoomed(hwnd));

                current.Add(new CurrentWindowSnapshot(hwnd, entry));
            }

            return current;
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

        /// <summary>
        /// 저장된 snapshot 식별자와 현재 열린 일반 앱 창을 비교한다.
        /// 매칭만 수행하며 창 이동/복원은 하지 않는다.
        /// </summary>
        public static LayoutSnapshotMatchResult MatchSavedToCurrent()
        {
            LayoutSnapshotReadResult read = ReadSaved();
            if (!read.FileExists)
            {
                return MatchFailure(false, read.FilePath, "Snapshot file is missing.");
            }

            if (!read.Loaded || read.Snapshot == null)
            {
                return MatchFailure(
                    true,
                    read.FilePath,
                    string.IsNullOrEmpty(read.Error) ? "Snapshot could not be loaded." : read.Error);
            }

            List<CurrentWindowSnapshot> current = CaptureCurrentWindows();
            Dictionary<string, CurrentWindowSnapshot> available =
                new Dictionary<string, CurrentWindowSnapshot>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < current.Count; i++)
            {
                string key = BuildIdentityKey(current[i].Entry);
                if (!available.ContainsKey(key))
                {
                    available[key] = current[i];
                }
            }

            List<LayoutSnapshotMatchedWindow> matched =
                new List<LayoutSnapshotMatchedWindow>();
            List<WindowSnapshotEntry> missing = new List<WindowSnapshotEntry>();
            HashSet<IntPtr> usedHandles = new HashSet<IntPtr>();

            for (int i = 0; i < read.Snapshot.Windows.Count; i++)
            {
                WindowSnapshotEntry saved = read.Snapshot.Windows[i];
                string key = BuildIdentityKey(saved);

                CurrentWindowSnapshot found;
                if (available.TryGetValue(key, out found))
                {
                    matched.Add(new LayoutSnapshotMatchedWindow(saved, found));
                    usedHandles.Add(found.Handle);
                    available.Remove(key);
                }
                else
                {
                    missing.Add(saved);
                }
            }

            List<CurrentWindowSnapshot> currentOnly = new List<CurrentWindowSnapshot>();
            for (int i = 0; i < current.Count; i++)
            {
                if (!usedHandles.Contains(current[i].Handle))
                {
                    currentOnly.Add(current[i]);
                }
            }

            return new LayoutSnapshotMatchResult(
                true,
                true,
                read.FilePath,
                null,
                read.SkippedWindowCount,
                read.Snapshot.Windows.Count,
                current.Count,
                matched,
                missing,
                currentOnly);
        }

        /// <summary>
        /// 매칭된 창의 복원 대상 모니터/픽셀 좌표를 계산한다.
        /// 실제 창 이동은 수행하지 않는다.
        /// </summary>
        public static LayoutRestorePlanResult BuildRestorePlan()
        {
            LayoutSnapshotMatchResult match = MatchSavedToCurrent();
            if (!match.Ready)
            {
                return new LayoutRestorePlanResult(
                    false,
                    match.FileExists,
                    match.FilePath,
                    match.Error,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null);
            }

            Dictionary<string, Screen> screens =
                new Dictionary<string, Screen>(StringComparer.OrdinalIgnoreCase);
            Screen[] allScreens = Screen.AllScreens;
            for (int i = 0; i < allScreens.Length; i++)
            {
                Screen screen = allScreens[i];
                if (!screens.ContainsKey(screen.DeviceName))
                {
                    screens[screen.DeviceName] = screen;
                }
            }

            List<LayoutRestorePlanItem> planned = new List<LayoutRestorePlanItem>();
            List<LayoutRestorePlanSkippedItem> skipped =
                new List<LayoutRestorePlanSkippedItem>();

            for (int i = 0; i < match.Matched.Count; i++)
            {
                LayoutSnapshotMatchedWindow item = match.Matched[i];
                WindowSnapshotEntry saved = item.Saved;

                Screen targetScreen;
                if (!screens.TryGetValue(saved.MonitorDeviceName, out targetScreen))
                {
                    skipped.Add(new LayoutRestorePlanSkippedItem(item, "monitor-missing"));
                    continue;
                }

                if (!IsRestorableNormalizedRect(saved))
                {
                    skipped.Add(new LayoutRestorePlanSkippedItem(item, "geometry-invalid"));
                    continue;
                }

                Rectangle workArea = targetScreen.WorkingArea;
                Rectangle targetRect = new Rectangle(
                    workArea.Left + Denormalize(saved.NormalizedX, workArea.Width),
                    workArea.Top + Denormalize(saved.NormalizedY, workArea.Height),
                    Denormalize(saved.NormalizedWidth, workArea.Width),
                    Denormalize(saved.NormalizedHeight, workArea.Height));

                planned.Add(new LayoutRestorePlanItem(
                    item,
                    targetScreen.DeviceName,
                    workArea,
                    targetRect));
            }

            return new LayoutRestorePlanResult(
                true,
                true,
                match.FilePath,
                null,
                match.SavedWindowCount,
                match.CurrentWindowCount,
                planned,
                new List<WindowSnapshotEntry>(match.Missing),
                skipped,
                new List<CurrentWindowSnapshot>(match.CurrentOnly));
        }

        /// <summary>
        /// 저장된 snapshot을 현재 열려 있고 매칭된 창에 실제 적용한다.
        /// 앱을 실행하지 않으며 missing/current-only 창은 건드리지 않는다.
        /// </summary>
        public static LayoutRestoreResult RestoreSaved()
        {
            LayoutRestorePlanResult plan = BuildRestorePlan();
            if (!plan.Ready)
            {
                return new LayoutRestoreResult(
                    false,
                    plan.FileExists,
                    plan.FilePath,
                    plan.Error,
                    0,
                    0,
                    null,
                    null,
                    null,
                    null,
                    null);
            }

            List<LayoutRestoreAppliedItem> applied =
                new List<LayoutRestoreAppliedItem>();
            List<LayoutRestoreFailedItem> failed =
                new List<LayoutRestoreFailedItem>();

            for (int i = 0; i < plan.Planned.Count; i++)
            {
                LayoutRestorePlanItem item = plan.Planned[i];
                IntPtr hwnd = item.Match.Current.Handle;
                WindowSnapshotEntry saved = item.Match.Saved;

                if (!Native.IsWindow(hwnd))
                {
                    failed.Add(new LayoutRestoreFailedItem(item, "window-gone"));
                    continue;
                }

                if (Native.IsIconic(hwnd) || Native.IsZoomed(hwnd))
                {
                    Native.ShowWindow(hwnd, Native.SW_RESTORE);
                }

                if (!WindowManager.MoveToStable(
                    hwnd,
                    item.TargetRect,
                    item.TargetMonitorDeviceName))
                {
                    failed.Add(new LayoutRestoreFailedItem(item, "move-failed"));
                    continue;
                }

                if (saved.Maximized)
                {
                    Native.ShowWindow(hwnd, Native.SW_SHOWMAXIMIZED);
                }

                Rectangle actual = WindowManager.GetVisualRect(hwnd);
                bool isMaximized = Native.IsZoomed(hwnd);

                if (saved.Maximized)
                {
                    if (!isMaximized)
                    {
                        failed.Add(new LayoutRestoreFailedItem(item, "maximize-failed"));
                        continue;
                    }
                }
                else
                {
                    if (isMaximized)
                    {
                        failed.Add(new LayoutRestoreFailedItem(item, "unexpected-maximized"));
                        continue;
                    }

                    if (!RectApproximatelyEquals(actual, item.TargetRect, 8))
                    {
                        failed.Add(new LayoutRestoreFailedItem(item, "position-mismatch"));
                        continue;
                    }
                }

                applied.Add(new LayoutRestoreAppliedItem(item, actual, isMaximized));
            }

            return new LayoutRestoreResult(
                true,
                true,
                plan.FilePath,
                null,
                plan.SavedWindowCount,
                plan.CurrentWindowCount,
                applied,
                failed,
                new List<WindowSnapshotEntry>(plan.MissingWindows),
                new List<LayoutRestorePlanSkippedItem>(plan.Skipped),
                new List<CurrentWindowSnapshot>(plan.CurrentOnly));
        }

        private static bool RectApproximatelyEquals(
            Rectangle actual,
            Rectangle expected,
            int tolerance)
        {
            if (actual.IsEmpty)
            {
                return false;
            }

            return Math.Abs(actual.Left - expected.Left) <= tolerance
                && Math.Abs(actual.Top - expected.Top) <= tolerance
                && Math.Abs(actual.Width - expected.Width) <= tolerance
                && Math.Abs(actual.Height - expected.Height) <= tolerance;
        }

        private static bool IsRestorableNormalizedRect(WindowSnapshotEntry entry)
        {
            if (entry.NormalizedX < 0 || entry.NormalizedY < 0
                || entry.NormalizedWidth <= 0 || entry.NormalizedHeight <= 0)
            {
                return false;
            }

            if (entry.NormalizedX > CoordinateScale
                || entry.NormalizedY > CoordinateScale
                || entry.NormalizedWidth > CoordinateScale
                || entry.NormalizedHeight > CoordinateScale)
            {
                return false;
            }

            return (long)entry.NormalizedX + entry.NormalizedWidth <= CoordinateScale
                && (long)entry.NormalizedY + entry.NormalizedHeight <= CoordinateScale;
        }

        private static int Denormalize(int value, int total)
        {
            if (total <= 0)
            {
                return 0;
            }

            return (int)Math.Round(
                (double)value * total / CoordinateScale,
                MidpointRounding.AwayFromZero);
        }

        private static LayoutSnapshotMatchResult MatchFailure(
            bool fileExists,
            string path,
            string error)
        {
            return new LayoutSnapshotMatchResult(
                false,
                fileExists,
                path,
                error,
                0,
                0,
                0,
                null,
                null,
                null);
        }

        private static string BuildIdentityKey(WindowSnapshotEntry entry)
        {
            return entry.ProcessName + "\0"
                + entry.WindowClass + "\0"
                + entry.InstanceIndex.ToString(CultureInfo.InvariantCulture);
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
