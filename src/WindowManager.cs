using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>실제로 창을 찾아 옮기고 크기를 바꾸는 부분.</summary>
    public sealed class WindowManager
    {
        private const int MinWidth = 240;
        private const int MinHeight = 160;

        private readonly Dictionary<IntPtr, Native.WINDOWPLACEMENT> _originalPlacements
            = new Dictionary<IntPtr, Native.WINDOWPLACEMENT>();

        private IntPtr _cycleWindow = IntPtr.Zero;
        private SnapAction _cycleAction = SnapAction.Restore;
        private int _cycleIndex = 0;
        private DateTime _cycleTime = DateTime.MinValue;

        private readonly Config _config;

        public WindowManager(Config config)
        {
            _config = config;
        }

        /// <summary>명령을 현재 활성 창에 적용한다. 적용했으면 true.</summary>
        public bool Apply(SnapAction action)
        {
            return ApplyTo(action, GetTargetWindow());
        }

        /// <summary>창을 직접 지정해서 명령을 적용한다. 명령줄 실행과 자동 검증에 쓴다.</summary>
        public bool ApplyTo(SnapAction action, IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd))
            {
                return false;
            }

            PruneDeadWindows();

            if (action == SnapAction.Restore)
            {
                return RestoreOriginal(hwnd);
            }

            RememberOriginal(hwnd);

            if (action == SnapAction.Maximize)
            {
                Native.ShowWindow(hwnd, Native.SW_SHOWMAXIMIZED);
                ResetCycle();
                return true;
            }

            if (action == SnapAction.NextDisplay || action == SnapAction.PreviousDisplay)
            {
                ResetCycle();
                return MoveToAdjacentDisplay(hwnd, action == SnapAction.NextDisplay);
            }

            if (Native.IsIconic(hwnd) || Native.IsZoomed(hwnd))
            {
                Native.ShowWindow(hwnd, Native.SW_RESTORE);
            }

            Rectangle work = Screen.FromHandle(hwnd).WorkingArea;
            Rectangle current = GetVisualRect(hwnd);
            Rectangle target = Calculate(action, work, current, hwnd);
            MoveTo(hwnd, target);
            return true;
        }

        /// <summary>명령과 화면 작업 영역으로부터 창이 놓일 자리를 계산한다.</summary>
        private Rectangle Calculate(SnapAction action, Rectangle work, Rectangle current, IntPtr hwnd)
        {
            int gap = _config.Gap;
            Rectangle area = Deflate(work, gap / 2);

            Rectangle result;
            switch (action)
            {
                case SnapAction.LeftHalf:
                    result = HorizontalSlice(area, action, hwnd, true);
                    break;
                case SnapAction.RightHalf:
                    result = HorizontalSlice(area, action, hwnd, false);
                    break;
                case SnapAction.TopHalf:
                    result = VerticalSlice(area, action, hwnd, true);
                    break;
                case SnapAction.BottomHalf:
                    result = VerticalSlice(area, action, hwnd, false);
                    break;

                case SnapAction.TopLeft:
                    result = new Rectangle(area.Left, area.Top, area.Width / 2, area.Height / 2);
                    break;
                case SnapAction.TopRight:
                    result = new Rectangle(area.Left + area.Width / 2, area.Top,
                        area.Width - area.Width / 2, area.Height / 2);
                    break;
                case SnapAction.BottomLeft:
                    result = new Rectangle(area.Left, area.Top + area.Height / 2,
                        area.Width / 2, area.Height - area.Height / 2);
                    break;
                case SnapAction.BottomRight:
                    result = new Rectangle(area.Left + area.Width / 2, area.Top + area.Height / 2,
                        area.Width - area.Width / 2, area.Height - area.Height / 2);
                    break;

                case SnapAction.FirstThird:
                    result = new Rectangle(area.Left, area.Top, area.Width / 3, area.Height);
                    break;
                case SnapAction.CenterThird:
                    result = new Rectangle(area.Left + area.Width / 3, area.Top, area.Width / 3, area.Height);
                    break;
                case SnapAction.LastThird:
                    result = new Rectangle(area.Left + (area.Width * 2) / 3, area.Top,
                        area.Width - (area.Width * 2) / 3, area.Height);
                    break;
                case SnapAction.FirstTwoThirds:
                    result = new Rectangle(area.Left, area.Top, (area.Width * 2) / 3, area.Height);
                    break;
                case SnapAction.LastTwoThirds:
                    result = new Rectangle(area.Left + area.Width / 3, area.Top,
                        area.Width - area.Width / 3, area.Height);
                    break;

                case SnapAction.MaximizeHeight:
                    result = new Rectangle(current.Left, area.Top, current.Width, area.Height);
                    ResetCycle();
                    break;
                case SnapAction.Center:
                    result = new Rectangle(
                        area.Left + (area.Width - current.Width) / 2,
                        area.Top + (area.Height - current.Height) / 2,
                        current.Width, current.Height);
                    ResetCycle();
                    break;
                case SnapAction.Larger:
                    result = Resize(area, current, 1);
                    ResetCycle();
                    break;
                case SnapAction.Smaller:
                    result = Resize(area, current, -1);
                    ResetCycle();
                    break;

                default:
                    result = current;
                    break;
            }

            if (gap > 0 && IsTiling(action))
            {
                result = Deflate(result, gap / 2);
            }

            return Clamp(result, work);
        }

        private static bool IsTiling(SnapAction action)
        {
            switch (action)
            {
                case SnapAction.Center:
                case SnapAction.Larger:
                case SnapAction.Smaller:
                    return false;
                default:
                    return true;
            }
        }

        /// <summary>같은 단축키를 이어서 누르면 사용자가 정한 세 비율을 순서대로 적용한다.</summary>
        private double NextFraction(SnapAction action, IntPtr hwnd)
        {
            if (!_config.CycleHalves)
            {
                return RatioToFraction(_config.CycleRatio1);
            }

            bool sameContext = hwnd == _cycleWindow
                && action == _cycleAction
                && (DateTime.UtcNow - _cycleTime).TotalSeconds < 2.0;

            if (sameContext)
            {
                _cycleIndex = (_cycleIndex + 1) % 3;
            }
            else
            {
                _cycleIndex = 0;
            }

            _cycleWindow = hwnd;
            _cycleAction = action;
            _cycleTime = DateTime.UtcNow;

            if (_cycleIndex == 1) return RatioToFraction(_config.CycleRatio2);
            if (_cycleIndex == 2) return RatioToFraction(_config.CycleRatio3);
            return RatioToFraction(_config.CycleRatio1);
        }

        private static double RatioToFraction(int ratio)
        {
            int safeRatio = Math.Max(20, Math.Min(80, ratio));
            return safeRatio / 100.0;
        }

        private void ResetCycle()
        {
            _cycleWindow = IntPtr.Zero;
            _cycleTime = DateTime.MinValue;
            _cycleIndex = 0;
        }

        private Rectangle HorizontalSlice(Rectangle area, SnapAction action, IntPtr hwnd, bool alignLeft)
        {
            double fraction = NextFraction(action, hwnd);
            int width = (int)Math.Round(area.Width * fraction);
            if (alignLeft)
            {
                return new Rectangle(area.Left, area.Top, width, area.Height);
            }
            return new Rectangle(area.Right - width, area.Top, width, area.Height);
        }

        private Rectangle VerticalSlice(Rectangle area, SnapAction action, IntPtr hwnd, bool alignTop)
        {
            double fraction = NextFraction(action, hwnd);
            int height = (int)Math.Round(area.Height * fraction);
            if (alignTop)
            {
                return new Rectangle(area.Left, area.Top, area.Width, height);
            }
            return new Rectangle(area.Left, area.Bottom - height, area.Width, height);
        }

        private static Rectangle Resize(Rectangle area, Rectangle current, int direction)
        {
            int stepX = Math.Max(20, area.Width / 20);
            int stepY = Math.Max(20, area.Height / 20);

            int width = current.Width + direction * stepX;
            int height = current.Height + direction * stepY;

            width = Math.Max(MinWidth, Math.Min(area.Width, width));
            height = Math.Max(MinHeight, Math.Min(area.Height, height));

            int centerX = current.Left + current.Width / 2;
            int centerY = current.Top + current.Height / 2;
            return new Rectangle(centerX - width / 2, centerY - height / 2, width, height);
        }

        private static Rectangle Deflate(Rectangle rect, int amount)
        {
            if (amount <= 0)
            {
                return rect;
            }
            Rectangle result = new Rectangle(
                rect.Left + amount,
                rect.Top + amount,
                rect.Width - amount * 2,
                rect.Height - amount * 2);
            if (result.Width < MinWidth) { result.Width = Math.Min(rect.Width, MinWidth); }
            if (result.Height < MinHeight) { result.Height = Math.Min(rect.Height, MinHeight); }
            return result;
        }

        private static Rectangle Clamp(Rectangle rect, Rectangle work)
        {
            int width = Math.Max(MinWidth, Math.Min(rect.Width, work.Width));
            int height = Math.Max(MinHeight, Math.Min(rect.Height, work.Height));
            int x = Math.Max(work.Left, Math.Min(rect.Left, work.Right - width));
            int y = Math.Max(work.Top, Math.Min(rect.Top, work.Bottom - height));
            return new Rectangle(x, y, width, height);
        }

        // 모니터 이동

        private bool MoveToAdjacentDisplay(IntPtr hwnd, bool forward)
        {
            Screen[] screens = OrderedScreens();
            if (screens.Length < 2)
            {
                return false;
            }

            Screen currentScreen = Screen.FromHandle(hwnd);
            int index = -1;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].DeviceName == currentScreen.DeviceName)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
            {
                index = 0;
            }

            int nextIndex = forward
                ? (index + 1) % screens.Length
                : (index - 1 + screens.Length) % screens.Length;

            Rectangle from = screens[index].WorkingArea;
            Rectangle to = screens[nextIndex].WorkingArea;

            bool wasMaximized = Native.IsZoomed(hwnd);
            if (wasMaximized || Native.IsIconic(hwnd))
            {
                Native.ShowWindow(hwnd, Native.SW_RESTORE);
            }

            Rectangle current = GetVisualRect(hwnd);

            // 원래 모니터에서 차지하던 비율을 그대로 새 모니터에 옮긴다.
            double relX = from.Width > 0 ? (double)(current.Left - from.Left) / from.Width : 0;
            double relY = from.Height > 0 ? (double)(current.Top - from.Top) / from.Height : 0;
            double relW = from.Width > 0 ? (double)current.Width / from.Width : 0.5;
            double relH = from.Height > 0 ? (double)current.Height / from.Height : 0.5;

            Rectangle target = new Rectangle(
                to.Left + (int)Math.Round(to.Width * relX),
                to.Top + (int)Math.Round(to.Height * relY),
                (int)Math.Round(to.Width * relW),
                (int)Math.Round(to.Height * relH));

            target = Clamp(target, to);

            // 모니터 배율이 다르면 첫 호출 뒤 Windows가 크기를 다시 조정하므로 두 번 적용한다.
            MoveTo(hwnd, target);
            MoveTo(hwnd, target);

            if (wasMaximized)
            {
                Native.ShowWindow(hwnd, Native.SW_SHOWMAXIMIZED);
            }
            return true;
        }

        private static Screen[] OrderedScreens()
        {
            Screen[] screens = Screen.AllScreens;
            Array.Sort(screens, delegate(Screen a, Screen b)
            {
                if (a.Bounds.X != b.Bounds.X)
                {
                    return a.Bounds.X.CompareTo(b.Bounds.X);
                }
                return a.Bounds.Y.CompareTo(b.Bounds.Y);
            });
            return screens;
        }

        // 원래 크기 기억과 복원

        private void RememberOriginal(IntPtr hwnd)
        {
            if (_originalPlacements.ContainsKey(hwnd))
            {
                return;
            }

            Native.WINDOWPLACEMENT placement = new Native.WINDOWPLACEMENT();
            placement.length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.WINDOWPLACEMENT));
            if (Native.GetWindowPlacement(hwnd, ref placement))
            {
                _originalPlacements[hwnd] = placement;
            }
        }

        private bool RestoreOriginal(IntPtr hwnd)
        {
            ResetCycle();

            Native.WINDOWPLACEMENT placement;
            if (_originalPlacements.TryGetValue(hwnd, out placement))
            {
                _originalPlacements.Remove(hwnd);
                if (Native.IsIconic(hwnd) || Native.IsZoomed(hwnd))
                {
                    Native.ShowWindow(hwnd, Native.SW_RESTORE);
                }
                placement.length = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.WINDOWPLACEMENT));
                placement.flags = 0;
                if (placement.showCmd == Native.SW_SHOWMINIMIZED)
                {
                    placement.showCmd = Native.SW_SHOWNORMAL;
                }
                return Native.SetWindowPlacement(hwnd, ref placement);
            }

            // 기억해 둔 크기가 없으면 최대화만 해제한다.
            if (Native.IsZoomed(hwnd))
            {
                Native.ShowWindow(hwnd, Native.SW_RESTORE);
                return true;
            }
            return false;
        }

        private void PruneDeadWindows()
        {
            if (_originalPlacements.Count == 0)
            {
                return;
            }

            List<IntPtr> dead = new List<IntPtr>();
            foreach (KeyValuePair<IntPtr, Native.WINDOWPLACEMENT> pair in _originalPlacements)
            {
                if (!Native.IsWindow(pair.Key))
                {
                    dead.Add(pair.Key);
                }
            }
            for (int i = 0; i < dead.Count; i++)
            {
                _originalPlacements.Remove(dead[i]);
            }
        }

        // 창 좌표 계산

        /// <summary>눈에 보이는 창 테두리 기준 사각형. Windows의 투명한 여백을 뺀 값이다.</summary>
        public static Rectangle GetVisualRect(IntPtr hwnd)
        {
            Native.RECT rect;
            int hr = Native.DwmGetWindowAttribute(hwnd, Native.DWMWA_EXTENDED_FRAME_BOUNDS,
                out rect, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.RECT)));
            if (hr != 0)
            {
                if (!Native.GetWindowRect(hwnd, out rect))
                {
                    return Rectangle.Empty;
                }
            }
            return new Rectangle(rect.Left, rect.Top, rect.Width, rect.Height);
        }

        /// <summary>보이는 테두리가 목표 사각형에 딱 맞도록 투명 여백만큼 보정해서 옮긴다.</summary>
        public static void MoveTo(IntPtr hwnd, Rectangle target)
        {
            Native.RECT outer;
            if (!Native.GetWindowRect(hwnd, out outer))
            {
                return;
            }

            int left = target.Left;
            int top = target.Top;
            int width = target.Width;
            int height = target.Height;

            Native.RECT visual;
            int hr = Native.DwmGetWindowAttribute(hwnd, Native.DWMWA_EXTENDED_FRAME_BOUNDS,
                out visual, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Native.RECT)));
            if (hr == 0)
            {
                left -= (visual.Left - outer.Left);
                top -= (visual.Top - outer.Top);
                width += (outer.Width - visual.Width);
                height += (outer.Height - visual.Height);
            }

            Native.SetWindowPos(hwnd, IntPtr.Zero, left, top, width, height,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE);
        }

        // 대상 창 고르기

        /// <summary>배치 가능한 활성 창을 고른다. 시스템 셸/일시적 팝업은 제외하고 일반 borderless 앱 창은 허용한다.</summary>
        public static IntPtr GetTargetWindow()
        {
            IntPtr hwnd = Native.GetForegroundWindow();
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd) || !Native.IsWindowVisible(hwnd))
            {
                return IntPtr.Zero;
            }

            long style = Native.GetWindowLongSafe(hwnd, Native.GWL_STYLE);
            if ((style & Native.WS_CHILD) != 0)
            {
                return IntPtr.Zero;
            }

            long exStyle = Native.GetWindowLongSafe(hwnd, Native.GWL_EXSTYLE);
            if ((exStyle & Native.WS_EX_TOOLWINDOW) != 0)
            {
                return IntPtr.Zero;
            }

            int cloaked;
            if (Native.DwmGetWindowAttribute(hwnd, Native.DWMWA_CLOAKED, out cloaked, sizeof(int)) == 0
                && cloaked != 0)
            {
                return IntPtr.Zero;
            }

            StringBuilder className = new StringBuilder(256);
            Native.GetClassName(hwnd, className, className.Capacity);
            string name = className.ToString();
            if (IsBlockedWindowClass(name))
            {
                return IntPtr.Zero;
            }

            // 제목 표시줄이 없는 Electron/Chromium/커스텀 프레임 창도 실제 앱 창이면 허용한다.
            // 대신 아주 작은 메뉴/오버레이가 실수로 화면 전체에 타일링되는 것은 막는다.
            Rectangle visual = GetVisualRect(hwnd);
            if (visual.IsEmpty || visual.Width < 80 || visual.Height < 60)
            {
                return IntPtr.Zero;
            }

            return hwnd;
        }

        private static bool IsBlockedWindowClass(string name)
        {
            return name == "Progman"
                || name == "WorkerW"
                || name == "Shell_TrayWnd"
                || name == "Shell_SecondaryTrayWnd"
                || name == "Windows.UI.Core.CoreWindow"
                || name == "MultitaskingViewFrame"
                || name == "#32768"
                || name == "tooltips_class32"
                || name == "SysShadow"
                || name == "TaskListThumbnailWnd"
                || name == "TaskSwitcherWnd";
        }
    }
}
