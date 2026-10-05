using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>알림 영역 아이콘으로 계속 떠 있으면서 단축키를 받아 처리한다.</summary>
    public sealed class TrayApp : ApplicationContext
    {
        private readonly Config _config;
        private readonly WindowManager _windows;
        private readonly HotkeyManager _hotkeys;
        private readonly NotifyIcon _tray;
        private readonly ToolStripMenuItem _startupItem;
        private readonly ToolStripMenuItem _updateItem;
        private readonly ToolStripMenuItem _restoreLayoutItem;
        private readonly Control _uiDispatcher;
        private ContextMenuStrip _menu;
        private SettingsForm _settingsForm;
        private int _menuDpi;
        private Font _menuRegularFont;
        private Font _menuBoldFont;
        private Icon _trayIcon;
        private bool _updateNotificationPending;

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hwnd);

        public TrayApp()
        {
            _config = Config.Load();
            _windows = new WindowManager(_config);

            _uiDispatcher = new Control();
            _uiDispatcher.CreateControl();

            _hotkeys = new HotkeyManager();
            _hotkeys.HotkeyPressed += OnHotkeyPressed;
            _hotkeys.QuickLayoutHotkeyPressed += OnQuickLayoutHotkeyPressed;

            RoundedContextMenuStrip menu = new RoundedContextMenuStrip();
            menu.MinimumSize = new Size(220, 0);

            ToolStripMenuItem settingsItem = MakeMenuItem("설정...");
            settingsItem.Font = new Font(menu.Font, FontStyle.Bold);
            settingsItem.Click += delegate { ShowSettings(false); };
            menu.Items.Add(settingsItem);

            _startupItem = MakeMenuItem("Windows 시작할 때 함께 실행");
            _startupItem.CheckOnClick = true;
            _startupItem.Checked = Startup.IsEnabled();
            _startupItem.Click += delegate { Startup.SetEnabled(_startupItem.Checked); };
            menu.Items.Add(_startupItem);

            ToolStripMenuItem saveLayoutItem = MakeMenuItem("Quick Layout 저장");
            saveLayoutItem.Click += delegate { QueueQuickLayoutAction(SaveQuickLayout); };
            menu.Items.Add(saveLayoutItem);

            _restoreLayoutItem = MakeMenuItem("Quick Layout 복원");
            _restoreLayoutItem.Click += delegate { QueueQuickLayoutAction(RestoreQuickLayout); };
            menu.Items.Add(_restoreLayoutItem);

            _updateItem = MakeMenuItem("업데이트 있음");
            _updateItem.Font = new Font(menu.Font, FontStyle.Bold);
            _updateItem.ForeColor = Color.FromArgb(196, 96, 0);
            _updateItem.Visible = false;
            _updateItem.Click += delegate { ShowSettings(true); };
            menu.Items.Add(_updateItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = MakeMenuItem("종료");
            exitItem.Click += delegate { ExitApp(); };
            menu.Items.Add(exitItem);

            // WinForms의 자동 DPI 리사이징을 사용하지 않으므로 트레이 메뉴는 열릴 때마다
            // 현재 메뉴 창의 DPI를 기준으로 명시적으로 크기를 맞춘다.
            ApplyMenuScale(menu);
            menu.Opening += delegate
            {
                _startupItem.Checked = Startup.IsEnabled();
                _restoreLayoutItem.Enabled = System.IO.File.Exists(LayoutSnapshot.FilePath);
                ApplyMenuScale(menu);
            };
            menu.Opened += delegate { ApplyMenuScale(menu); };
            _menu = menu;

            _tray = new NotifyIcon();
            _trayIcon = TrayUpdateIcon.Create(false);
            _tray.Icon = _trayIcon;
            _tray.Text = "Tessdeck for Windows";
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;
            _tray.MouseUp += OnTrayMouseUp;
            _tray.BalloonTipClicked += delegate
            {
                if (!_updateNotificationPending) return;
                _updateNotificationPending = false;
                ShowSettings(true);
            };

            UpdateBadgeState.Changed += OnUpdateBadgeStateChanged;
            ApplyUpdateBadge(UpdateBadgeState.GetPendingTag());

            bool openSettingsAfterWelcome = false;
            if (_config.ShowWelcomeGuide)
            {
                using (WelcomeForm welcome = new WelcomeForm())
                {
                    DialogResult result = welcome.ShowDialog();
                    openSettingsAfterWelcome = result == DialogResult.Yes;
                    _config.ShowWelcomeGuide = !welcome.DoNotShowAgain;
                    _config.Save();
                }
            }
            else if (!System.IO.File.Exists(Config.FilePath))
            {
                _config.Save();
            }

            ApplyHotkeys(true);

            if (openSettingsAfterWelcome)
            {
                ShowSettings(false);
            }

            StartAutomaticUpdateCheck();
        }

        private static ToolStripMenuItem MakeMenuItem(string text)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.ForeColor = UiPalette.Text;
            item.Padding = new Padding(8, 5, 8, 5);
            item.AutoToolTip = false;
            return item;
        }

        private static int ScaleMenuPixel(int value, float scale)
        {
            return Math.Max(1, (int)Math.Round(value * scale));
        }

        private void ApplyMenuScale(ContextMenuStrip menu)
        {
            int dpi = GetMenuDpi(menu);
            if (dpi <= 0) dpi = 96;
            if (_menuDpi == dpi) return;

            float scale = dpi / 96f;
            Font regular = new Font("Malgun Gothic", 12f * scale, FontStyle.Regular, GraphicsUnit.Pixel);
            Font bold = new Font("Malgun Gothic", 12f * scale, FontStyle.Bold, GraphicsUnit.Pixel);
            Font oldRegular = _menuRegularFont;
            Font oldBold = _menuBoldFont;

            menu.SuspendLayout();
            try
            {
                menu.Font = regular;
                menu.Padding = new Padding(ScaleMenuPixel(6, scale));
                menu.MinimumSize = new Size(ScaleMenuPixel(220, scale), 0);

                foreach (ToolStripItem item in menu.Items)
                {
                    ToolStripMenuItem menuItem = item as ToolStripMenuItem;
                    if (menuItem != null)
                    {
                        bool isBold = menuItem.Font != null && menuItem.Font.Bold;
                        menuItem.Font = isBold ? bold : regular;
                        menuItem.Padding = new Padding(
                            ScaleMenuPixel(8, scale), ScaleMenuPixel(5, scale),
                            ScaleMenuPixel(8, scale), ScaleMenuPixel(5, scale));
                    }
                    else if (item is ToolStripSeparator)
                    {
                        item.Margin = new Padding(
                            ScaleMenuPixel(4, scale), ScaleMenuPixel(2, scale),
                            ScaleMenuPixel(4, scale), ScaleMenuPixel(2, scale));
                    }
                }
            }
            finally
            {
                menu.ResumeLayout(true);
            }

            _menuRegularFont = regular;
            _menuBoldFont = bold;
            _menuDpi = dpi;

            if (oldRegular != null) oldRegular.Dispose();
            if (oldBold != null) oldBold.Dispose();
        }

        private static int GetMenuDpi(ContextMenuStrip menu)
        {
            if (menu != null && menu.IsHandleCreated)
            {
                try
                {
                    uint dpi = GetDpiForWindow(menu.Handle);
                    if (dpi > 0) return (int)dpi;
                }
                catch (EntryPointNotFoundException)
                {
                }
            }

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                return (int)Math.Round(g.DpiX);
            }
        }

        private void StartAutomaticUpdateCheck()
        {
            if (!UpdateChecker.IsAutomaticCheckDue()) return;

            ThreadPool.QueueUserWorkItem(delegate
            {
                UpdateCheckResult result = UpdateChecker.CheckNow();

                if (result.Status == UpdateCheckStatus.UpdateAvailable)
                {
                    UpdateBadgeState.SetPending(result.TagName);

                    if (!UpdateChecker.ShouldNotify(result.TagName)) return;
                    UpdateChecker.MarkNotified(result.TagName);
                    try
                    {
                        _uiDispatcher.BeginInvoke((MethodInvoker)delegate
                        {
                            ShowUpdateNotification(result);
                        });
                    }
                    catch (InvalidOperationException) { }
                    return;
                }

                if (result.Status == UpdateCheckStatus.UpToDate || result.Status == UpdateCheckStatus.NoRelease)
                {
                    UpdateBadgeState.Clear();
                }
            });
        }

        private void OnUpdateBadgeStateChanged(string tagName)
        {
            try
            {
                if (_uiDispatcher.IsDisposed || !_uiDispatcher.IsHandleCreated) return;
                _uiDispatcher.BeginInvoke((MethodInvoker)delegate
                {
                    ApplyUpdateBadge(tagName);
                });
            }
            catch (InvalidOperationException) { }
        }

        private void ApplyUpdateBadge(string tagName)
        {
            bool hasUpdate = !string.IsNullOrWhiteSpace(tagName);
            Icon nextIcon = TrayUpdateIcon.Create(hasUpdate);
            Icon oldIcon = _trayIcon;
            _trayIcon = nextIcon;
            _tray.Icon = nextIcon;
            if (oldIcon != null) oldIcon.Dispose();

            _tray.Text = hasUpdate
                ? "Tessdeck for Windows · 업데이트 " + tagName
                : "Tessdeck for Windows";
            _updateItem.Visible = hasUpdate;
            _updateItem.Text = hasUpdate ? "업데이트 있음 · " + tagName : "업데이트 있음";
        }

        private void ShowUpdateNotification(UpdateCheckResult result)
        {
            if (result == null || result.Status != UpdateCheckStatus.UpdateAvailable) return;

            _updateNotificationPending = true;
            _tray.BalloonTipTitle = "Tessdeck 업데이트가 있습니다";
            _tray.BalloonTipText = "새 버전 " + result.TagName + "을 사용할 수 있습니다. 눌러서 업데이트 정보를 확인하세요.";
            _tray.BalloonTipIcon = ToolTipIcon.Info;
            _tray.ShowBalloonTip(10000);
        }

        private void OnTrayMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            try
            {
                MethodInfo method = typeof(NotifyIcon).GetMethod(
                    "ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
                if (method != null)
                {
                    method.Invoke(_tray, null);
                    return;
                }
            }
            catch (Exception) { }

            if (_menu != null) _menu.Show(Control.MousePosition);
        }

        private void OnHotkeyPressed(SnapAction action)
        {
            if (_settingsForm != null) return;
            _windows.Apply(action);
        }

        private void OnQuickLayoutHotkeyPressed(QuickLayoutHotkeyAction action)
        {
            if (_settingsForm != null) return;

            if (action == QuickLayoutHotkeyAction.Save)
            {
                QueueQuickLayoutAction(SaveQuickLayout);
                return;
            }

            if (action == QuickLayoutHotkeyAction.Restore)
            {
                QueueQuickLayoutAction(RestoreQuickLayout);
            }
        }

        private int ApplyHotkeys(bool notifyFailures)
        {
            List<string> failed = _hotkeys.RegisterAll(_config);
            if (!notifyFailures || failed.Count == 0) return failed.Count;

            string message = "";
            int shown = failed.Count > 5 ? 5 : failed.Count;
            for (int i = 0; i < shown; i++) message += failed[i] + "\n";
            if (failed.Count > shown) message += "외 " + (failed.Count - shown) + "개";

            _updateNotificationPending = false;
            _tray.BalloonTipTitle = "이미 다른 프로그램이 쓰는 단축키가 있습니다";
            _tray.BalloonTipText = "아래 단축키는 등록하지 못했습니다. 설정에서 다른 조합으로 바꿔 주세요.\n" + message;
            _tray.BalloonTipIcon = ToolTipIcon.Warning;
            _tray.ShowBalloonTip(8000);
            return failed.Count;
        }

        private void QueueQuickLayoutAction(MethodInvoker action)
        {
            try
            {
                _uiDispatcher.BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void SaveQuickLayout()
        {
            LayoutSnapshotSaveResult result = LayoutSnapshot.SaveCurrent();

            if (result.Saved)
            {
                ShowQuickLayoutNotification(
                    "Quick Layout 저장 완료",
                    result.WindowCount + "개 창의 현재 위치를 저장했습니다.",
                    ToolTipIcon.Info);
                return;
            }

            if (result.SkippedEmpty)
            {
                ShowQuickLayoutNotification(
                    "Quick Layout을 저장하지 않았습니다",
                    "저장할 일반 앱 창이 없어 기존 레이아웃을 그대로 유지했습니다.",
                    ToolTipIcon.Warning);
                return;
            }

            ShowQuickLayoutNotification(
                "Quick Layout 저장 실패",
                "레이아웃 파일을 저장하지 못했습니다.",
                ToolTipIcon.Error);
        }

        private void RestoreQuickLayout()
        {
            LayoutRestoreResult result = LayoutSnapshot.RestoreSaved();

            if (!result.Ready)
            {
                string message = result.FileExists
                    ? "저장된 레이아웃을 읽지 못했습니다."
                    : "아직 저장된 Quick Layout이 없습니다.";

                ShowQuickLayoutNotification(
                    "Quick Layout 복원 불가",
                    message,
                    ToolTipIcon.Warning);
                return;
            }

            int unresolved = result.Failed.Count
                + result.MissingWindows.Count
                + result.Skipped.Count;

            if (unresolved == 0)
            {
                ShowQuickLayoutNotification(
                    "Quick Layout 복원 완료",
                    result.Applied.Count + "개 창을 저장된 위치로 복원했습니다.",
                    ToolTipIcon.Info);
                return;
            }

            string details = result.Applied.Count + "개 복원";
            if (result.MissingWindows.Count > 0)
                details += ", 닫힌 창 " + result.MissingWindows.Count + "개";
            if (result.Skipped.Count > 0)
                details += ", 건너뜀 " + result.Skipped.Count + "개";
            if (result.Failed.Count > 0)
                details += ", 실패 " + result.Failed.Count + "개";

            ShowQuickLayoutNotification(
                "Quick Layout 일부 복원",
                details,
                result.Failed.Count > 0 ? ToolTipIcon.Warning : ToolTipIcon.Info);
        }

        private void ShowQuickLayoutNotification(
            string title,
            string message,
            ToolTipIcon icon)
        {
            _updateNotificationPending = false;
            _tray.BalloonTipTitle = title;
            _tray.BalloonTipText = message;
            _tray.BalloonTipIcon = icon;
            _tray.ShowBalloonTip(5000);
        }

        private void ShowSettings(bool showAboutPage)
        {
            if (_settingsForm != null)
            {
                if (showAboutPage) _settingsForm.ShowAboutPage();
                _settingsForm.Activate();
                return;
            }

            _hotkeys.UnregisterAll();
            _settingsForm = new SettingsForm(_config);
            SettingsWelcomeGuide.Attach(_settingsForm);
            SettingsUpdateSection.Attach(_settingsForm);
            if (showAboutPage) _settingsForm.ShowAboutPage();
            try
            {
                if (_settingsForm.ShowDialog() == DialogResult.OK)
                {
                    _config.CopyFrom(_settingsForm.ResultConfig);
                    _config.Save();
                }
            }
            finally
            {
                _settingsForm.Dispose();
                _settingsForm = null;
                ApplyHotkeys(true);
            }
        }

        private void ExitApp()
        {
            UpdateBadgeState.Changed -= OnUpdateBadgeStateChanged;
            _hotkeys.HotkeyPressed -= OnHotkeyPressed;
            _hotkeys.QuickLayoutHotkeyPressed -= OnQuickLayoutHotkeyPressed;
            _tray.Visible = false;
            _tray.Dispose();
            if (_trayIcon != null) _trayIcon.Dispose();
            if (_menu != null) _menu.Dispose();
            if (_menuRegularFont != null) _menuRegularFont.Dispose();
            if (_menuBoldFont != null) _menuBoldFont.Dispose();
            _uiDispatcher.Dispose();
            _hotkeys.Dispose();
            ExitThread();
        }
    }
}
