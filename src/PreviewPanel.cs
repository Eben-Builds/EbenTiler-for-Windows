using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RectangleWindows
{
    /// <summary>
    /// 고른 기능이 창을 화면 어디에 놓는지 그림으로 보여 준다.
    /// 설명을 읽지 않아도 어떤 배치인지 한눈에 알 수 있게 하려는 것이다.
    /// </summary>
    public sealed class PreviewPanel : Control
    {
        private static readonly Color ScreenFill   = Color.FromArgb(255, 244, 246, 248);
        private static readonly Color ScreenEdge   = Color.FromArgb(255, 197, 203, 211);
        private static readonly Color TaskbarFill  = Color.FromArgb(255, 222, 227, 233);
        private static readonly Color WindowFill   = Color.FromArgb(255,  37,  99, 235);
        private static readonly Color WindowEdge   = Color.FromArgb(255,  29,  78, 216);
        private static readonly Color GhostEdge    = Color.FromArgb(255, 148, 163, 184);
        private static readonly Color ArrowColor   = Color.FromArgb(255,  30,  41,  59);

        private SnapAction _action = SnapAction.LeftHalf;

        public PreviewPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint
                | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = Color.White;
        }

        public void SetAction(SnapAction action)
        {
            _action = action;
            Invalidate();
        }

        /// <summary>이 기능이 어떤 배치인지 한 줄로 설명한다.</summary>
        public static string Describe(SnapAction action)
        {
            switch (action)
            {
                case SnapAction.LeftHalf:
                case SnapAction.RightHalf:
                case SnapAction.TopHalf:
                case SnapAction.BottomHalf:
                    return "이어서 다시 누르면 1/2 → 1/3 → 2/3 으로 폭이 바뀝니다.";
                case SnapAction.MaximizeHeight:
                    return "가로 폭은 그대로 두고 위아래로만 꽉 채웁니다.";
                case SnapAction.Center:
                    return "크기는 그대로 두고 화면 한가운데로 옮깁니다.";
                case SnapAction.Larger:
                    return "가운데를 기준으로 한 단계 크게 만듭니다.";
                case SnapAction.Smaller:
                    return "가운데를 기준으로 한 단계 작게 만듭니다.";
                case SnapAction.Restore:
                    return "배치하기 전에 있던 자리와 크기로 되돌립니다.";
                case SnapAction.NextDisplay:
                    return "차지하던 비율을 유지한 채 다음 모니터로 옮깁니다.";
                case SnapAction.PreviousDisplay:
                    return "차지하던 비율을 유지한 채 이전 모니터로 옮깁니다.";
                case SnapAction.Maximize:
                    return "작업표시줄을 뺀 화면 전체를 채웁니다.";
                default:
                    return "화면을 나눠 그 자리에 딱 맞춰 넣습니다.";
            }
        }

        /// <summary>화면을 1x1 로 봤을 때 창이 놓이는 자리.</summary>
        private static RectangleF TargetFraction(SnapAction action)
        {
            float third = 1f / 3f;
            switch (action)
            {
                case SnapAction.LeftHalf:       return new RectangleF(0f, 0f, 0.5f, 1f);
                case SnapAction.RightHalf:      return new RectangleF(0.5f, 0f, 0.5f, 1f);
                case SnapAction.TopHalf:        return new RectangleF(0f, 0f, 1f, 0.5f);
                case SnapAction.BottomHalf:     return new RectangleF(0f, 0.5f, 1f, 0.5f);

                case SnapAction.TopLeft:        return new RectangleF(0f, 0f, 0.5f, 0.5f);
                case SnapAction.TopRight:       return new RectangleF(0.5f, 0f, 0.5f, 0.5f);
                case SnapAction.BottomLeft:     return new RectangleF(0f, 0.5f, 0.5f, 0.5f);
                case SnapAction.BottomRight:    return new RectangleF(0.5f, 0.5f, 0.5f, 0.5f);

                case SnapAction.FirstThird:     return new RectangleF(0f, 0f, third, 1f);
                case SnapAction.CenterThird:    return new RectangleF(third, 0f, third, 1f);
                case SnapAction.LastThird:      return new RectangleF(third * 2f, 0f, third, 1f);
                case SnapAction.FirstTwoThirds: return new RectangleF(0f, 0f, third * 2f, 1f);
                case SnapAction.LastTwoThirds:  return new RectangleF(third, 0f, third * 2f, 1f);

                case SnapAction.Maximize:       return new RectangleF(0f, 0f, 1f, 1f);
                case SnapAction.MaximizeHeight: return new RectangleF(0.30f, 0f, 0.40f, 1f);
                case SnapAction.Center:         return new RectangleF(0.26f, 0.24f, 0.48f, 0.52f);
                case SnapAction.Larger:         return new RectangleF(0.12f, 0.13f, 0.76f, 0.74f);
                case SnapAction.Smaller:        return new RectangleF(0.30f, 0.29f, 0.40f, 0.42f);
                case SnapAction.Restore:        return new RectangleF(0.30f, 0.27f, 0.40f, 0.46f);
                default:                        return new RectangleF(0.25f, 0.25f, 0.5f, 0.5f);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_action == SnapAction.NextDisplay || _action == SnapAction.PreviousDisplay)
            {
                PaintTwoScreens(g, _action == SnapAction.NextDisplay);
                return;
            }

            Rectangle screen = ScreenBox(ClientRectangle, 1);
            DrawScreen(g, screen);

            RectangleF f = TargetFraction(_action);
            Rectangle work = WorkArea(screen);
            Rectangle target = Scale(work, f);

            if (_action == SnapAction.Restore)
            {
                // 되돌리기 전 자리(점선)와 되돌아갈 자리(칠한 사각형)를 함께 보여 준다.
                // 점선이 칠한 사각형에 가리지 않도록 나중에 그린다.
                Rectangle from = Scale(work, new RectangleF(0f, 0f, 0.5f, 1f));
                DrawWindow(g, target);
                DrawGhost(g, from);
                DrawArrow(g, ArrowColor,
                    new Point(from.Right - 6, from.Top + from.Height / 2),
                    new Point(target.Left + 4, target.Top + target.Height / 2));
                return;
            }

            if (_action == SnapAction.Larger || _action == SnapAction.Smaller)
            {
                bool grow = _action == SnapAction.Larger;
                RectangleF baseF = grow
                    ? new RectangleF(0.26f, 0.27f, 0.48f, 0.46f)
                    : new RectangleF(0.16f, 0.17f, 0.68f, 0.66f);
                Rectangle baseRect = Scale(work, baseF);

                DrawWindow(g, target);
                DrawGhost(g, baseRect);

                // 화살표가 파란 창 위에 놓이면 흰색으로, 바깥 배경 위면 짙은 색으로 그린다.
                Color arrow = grow ? Color.White : ArrowColor;
                DrawResizeArrows(g, baseRect, target, grow, arrow);
                return;
            }

            DrawWindow(g, target);
        }

        private void PaintTwoScreens(Graphics g, bool forward)
        {
            Rectangle full = ClientRectangle;
            int gap = Math.Max(6, full.Width / 22);
            int halfW = (full.Width - gap) / 2;

            Rectangle leftBox = ScreenBox(new Rectangle(full.Left, full.Top, halfW, full.Height), 2);
            Rectangle rightBox = ScreenBox(new Rectangle(full.Left + halfW + gap, full.Top, halfW, full.Height), 2);

            DrawScreen(g, leftBox);
            DrawScreen(g, rightBox);

            RectangleF f = new RectangleF(0f, 0f, 0.5f, 1f);
            Rectangle fromWork = WorkArea(forward ? leftBox : rightBox);
            Rectangle toWork = WorkArea(forward ? rightBox : leftBox);

            Rectangle fromRect = Scale(fromWork, f);
            Rectangle toRect = Scale(toWork, f);

            DrawWindow(g, toRect);
            DrawGhost(g, fromRect);

            int midY = full.Top + full.Height / 2;
            if (forward)
            {
                DrawArrow(g, ArrowColor, new Point(leftBox.Right + 2, midY), new Point(rightBox.Left - 2, midY));
            }
            else
            {
                DrawArrow(g, ArrowColor, new Point(rightBox.Left - 2, midY), new Point(leftBox.Right + 2, midY));
            }
        }

        /// <summary>화면(모니터) 그림이 들어갈 자리를 16:10 비율로 잡는다.</summary>
        private static Rectangle ScreenBox(Rectangle area, int inset)
        {
            int pad = 8 + inset;
            int w = area.Width - pad * 2;
            int h = area.Height - pad * 2;
            if (w < 10 || h < 10) { return new Rectangle(area.Left, area.Top, Math.Max(10, w), Math.Max(10, h)); }

            int boxW = w;
            int boxH = (int)(w * 0.62f);
            if (boxH > h)
            {
                boxH = h;
                boxW = (int)(h / 0.62f);
            }
            int x = area.Left + (area.Width - boxW) / 2;
            int y = area.Top + (area.Height - boxH) / 2;
            return new Rectangle(x, y, boxW, boxH);
        }

        /// <summary>작업표시줄을 뺀 영역. 실제 동작과 같게 아래쪽을 비워 둔다.</summary>
        private static Rectangle WorkArea(Rectangle screen)
        {
            int taskbar = Math.Max(4, (int)(screen.Height * 0.10f));
            return new Rectangle(screen.Left + 1, screen.Top + 1, screen.Width - 2, screen.Height - taskbar - 1);
        }

        private static Rectangle Scale(Rectangle area, RectangleF f)
        {
            int x = area.Left + (int)Math.Round(area.Width * f.X);
            int y = area.Top + (int)Math.Round(area.Height * f.Y);
            int w = (int)Math.Round(area.Width * f.Width);
            int h = (int)Math.Round(area.Height * f.Height);
            if (w < 3) { w = 3; }
            if (h < 3) { h = 3; }
            return new Rectangle(x, y, w, h);
        }

        private static void DrawScreen(Graphics g, Rectangle screen)
        {
            using (SolidBrush fill = new SolidBrush(ScreenFill))
            {
                g.FillRectangle(fill, screen);
            }
            int taskbar = Math.Max(4, (int)(screen.Height * 0.10f));
            using (SolidBrush bar = new SolidBrush(TaskbarFill))
            {
                g.FillRectangle(bar, screen.Left, screen.Bottom - taskbar, screen.Width, taskbar);
            }
            using (Pen edge = new Pen(ScreenEdge, 1f))
            {
                g.DrawRectangle(edge, screen.Left, screen.Top, screen.Width - 1, screen.Height - 1);
            }
        }

        private static void DrawWindow(Graphics g, Rectangle rect)
        {
            using (SolidBrush fill = new SolidBrush(WindowFill))
            {
                g.FillRectangle(fill, rect);
            }
            using (Pen edge = new Pen(WindowEdge, 1f))
            {
                g.DrawRectangle(edge, rect.Left, rect.Top, rect.Width - 1, rect.Height - 1);
            }
        }

        /// <summary>바뀌기 전 자리를 점선으로 표시한다.</summary>
        private static void DrawGhost(Graphics g, Rectangle rect)
        {
            using (Pen ghost = new Pen(GhostEdge, 1.4f))
            {
                ghost.DashStyle = DashStyle.Dash;
                g.DrawRectangle(ghost, rect);
            }
        }

        private static void DrawArrow(Graphics g, Color color, Point from, Point to)
        {
            using (Pen pen = new Pen(color, 1.8f))
            {
                pen.EndCap = LineCap.ArrowAnchor;
                g.DrawLine(pen, from, to);
            }
        }

        private static void DrawResizeArrows(Graphics g, Rectangle baseRect, Rectangle target, bool grow, Color color)
        {
            int cy = target.Top + target.Height / 2;
            if (grow)
            {
                DrawArrow(g, color, new Point(baseRect.Left, cy), new Point(target.Left + 4, cy));
                DrawArrow(g, color, new Point(baseRect.Right, cy), new Point(target.Right - 4, cy));
            }
            else
            {
                DrawArrow(g, color, new Point(baseRect.Left, cy), new Point(target.Left - 4, cy));
                DrawArrow(g, color, new Point(baseRect.Right, cy), new Point(target.Right + 4, cy));
            }
        }
    }
}
