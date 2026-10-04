using System;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>설정 > 정보에 업데이트 확인 UI를 가볍게 붙인다.</summary>
    internal static class SettingsUpdateSection
    {
        private const string UpdateButtonName = "TessdeckUpdateButton";
        private const string UpdateStatusName = "TessdeckUpdateStatus";

        public static void Attach(SettingsForm form)
        {
            if (form == null || form.IsDisposed) return;
            Panel aboutPage = FindAboutPage(form);
            if (aboutPage == null) return;
            if (FindByName(aboutPage, UpdateButtonName) != null) return;

            float scale = form.ClientSize.Width > 0 ? form.ClientSize.Width / 960f : 1f;
            if (scale <= 0f) scale = 1f;

            Label status = new Label();
            status.Name = UpdateStatusName;
            status.Text = "하루 한 번 새 버전만 확인하며 자동으로 설치하지 않습니다.";
            status.Location = new Point(S(350, scale), S(58, scale));
            status.Size = new Size(S(342, scale), S(24, scale));
            status.Font = new Font("Malgun Gothic", 8.2f * scale, FontStyle.Regular, GraphicsUnit.Point);
            status.ForeColor = UiPalette.TextMuted;
            status.BackColor = Color.Transparent;
            status.TextAlign = ContentAlignment.MiddleRight;
            status.AutoEllipsis = true;
            status.AccessibleName = "업데이트 상태";
            aboutPage.Controls.Add(status);

            RoundedButton check = new RoundedButton();
            check.Name = UpdateButtonName;
            check.Text = "업데이트 확인";
            check.Location = new Point(S(548, scale), S(16, scale));
            check.Size = new Size(S(144, scale), S(34, scale));
            check.Font = new Font("Malgun Gothic", 9f * scale, FontStyle.Bold, GraphicsUnit.Point);
            check.PrimaryStyle = false;
            check.CornerRadius = S(8, scale);
            check.SurroundingBackColor = UiPalette.Surface;
            check.AccessibleName = "업데이트 확인";
            aboutPage.Controls.Add(check);

            string pendingTag = UpdateBadgeState.GetPendingTag();
            if (!string.IsNullOrWhiteSpace(pendingTag))
            {
                status.Text = "새 버전 " + pendingTag + "을 사용할 수 있습니다.";
            }

            string availableReleaseUrl = null;
            check.Click += delegate
            {
                if (!string.IsNullOrWhiteSpace(availableReleaseUrl))
                {
                    OpenUrl(form, availableReleaseUrl);
                    return;
                }

                check.Enabled = false;
                check.Text = "확인 중...";
                status.Text = "GitHub의 공개 최신 릴리스를 확인하고 있습니다.";

                ThreadPool.QueueUserWorkItem(delegate
                {
                    UpdateCheckResult result = UpdateChecker.CheckNow();
                    if (result.Status == UpdateCheckStatus.UpdateAvailable)
                    {
                        UpdateBadgeState.SetPending(result.TagName);
                    }
                    else if (result.Status == UpdateCheckStatus.UpToDate || result.Status == UpdateCheckStatus.NoRelease)
                    {
                        UpdateBadgeState.Clear();
                    }

                    try
                    {
                        if (form.IsDisposed || !form.IsHandleCreated) return;
                        form.BeginInvoke((MethodInvoker)delegate
                        {
                            check.Enabled = true;
                            check.Text = "업데이트 확인";

                            if (result.Status == UpdateCheckStatus.UpdateAvailable)
                            {
                                availableReleaseUrl = result.ReleaseUrl;
                                status.Text = "새 버전 " + result.TagName + "을 사용할 수 있습니다.";
                                check.Text = "업데이트 페이지 열기";
                            }
                            else if (result.Status == UpdateCheckStatus.UpToDate)
                            {
                                status.Text = "현재 최신 버전을 사용하고 있습니다.";
                            }
                            else if (result.Status == UpdateCheckStatus.NoRelease)
                            {
                                status.Text = "아직 공개된 정식 릴리스가 없습니다.";
                            }
                            else
                            {
                                status.Text = string.IsNullOrWhiteSpace(result.ErrorMessage)
                                    ? "업데이트 정보를 확인하지 못했습니다."
                                    : result.ErrorMessage;
                            }
                        });
                    }
                    catch (InvalidOperationException) { }
                });
            };

            aboutPage.Controls.SetChildIndex(check, 0);
            aboutPage.Controls.SetChildIndex(status, 0);
        }

        /// <summary>업데이트 알림이나 트레이 메뉴에서 설정의 정보 페이지를 바로 보여 준다.</summary>
        public static void ShowAboutPage(this SettingsForm form)
        {
            if (form == null || form.IsDisposed) return;
            Attach(form);

            try
            {
                MethodInfo showPage = typeof(SettingsForm).GetMethod(
                    "ShowPage", BindingFlags.Instance | BindingFlags.NonPublic);
                if (showPage != null)
                {
                    showPage.Invoke(form, new object[] { "about" });
                }
            }
            catch (TargetInvocationException)
            {
            }
            catch (MethodAccessException)
            {
            }
        }

        private static int S(int value, float scale)
        {
            return Math.Max(1, (int)Math.Round(value * scale));
        }

        private static Panel FindAboutPage(Control root)
        {
            if (root == null) return null;

            Panel panel = root as Panel;
            if (panel != null && ContainsDirectLabel(panel, "Tessdeck for Windows")) return panel;

            foreach (Control child in root.Controls)
            {
                Panel found = FindAboutPage(child);
                if (found != null) return found;
            }
            return null;
        }

        private static bool ContainsDirectLabel(Control root, string text)
        {
            foreach (Control child in root.Controls)
            {
                Label label = child as Label;
                if (label != null && string.Equals(label.Text, text, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static Control FindByName(Control root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.Name, name, StringComparison.Ordinal)) return root;
            foreach (Control child in root.Controls)
            {
                Control found = FindByName(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void OpenUrl(IWin32Window owner, string url)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url) || !url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase)) return;
                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = url;
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception)
            {
                MessageBox.Show(owner, "업데이트 페이지를 열지 못했습니다.", "Tessdeck for Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
