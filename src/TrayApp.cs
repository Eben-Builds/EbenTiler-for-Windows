using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace RumiFlowWindows
{
    /// <summary>알림 영역 아이콘으로 계속 떠 있으면서 단축키를 받아 처리한다.</summary>
    public sealed class TrayApp : ApplicationContext
    {
        private readonly Config _config;
        private readonly WindowManager _windows;
        private readonly HotkeyManager _hotkeys;
        private readonly NotifyIcon _tray;
        private readonly ToolStripMenuItem _startupItem;
        private ContextMenuStrip _menu;
        private SettingsForm _settingsForm;

        public TrayApp()
        {
            bool firstRun = !System.IO.File.Exists(Config.FilePath);
            _config = Config.Load();
            _windows = new WindowManager(_config);

            _hotkeys = new HotkeyManager();
            _hotkeys.HotkeyPressed += OnHotkeyPressed;

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ShowImageMargin = true;
            menu.ShowCheckMargin = false;

            ToolStripMenuItem settingsItem = new ToolStripMenuItem("단축키 설정...");
            settingsItem.Click += delegate { ShowSettings(); };
            menu.Items.Add(settingsItem);

            _startupItem = new ToolStripMenuItem("Windows 시작할 때 함께 실행");
            _startupItem.CheckOnClick = true;
            _startupItem.Checked = Startup.IsEnabled();
            _startupItem.Click += delegate { Startup.SetEnabled(_startupItem.Checked); };
            menu.Items.Add(_startupItem);

            menu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("종료");
            exitItem.Click += delegate { ExitApp(); };
            menu.Items.Add(exitItem);

            menu.Opening += delegate { _startupItem.Checked = Startup.IsEnabled(); };
            _menu = menu;

            _tray = new NotifyIcon();
            _tray.Icon = AppIcon.LoadSmall();
            _tray.Text = "RumiFlow for Windows";
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;
            _tray.MouseUp += OnTrayMouseUp;

            int failedCount = ApplyHotkeys(true);

            if (firstRun)
            {
                _config.Save();
            }

            if (firstRun && failedCount == 0)
            {
                _tray.BalloonTipTitle = "RumiFlow for Windows 실행 중";
                _tray.BalloonTipText =
                    "Ctrl+Alt+방향키로 창을 절반씩 배치하고, Ctrl+Alt+U/I/J/K로 사분면에 붙입니다.\n" +
                    "아이콘은 작업표시줄 오른쪽 숨김(∧) 안에 있을 수 있습니다.";
                _tray.BalloonTipIcon = ToolTipIcon.Info;
                _tray.ShowBalloonTip(7000);
            }
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

        private int ApplyHotkeys(bool notifyFailures)
        {
            List<string> failed = _hotkeys.RegisterAll(_config);
            if (!notifyFailures || failed.Count == 0) return failed.Count;

            string message = "";
            int shown = failed.Count > 5 ? 5 : failed.Count;
            for (int i = 0; i < shown; i++) message += failed[i] + "\n";
            if (failed.Count > shown) message += "외 " + (failed.Count - shown) + "개";

            _tray.BalloonTipTitle = "이미 다른 프로그램이 쓰는 단축키가 있습니다";
            _tray.BalloonTipText = "아래 단축키는 등록하지 못했습니다. 설정에서 다른 조합으로 바꿔 주세요.\n" + message;
            _tray.BalloonTipIcon = ToolTipIcon.Warning;
            _tray.ShowBalloonTip(8000);
            return failed.Count;
        }

        private void ShowSettings()
        {
            if (_settingsForm != null)
            {
                _settingsForm.Activate();
                return;
            }

            _hotkeys.UnregisterAll();
            _settingsForm = new SettingsForm(_config);
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
            _tray.Visible = false;
            _tray.Dispose();
            _hotkeys.Dispose();
            ExitThread();
        }
    }
}
