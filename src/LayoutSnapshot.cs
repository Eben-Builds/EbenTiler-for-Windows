using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
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
