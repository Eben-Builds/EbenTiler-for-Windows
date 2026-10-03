using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>
    /// EbenTiler UI의 공통 색상과 둥근 모서리 그리기 도우미.
    /// Windows의 Fluent 계열 원칙을 WinForms 범위 안에서 가볍게 적용한다.
    /// </summary>
    internal static class UiPalette
    {
        public static readonly Color Canvas = Color.FromArgb(247, 249, 252);
        public static readonly Color Surface = Color.White;
        public static readonly Color SurfaceSoft = Color.FromArgb(244, 248, 253);
        public static readonly Color Primary = Color.FromArgb(10, 124, 245);
        public static readonly Color PrimaryDark = Color.FromArgb(0, 94, 214);
        public static readonly Color PrimarySoft = Color.FromArgb(232, 243, 255);
        public static readonly Color Text = Color.FromArgb(22, 34, 56);
        public static readonly Color TextMuted = Color.FromArgb(101, 116, 139);
        public static readonly Color Border = Color.FromArgb(220, 230, 241);
        public static readonly Color BorderStrong = Color.FromArgb(197, 214, 232);
        public static readonly Color Hover = Color.FromArgb(237, 245, 255);
        public static readonly Color Danger = Color.FromArgb(180, 55, 55);
    }

    internal static class UiDrawing
    {
        public static GraphicsPath RoundedRectangle(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = Math.Min(radius * 2f, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0f)
            {
                path.AddRectangle(bounds);
                path.CloseFigure();
                return path;
            }

            RectangleF arc = new RectangleF(bounds.X, bounds.Y, diameter, diameter);
            path.AddArc(arc, 180f, 90f);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270f, 90f);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0f, 90f);
            arc.X = bounds.Left;
            path.AddArc(arc, 90f, 90f);
            path.CloseFigure();
            return path;
        }

        public static void FillRoundedRectangle(Graphics g, Brush brush, RectangleF bounds, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(Graphics g, Pen pen, RectangleF bounds, float radius)
        {
            using (GraphicsPath path = RoundedRectangle(bounds, radius))
            {
                g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>가벼운 Fluent 스타일의 둥근 버튼.</summary>
    internal sealed class RoundedButton : Button
    {
        private bool _hover;
        private bool _pressed;

        public bool PrimaryStyle { get; set; }
        public int CornerRadius { get; set; }
        public Color SurroundingBackColor { get; set; }

        public RoundedButton()
        {
            CornerRadius = 8;
            SurroundingBackColor = Color.Empty;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            TabStop = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            _pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs mevent)
        {
            _pressed = true;
            Invalidate();
            base.OnMouseDown(mevent);
        }

        protected override void OnMouseUp(MouseEventArgs mevent)
        {
            _pressed = false;
            Invalidate();
            base.OnMouseUp(mevent);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Color surrounding = !SurroundingBackColor.IsEmpty
                ? SurroundingBackColor
                : (Parent != null ? Parent.BackColor : UiPalette.Canvas);

            g.Clear(surrounding);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color fill;
            Color border;
            Color text;
            if (!Enabled)
            {
                fill = Color.FromArgb(239, 243, 248);
                border = UiPalette.Border;
                text = Color.FromArgb(155, 166, 180);
            }
            else if (PrimaryStyle)
            {
                fill = _pressed ? UiPalette.PrimaryDark
                    : (_hover ? Color.FromArgb(0, 111, 231) : UiPalette.Primary);
                border = fill;
                text = Color.White;
            }
            else
            {
                fill = _pressed ? Color.FromArgb(227, 237, 248)
                    : (_hover ? UiPalette.Hover : UiPalette.Surface);
                border = UiPalette.BorderStrong;
                text = UiPalette.Text;
            }

            RectangleF rect = new RectangleF(
                1f, 1f,
                Math.Max(1f, Width - 2f),
                Math.Max(1f, Height - 2f));
            float radius = Math.Max(2f, CornerRadius - 0.5f);

            using (SolidBrush brush = new SolidBrush(fill))
            using (Pen pen = new Pen(border, 1f))
            {
                pen.Alignment = PenAlignment.Inset;
                UiDrawing.FillRoundedRectangle(g, brush, rect, radius);
                UiDrawing.DrawRoundedRectangle(g, pen, rect, radius);
            }

            TextRenderer.DrawText(
                g, Text, Font, ClientRectangle, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);

            if (Focused && ShowFocusCues && Width > 10 && Height > 10)
            {
                RectangleF focusRect = new RectangleF(4f, 4f, Width - 8f, Height - 8f);
                Color focusColor = PrimaryStyle ? Color.FromArgb(220, 255, 255, 255) : UiPalette.Primary;
                using (Pen focusPen = new Pen(focusColor, 1f))
                {
                    focusPen.DashStyle = DashStyle.Dot;
                    UiDrawing.DrawRoundedRectangle(
                        g, focusPen, focusRect, Math.Max(2f, radius - 3f));
                }
            }
        }
    }

    /// <summary>설정 창 왼쪽 사이드바용 탐색 버튼.</summary>
    internal sealed class NavigationButton : Button
    {
        private bool _hover;
        private bool _selected;

        // 이전 버전과의 소스 호환을 위해 남겨 두되 실제 렌더링은 벡터 아이콘을 사용한다.
        public string Glyph { get; set; }
        public bool Selected
        {
            get { return _selected; }
            set
            {
                if (_selected == value) return;
                _selected = value;
                Invalidate();
            }
        }

        public NavigationButton()
        {
            Glyph = "";
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            Cursor = Cursors.Hand;
            TabStop = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            Graphics g = pevent.Graphics;
            Color surrounding = Parent != null ? Parent.BackColor : UiPalette.Surface;
            g.Clear(surrounding);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            RectangleF rect = new RectangleF(1f, 1f, Math.Max(1f, Width - 2f), Math.Max(1f, Height - 2f));
            Color fill = _selected ? UiPalette.PrimarySoft : (_hover ? UiPalette.SurfaceSoft : UiPalette.Surface);
            Color textColor = _selected ? UiPalette.PrimaryDark : UiPalette.Text;
            using (SolidBrush fillBrush = new SolidBrush(fill))
            {
                UiDrawing.FillRoundedRectangle(g, fillBrush, rect, 8f);
            }

            if (_selected)
            {
                RectangleF accent = new RectangleF(5f, 9f, 3f, Math.Max(10f, Height - 18f));
                using (SolidBrush accentBrush = new SolidBrush(UiPalette.Primary))
                {
                    UiDrawing.FillRoundedRectangle(g, accentBrush, accent, 2f);
                }
            }

            int iconSize = Math.Max(17, Math.Min(21, Font.Height + 4));
            Rectangle iconRect = new Rectangle(19, Math.Max(0, (Height - iconSize) / 2), iconSize, iconSize);
            DrawNavigationIcon(g, iconRect, textColor);

            Rectangle textRect = new Rectangle(48, 0, Math.Max(1, Width - 56), Height);
            TextRenderer.DrawText(g, Text, Font, textRect, textColor,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.SingleLine | TextFormatFlags.NoPadding);
        }

        private void DrawNavigationIcon(Graphics g, Rectangle bounds, Color color)
        {
            float x = bounds.X + 1.5f;
            float y = bounds.Y + 1.5f;
            float w = Math.Max(10f, bounds.Width - 3f);
            float h = Math.Max(10f, bounds.Height - 3f);
            float stroke = Math.Max(1.35f, bounds.Width / 13f);

            using (Pen pen = new Pen(color, stroke))
            using (SolidBrush brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                if (Text == "일반")
                {
                    // Fluent 계열의 조절 슬라이더 아이콘.
                    float y1 = y + h * 0.26f;
                    float y2 = y + h * 0.50f;
                    float y3 = y + h * 0.74f;
                    g.DrawLine(pen, x, y1, x + w, y1);
                    g.DrawLine(pen, x, y2, x + w, y2);
                    g.DrawLine(pen, x, y3, x + w, y3);
                    g.FillEllipse(brush, x + w * 0.24f - stroke * 1.3f, y1 - stroke * 1.3f, stroke * 2.6f, stroke * 2.6f);
                    g.FillEllipse(brush, x + w * 0.68f - stroke * 1.3f, y2 - stroke * 1.3f, stroke * 2.6f, stroke * 2.6f);
                    g.FillEllipse(brush, x + w * 0.43f - stroke * 1.3f, y3 - stroke * 1.3f, stroke * 2.6f, stroke * 2.6f);
                    return;
                }

                if (Text == "단축키")
                {
                    RectangleF keyboard = new RectangleF(x, y + h * 0.12f, w, h * 0.76f);
                    UiDrawing.DrawRoundedRectangle(g, pen, keyboard, 2.8f);
                    float key = Math.Max(1.4f, w * 0.10f);
                    float gap = w * 0.08f;
                    for (int row = 0; row < 2; row++)
                    {
                        for (int col = 0; col < 4; col++)
                        {
                            float kx = x + w * 0.15f + col * (key + gap);
                            float ky = y + h * (0.30f + row * 0.22f);
                            g.FillRectangle(brush, kx, ky, key, key);
                        }
                    }
                    g.FillRectangle(brush, x + w * 0.27f, y + h * 0.70f, w * 0.46f, Math.Max(1.4f, stroke));
                    return;
                }

                if (Text == "레이아웃")
                {
                    RectangleF outer = new RectangleF(x, y, w, h);
                    UiDrawing.DrawRoundedRectangle(g, pen, outer, 2.6f);
                    float splitX = x + w * 0.45f;
                    float splitY = y + h * 0.52f;
                    g.DrawLine(pen, splitX, y, splitX, y + h);
                    g.DrawLine(pen, splitX, splitY, x + w, splitY);
                    return;
                }

                if (Text == "모니터")
                {
                    RectangleF screen = new RectangleF(x, y, w, h * 0.68f);
                    UiDrawing.DrawRoundedRectangle(g, pen, screen, 2.7f);
                    float center = x + w / 2f;
                    float standTop = y + h * 0.68f;
                    g.DrawLine(pen, center, standTop, center, y + h * 0.86f);
                    g.DrawLine(pen, center - w * 0.22f, y + h * 0.90f, center + w * 0.22f, y + h * 0.90f);
                    return;
                }

                // 정보: 둥근 정보 마크.
                g.DrawEllipse(pen, x, y, w, h);
                float centerX = x + w / 2f;
                g.FillEllipse(brush, centerX - stroke * 0.8f, y + h * 0.22f, stroke * 1.6f, stroke * 1.6f);
                g.DrawLine(pen, centerX, y + h * 0.45f, centerX, y + h * 0.72f);
            }
        }
    }

    /// <summary>알림 영역 메뉴를 Windows 11에서는 DWM 네이티브 모서리로, 이전 버전에서는 Region으로 둥글게 만든다.</summary>
    internal sealed class RoundedContextMenuStrip : ContextMenuStrip
    {
        private const int DwmwaWindowCornerPreference = 33;
        private const int DwmwcpRoundSmall = 3;
        private bool _nativeRoundedCorners;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);

        public RoundedContextMenuStrip()
        {
            AutoSize = true;
            BackColor = UiPalette.Surface;
            ForeColor = UiPalette.Text;
            Font = new Font("Malgun Gothic", 9f, FontStyle.Regular, GraphicsUnit.Point);
            Padding = new Padding(6);
            ShowImageMargin = false;
            ShowCheckMargin = true;
            DropShadowEnabled = true;
            Renderer = new EbenMenuRenderer();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _nativeRoundedCorners = TryApplyNativeRoundedCorners();
            UpdateFallbackRegion();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateFallbackRegion();
        }

        private bool TryApplyNativeRoundedCorners()
        {
            try
            {
                int preference = DwmwcpRoundSmall;
                return DwmSetWindowAttribute(
                    Handle,
                    DwmwaWindowCornerPreference,
                    ref preference,
                    Marshal.SizeOf(typeof(int))) == 0;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }

        private void UpdateFallbackRegion()
        {
            if (!IsHandleCreated || Width <= 0 || Height <= 0) return;

            Region old = Region;
            if (_nativeRoundedCorners)
            {
                Region = null;
            }
            else
            {
                using (GraphicsPath path = UiDrawing.RoundedRectangle(
                    new RectangleF(0f, 0f, Math.Max(1f, Width - 1f), Math.Max(1f, Height - 1f)), 9f))
                {
                    Region = new Region(path);
                }
            }
            if (old != null) old.Dispose();
        }
    }

    internal sealed class EbenMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (SolidBrush brush = new SolidBrush(UiPalette.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip.Width < 4 || e.ToolStrip.Height < 4) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            RectangleF rect = new RectangleF(
                1f, 1f,
                Math.Max(1f, e.ToolStrip.Width - 3f),
                Math.Max(1f, e.ToolStrip.Height - 3f));
            using (Pen pen = new Pen(UiPalette.BorderStrong, 1f))
            {
                pen.Alignment = PenAlignment.Inset;
                UiDrawing.DrawRoundedRectangle(e.Graphics, pen, rect, 8f);
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected) return;

            Rectangle bounds = e.Item.Bounds;
            RectangleF rect = new RectangleF(
                bounds.Left + 3, bounds.Top + 2,
                Math.Max(1, bounds.Width - 6), Math.Max(1, bounds.Height - 4));
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            using (SolidBrush brush = new SolidBrush(UiPalette.Hover))
            {
                UiDrawing.FillRoundedRectangle(e.Graphics, brush, rect, 6f);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
            e.Graphics.SmoothingMode = SmoothingMode.None;
            using (Pen pen = new Pen(UiPalette.Border, 1f))
            {
                e.Graphics.DrawLine(pen, 10, y, e.ToolStrip.Width - 10, y);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = UiPalette.Text;
            base.OnRenderItemText(e);
        }
    }
}
