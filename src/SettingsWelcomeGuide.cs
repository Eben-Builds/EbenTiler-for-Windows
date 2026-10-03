using System;
using System.Drawing;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>설정 > 일반에 시작 가이드 다시 보기 항목을 연결한다.</summary>
    internal static class SettingsWelcomeGuide
    {
        private const string ButtonName = "WelcomeGuideButton";

        public static void Attach(SettingsForm form)
        {
            if (form == null) return;

            // 설정을 여는 모든 경로에서 정보 > 업데이트 확인도 함께 붙인다.
            SettingsUpdateSection.Attach(form);
            if (FindByName(form, ButtonName) != null) return;

            Panel generalPage = FindGeneralPage(form);
            if (generalPage == null) return;

            float scale = form.ClientSize.Width > 0 ? form.ClientSize.Width / 960f : 1f;
            if (scale <= 0f) scale = 1f;

            MoveResetSection(generalPage, scale);

            Panel separator = new Panel();
            separator.Location = new Point(S(0, scale), S(382, scale));
            separator.Size = new Size(S(690, scale), Math.Max(1, S(1, scale)));
            separator.BackColor = UiPalette.Border;
            generalPage.Controls.Add(separator);

            Label title = MakeLabel(
                "시작 가이드", 0, 312, 150, 22, 10.5f, FontStyle.Bold, UiPalette.Text, scale);
            generalPage.Controls.Add(title);

            Label description = MakeLabel(
                "처음 실행할 때 보았던 핵심 단축키 안내를 다시 확인합니다.",
                0, 340, 500, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted, scale);
            generalPage.Controls.Add(description);

            RoundedButton button = new RoundedButton();
            button.Name = ButtonName;
            button.Text = "시작 가이드 다시 보기";
            button.Location = new Point(S(526, scale), S(332, scale));
            button.Size = new Size(S(166, scale), S(34, scale));
            button.PrimaryStyle = false;
            button.CornerRadius = S(8, scale);
            button.SurroundingBackColor = UiPalette.Surface;
            button.Font = MakeFont(9f, FontStyle.Bold, scale);
            button.AccessibleName = "시작 가이드 다시 보기";
            button.Click += delegate
            {
                using (WelcomeForm welcome = new WelcomeForm(true))
                {
                    welcome.ShowDialog(form);
                }
            };
            generalPage.Controls.Add(button);
            button.BringToFront();
        }

        private static void MoveResetSection(Panel page, float scale)
        {
            for (int i = 0; i < page.Controls.Count; i++)
            {
                Control control = page.Controls[i];
                if (control is Label && string.Equals(control.Text, "초기화", StringComparison.Ordinal))
                {
                    control.Location = new Point(S(0, scale), S(398, scale));
                }
                else if (control is Label && control.Text.StartsWith("단축키와 레이아웃 설정을", StringComparison.Ordinal))
                {
                    control.Location = new Point(S(0, scale), S(422, scale));
                }
                else if (control is Button && string.Equals(control.Text, "앱 설정 초기화", StringComparison.Ordinal))
                {
                    control.Location = new Point(S(548, scale), S(408, scale));
                }
            }
        }

        private static Panel FindGeneralPage(Control root)
        {
            for (int i = 0; i < root.Controls.Count; i++)
            {
                Control control = root.Controls[i];
                Label label = control as Label;
                if (label != null
                    && string.Equals(label.Text, "일반", StringComparison.Ordinal)
                    && label.Parent is Panel)
                {
                    return (Panel)label.Parent;
                }

                Panel nested = FindGeneralPage(control);
                if (nested != null) return nested;
            }
            return null;
        }

        private static Control FindByName(Control root, string name)
        {
            if (string.Equals(root.Name, name, StringComparison.Ordinal)) return root;
            for (int i = 0; i < root.Controls.Count; i++)
            {
                Control found = FindByName(root.Controls[i], name);
                if (found != null) return found;
            }
            return null;
        }

        private static int S(int value, float scale)
        {
            return (int)Math.Round(value * scale);
        }

        private static Font MakeFont(float size, FontStyle style, float scale)
        {
            return new Font("Malgun Gothic", size * scale, style, GraphicsUnit.Point);
        }

        private static Label MakeLabel(
            string text, int x, int y, int width, int height,
            float size, FontStyle style, Color color, float scale)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(S(x, scale), S(y, scale));
            label.Size = new Size(S(width, scale), S(height, scale));
            label.Font = MakeFont(size, style, scale);
            label.ForeColor = color;
            label.BackColor = Color.Transparent;
            label.AutoEllipsis = true;
            return label;
        }
    }
}
