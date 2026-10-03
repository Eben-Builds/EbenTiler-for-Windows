using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace EbenTilerWindows
{
    /// <summary>기본 트레이 아이콘 오른쪽 위에 업데이트 느낌표 배지를 합성한다.</summary>
    internal static class TrayUpdateIcon
    {
        public static Icon Create(bool hasUpdate)
        {
            if (!hasUpdate) return AppIcon.LoadSmall();

            const int size = 32;
            using (Icon baseIcon = AppIcon.LoadSized(size))
            using (Bitmap bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                g.DrawIcon(baseIcon, new Rectangle(0, 0, size, size));

                RectangleF badge = new RectangleF(18f, 0f, 14f, 14f);
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(80, 0, 0, 0)))
                {
                    g.FillEllipse(shadow, new RectangleF(19f, 1f, 13f, 13f));
                }
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(245, 139, 28)))
                {
                    g.FillEllipse(fill, badge);
                }
                using (Pen border = new Pen(Color.White, 1.3f))
                {
                    g.DrawEllipse(border, badge);
                }

                using (Pen mark = new Pen(Color.White, 2.3f))
                {
                    mark.StartCap = LineCap.Round;
                    mark.EndCap = LineCap.Round;
                    g.DrawLine(mark, 25f, 3.5f, 25f, 8.4f);
                }
                using (SolidBrush dot = new SolidBrush(Color.White))
                {
                    g.FillEllipse(dot, 23.8f, 10.1f, 2.4f, 2.4f);
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    using (Icon raw = Icon.FromHandle(handle))
                    {
                        return (Icon)raw.Clone();
                    }
                }
                finally
                {
                    Native.DestroyIcon(handle);
                }
            }
        }
    }
}
