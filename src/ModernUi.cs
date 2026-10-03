using System;
using System.Drawing;
using System.Drawing.Drawing2D;
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

            // 이전 hover/pressed 프레임의 안티앨리어싱 픽셀이 모서리에 남지 않도록
            // 컨트롤 전체를 실제 배경색으로 먼저 지운 뒤 둥근 버튼을 새로 그린다.
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

    /// <summary>알림 영역 메뉴를 부드러운 둥근 모서리로 그린다.</summary>
    internal sealed class RoundedContextMenuStrip : ContextMenuStrip
    {
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

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            if (Width <= 0 || Height <= 0) return;

            Region old = Region;
            using (GraphicsPath path = UiDrawing.RoundedRectangle(
                new RectangleF(0f, 0f, Width, Height), 10f))
            {
                Region = new Region(path);
            }
            if (old != null) old.Dispose();
        }
    }

    internal sealed class EbenMenuRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (SolidBrush brush = new SolidBrush(UiPalette.Surface))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            RectangleF rect = new RectangleF(0.5f, 0.5f, e.ToolStrip.Width - 1.5f, e.ToolStrip.Height - 1.5f);
            using (Pen pen = new Pen(UiPalette.BorderStrong, 1f))
            {
                UiDrawing.DrawRoundedRectangle(e.Graphics, pen, rect, 10f);
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
            using (SolidBrush brush = new SolidBrush(UiPalette.Hover))
            {
                UiDrawing.FillRoundedRectangle(e.Graphics, brush, rect, 6f);
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.ContentRectangle.Top + e.Item.ContentRectangle.Height / 2;
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
