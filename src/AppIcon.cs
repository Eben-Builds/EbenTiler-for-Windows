using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>
    /// 실행 파일에 박아 둔 아이콘을 꺼내 쓴다.
    /// 알림 영역은 작은 아이콘, 설정 창은 큰 아이콘을 사용한다.
    /// </summary>
    public static class AppIcon
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint PrivateExtractIcons(
            string fileName, int iconIndex, int cxIcon, int cyIcon,
            IntPtr[] iconHandles, uint[] iconIds, uint iconCount, uint flags);

        /// <summary>알림 영역용 작은 아이콘.</summary>
        public static Icon LoadSmall()
        {
            Icon icon = Extract(true);
            if (icon != null)
            {
                return icon;
            }
            return Fallback(32);
        }

        /// <summary>창 제목 표시줄과 작업 전환기에 쓸 큰 아이콘.</summary>
        public static Icon LoadLarge()
        {
            Icon icon = Extract(false);
            if (icon != null)
            {
                return icon;
            }
            return Fallback(64);
        }

        /// <summary>
        /// 설정 창처럼 정확한 물리 픽셀 크기가 필요한 곳에서 쓸 아이콘.
        /// EXE에 포함된 멀티사이즈 ICO에서 요청 크기에 맞는 프레임을 직접 꺼낸다.
        /// </summary>
        public static Icon LoadSized(int pixelSize)
        {
            int size = Math.Max(16, pixelSize);
            Icon icon = ExtractSized(size);
            if (icon != null)
            {
                return icon;
            }
            return Fallback(size);
        }

        private static Icon ExtractSized(int pixelSize)
        {
            IntPtr[] handles = new IntPtr[1];
            uint[] ids = new uint[1];
            try
            {
                uint count = PrivateExtractIcons(
                    Application.ExecutablePath, 0, pixelSize, pixelSize,
                    handles, ids, 1, 0);

                if (count > 0 && count != uint.MaxValue && handles[0] != IntPtr.Zero)
                {
                    using (Icon raw = Icon.FromHandle(handles[0]))
                    {
                        return (Icon)raw.Clone();
                    }
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                if (handles[0] != IntPtr.Zero)
                {
                    Native.DestroyIcon(handles[0]);
                }
            }
            return null;
        }

        private static Icon Extract(bool small)
        {
            try
            {
                IntPtr[] largeHandles = new IntPtr[1];
                IntPtr[] smallHandles = new IntPtr[1];
                uint count = Native.ExtractIconEx(Application.ExecutablePath, 0,
                    largeHandles, smallHandles, 1);

                IntPtr wanted = small ? smallHandles[0] : largeHandles[0];
                IntPtr other = small ? largeHandles[0] : smallHandles[0];

                if (count > 0 && wanted != IntPtr.Zero)
                {
                    Icon copy;
                    using (Icon raw = Icon.FromHandle(wanted))
                    {
                        copy = (Icon)raw.Clone();
                    }
                    Native.DestroyIcon(wanted);
                    if (other != IntPtr.Zero) { Native.DestroyIcon(other); }
                    return copy;
                }

                if (largeHandles[0] != IntPtr.Zero) { Native.DestroyIcon(largeHandles[0]); }
                if (smallHandles[0] != IntPtr.Zero) { Native.DestroyIcon(smallHandles[0]); }
            }
            catch (Exception)
            {
            }

            try
            {
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 실행 파일에서 아이콘을 못 꺼냈을 때 쓸 대체 그림.
        /// 랜딩페이지 파비콘과 같은 '큰 창 + 오른쪽 두 영역 + 화살표' 형태를 사용한다.
        /// </summary>
        private static Icon Fallback(int size)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                float scale = size / 64f;
                RectangleF main = new RectangleF(5f * scale, 8f * scale, 34f * scale, 48f * scale);
                RectangleF top = new RectangleF(42f * scale, 14f * scale, 17f * scale, 19f * scale);
                RectangleF bottom = new RectangleF(42f * scale, 37f * scale, 17f * scale, 19f * scale);

                using (LinearGradientBrush gradient = new LinearGradientBrush(
                    main,
                    Color.FromArgb(10, 133, 255),
                    Color.FromArgb(0, 90, 216),
                    LinearGradientMode.ForwardDiagonal))
                using (GraphicsPath mainPath = UiDrawing.RoundedRectangle(main, 9f * scale))
                {
                    g.FillPath(gradient, mainPath);
                }

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(102, 181, 255)))
                using (GraphicsPath path = UiDrawing.RoundedRectangle(top, 6f * scale))
                {
                    g.FillPath(brush, path);
                }

                using (SolidBrush brush = new SolidBrush(Color.FromArgb(32, 139, 244)))
                using (GraphicsPath path = UiDrawing.RoundedRectangle(bottom, 6f * scale))
                {
                    g.FillPath(brush, path);
                }

                float stroke = Math.Max(2f, 5f * scale);
                using (Pen arrow = new Pen(Color.White, stroke))
                {
                    arrow.StartCap = LineCap.Round;
                    arrow.EndCap = LineCap.Round;
                    arrow.LineJoin = LineJoin.Round;
                    g.DrawLines(arrow, new PointF[] {
                        new PointF(25f * scale, 24f * scale),
                        new PointF(34f * scale, 32f * scale),
                        new PointF(25f * scale, 40f * scale)
                    });
                }
            }

            IntPtr handle = bitmap.GetHicon();
            Icon icon;
            using (Icon raw = Icon.FromHandle(handle))
            {
                icon = (Icon)raw.Clone();
            }
            Native.DestroyIcon(handle);
            bitmap.Dispose();
            return icon;
        }
    }
}
