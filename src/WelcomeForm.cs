using System;
using System.Drawing;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>처음 사용하는 사람을 위한 최소 안내 화면.</summary>
    public sealed class WelcomeForm : Form
    {
        private readonly float _scale;
        private readonly bool _manualPreview;
        private CheckBox _doNotShowAgain;

        public bool DoNotShowAgain
        {
            get { return !_manualPreview && _doNotShowAgain != null && _doNotShowAgain.Checked; }
        }

        public WelcomeForm()
            : this(false)
        {
        }

        public WelcomeForm(bool manualPreview)
        {
            _manualPreview = manualPreview;

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                _scale = g.DpiX / 96f;
            }

            BuildUi();
        }

        private int S(int value)
        {
            return (int)Math.Round(value * _scale);
        }

        private Font MakeFont(float size, FontStyle style)
        {
            return new Font("Malgun Gothic", size * _scale, style, GraphicsUnit.Point);
        }

        private Label MakeLabel(string text, int x, int y, int width, int height, float size, FontStyle style, Color color)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(S(x), S(y));
            label.Size = new Size(S(width), S(height));
            label.Font = MakeFont(size, style);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = true;
            return label;
        }

        private void BuildUi()
        {
            Text = "EbenTiler for Windows - 시작하기";
            ShowIcon = true;
            Icon = AppIcon.LoadLarge();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = _manualPreview ? FormStartPosition.CenterParent : FormStartPosition.CenterScreen;
            BackColor = UiPalette.Canvas;
            ForeColor = UiPalette.Text;
            Font = MakeFont(9f, FontStyle.Regular);
            ClientSize = new Size(S(650), S(454));
            KeyPreview = true;
            DoubleBuffered = true;

            PictureBox iconBox = new PictureBox();
            iconBox.Location = new Point(S(28), S(24));
            iconBox.Size = new Size(S(48), S(48));
            iconBox.SizeMode = PictureBoxSizeMode.Normal;
            Icon icon = AppIcon.LoadSized(S(48));
            iconBox.Image = icon.ToBitmap();
            icon.Dispose();
            iconBox.AccessibleName = "EbenTiler 앱 아이콘";
            Controls.Add(iconBox);

            Controls.Add(MakeLabel("EbenTiler를 바로 시작해 보세요", 92, 22, 510, 34, 17f, FontStyle.Bold, UiPalette.Text));
            Controls.Add(MakeLabel(
                "복잡한 설정 없이 단축키만 누르면 현재 창이 원하는 위치로 이동합니다.",
                92, 56, 520, 25, 9.5f, FontStyle.Regular, UiPalette.TextMuted));

            AddGuideCard(
                28, 108,
                "1", "화면 절반으로 배치",
                "Ctrl + Alt + ←   /   Ctrl + Alt + →",
                "현재 창을 왼쪽 또는 오른쪽 절반에 붙입니다.");

            AddGuideCard(
                28, 190,
                "2", "사분면으로 배치",
                "Ctrl + Alt + U / I / J / K",
                "창을 왼쪽 위·오른쪽 위·왼쪽 아래·오른쪽 아래에 붙입니다.");

            AddGuideCard(
                28, 272,
                "3", "같은 방향키를 반복",
                "1/2  →  1/3  →  2/3",
                "2초 안에 같은 절반 단축키를 다시 누르면 폭이 순서대로 바뀝니다.");

            Controls.Add(MakeLabel(
                "작업표시줄 오른쪽 EbenTiler 아이콘에서 언제든 단축키와 시작 옵션을 바꿀 수 있습니다.",
                30, 360, 590, 24, 8.8f, FontStyle.Regular, UiPalette.TextMuted));

            if (_manualPreview)
            {
                Controls.Add(MakeLabel(
                    "이 화면은 설정 > 일반에서 언제든 다시 열 수 있습니다.",
                    30, 408, 390, 28, 8.8f, FontStyle.Regular, UiPalette.TextMuted));

                RoundedButton close = MakeButton("닫기", 496, 402, 126, true);
                close.Click += delegate
                {
                    DialogResult = DialogResult.OK;
                    Close();
                };
                close.AccessibleName = "시작 가이드 닫기";
                Controls.Add(close);

                AcceptButton = close;
                CancelButton = close;
                return;
            }

            _doNotShowAgain = new CheckBox();
            _doNotShowAgain.Text = "다시 표시하지 않기";
            _doNotShowAgain.Location = new Point(S(30), S(408));
            _doNotShowAgain.Size = new Size(S(190), S(28));
            _doNotShowAgain.Font = MakeFont(8.8f, FontStyle.Regular);
            _doNotShowAgain.ForeColor = UiPalette.TextMuted;
            _doNotShowAgain.BackColor = UiPalette.Canvas;
            _doNotShowAgain.Checked = true;
            _doNotShowAgain.AccessibleName = "시작 가이드를 다시 표시하지 않기";
            Controls.Add(_doNotShowAgain);

            RoundedButton settings = MakeButton("단축키 설정", 370, 402, 116, false);
            settings.Click += delegate
            {
                DialogResult = DialogResult.Yes;
                Close();
            };
            Controls.Add(settings);

            RoundedButton start = MakeButton("바로 시작", 496, 402, 126, true);
            start.Click += delegate
            {
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(start);

            AcceptButton = start;
            CancelButton = start;
        }

        private RoundedButton MakeButton(string text, int x, int y, int width, bool primary)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.Location = new Point(S(x), S(y));
            button.Size = new Size(S(width), S(36));
            button.Font = MakeFont(9f, FontStyle.Bold);
            button.PrimaryStyle = primary;
            button.CornerRadius = S(8);
            button.SurroundingBackColor = UiPalette.Canvas;
            return button;
        }

        private void AddGuideCard(int x, int y, string number, string title, string shortcut, string description)
        {
            Panel card = new Panel();
            card.Location = new Point(S(x), S(y));
            card.Size = new Size(S(594), S(70));
            card.BackColor = UiPalette.Surface;
            card.Paint += delegate(object sender, PaintEventArgs e)
            {
                RectangleF rect = new RectangleF(0.5f, 0.5f, card.Width - 1f, card.Height - 1f);
                using (Pen pen = new Pen(UiPalette.Border, 1f))
                {
                    UiDrawing.DrawRoundedRectangle(e.Graphics, pen, rect, S(9));
                }
            };

            Label numberLabel = MakeLabel(number, 12, 14, 38, 38, 12f, FontStyle.Bold, UiPalette.PrimaryDark);
            numberLabel.TextAlign = ContentAlignment.MiddleCenter;
            numberLabel.BackColor = UiPalette.PrimarySoft;
            card.Controls.Add(numberLabel);

            card.Controls.Add(MakeLabel(title, 62, 9, 230, 24, 10f, FontStyle.Bold, UiPalette.Text));
            card.Controls.Add(MakeLabel(shortcut, 304, 9, 270, 24, 9.3f, FontStyle.Bold, UiPalette.PrimaryDark));
            card.Controls.Add(MakeLabel(description, 62, 36, 512, 23, 8.6f, FontStyle.Regular, UiPalette.TextMuted));

            Controls.Add(card);
        }
    }
}
