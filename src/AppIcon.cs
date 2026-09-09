using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RectangleWindows
{
    /// <summary>
    /// 실행 파일에 박아 둔 아이콘을 꺼내 쓴다.
    /// 알림 영역은 16픽셀, 창 제목 표시줄은 32픽셀을 쓰는데,
    /// 큰 그림 하나를 줄여 쓰면 흐려지므로 크기에 맞는 그림을 직접 꺼낸다.
    /// </summary>
    public static class AppIcon
    {
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
            return Fallback(32);
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

        /// <summary>실행 파일에서 아이콘을 못 꺼냈을 때 쓸 대체 그림. 겹친 두 창 모양이다.</summary>
        private static Icon Fallback(int size)
        {
            Bitmap bitmap = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.Transparent);

                Color blue = Color.FromArgb(255, 37, 99, 235);
                int pen = Math.Max(2, (int)Math.Round(size * 0.07));
                int edge = (int)Math.Round(size * 0.03);
                int back = (int)Math.Round(size * 0.58);
                int front = (int)Math.Round(size * 0.64);
                int fx = size - edge - front;

                using (Pen p = new Pen(blue, pen))
                {
                    p.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(p, edge, edge, back - 1, back - 1);
                }
                using (SolidBrush clear = new SolidBrush(Color.Transparent))
                {
                    g.CompositingMode = CompositingMode.SourceCopy;
                    g.FillRectangle(clear, fx - 2, fx - 2, front + 4, front + 4);
                    g.CompositingMode = CompositingMode.SourceOver;
                }

                int half = front / 2;
                using (SolidBrush b = new SolidBrush(blue))
                {
                    g.FillRectangle(b, fx, fx, half, front);
                }
                using (SolidBrush w = new SolidBrush(Color.White))
                {
                    g.FillRectangle(w, fx + half, fx, front - half, front);
                }
                using (Pen p = new Pen(blue, pen))
                {
                    p.Alignment = PenAlignment.Inset;
                    g.DrawRectangle(p, fx, fx, front - 1, front - 1);
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
