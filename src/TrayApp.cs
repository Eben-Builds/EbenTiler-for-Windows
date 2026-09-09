using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RectangleWindows
{
    /// <summary>알림 영역 아이콘으로 계속 떠 있으면서 단축키를 받아 처리한다.</summary>
    public sealed class TrayApp : ApplicationContext
    {
        private readonly Config _config;
        private readonly WindowManager _windows;
        private readonly HotkeyManager _hotkeys;
        private readonly NotifyIcon _tray;
        private readonly ToolStripMenuItem _startupItem;
        private SettingsForm _settingsForm;

        public TrayApp()
        {
            bool firstRun = !System.IO.File.Exists(Config.FilePath);
            _config = Config.Load();
            _windows = new WindowManager(_config);

            _hotkeys = new HotkeyManager();
            _hotkeys.HotkeyPressed += OnHotkeyPressed;

            ContextMenuStrip menu = new ContextMenuStrip();
            menu.ShowImageMargin = false;

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

            _tray = new NotifyIcon();
            _tray.Icon = LoadIcon();
            _tray.Text = "Rectangle for Windows";
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { ShowSettings(); };

            int failedCount = ApplyHotkeys(true);

            // 처음 실행이면 어디에 있는지 알려 준다. 알림 영역 아이콘은 숨김 목록에 들어가기 쉽다.
            // 등록 실패 안내가 이미 떠 있으면 그쪽을 우선한다.
            if (firstRun && failedCount == 0)
            {
                _config.Save();
                _tray.BalloonTipTitle = "Rectangle for Windows 실행 중";
                _tray.BalloonTipText =
                    "Ctrl+Alt+방향키로 창을 절반씩 배치하고, Ctrl+Alt+U/I/J/K로 사분면에 붙입니다.\n" +
                    "아이콘은 작업표시줄 오른쪽 숨김(∧) 안에 있을 수 있습니다.";
                _tray.BalloonTipIcon = ToolTipIcon.Info;
                _tray.ShowBalloonTip(7000);
            }
        }

        private void OnHotkeyPressed(SnapAction action)
        {
            // 설정 창이 열려 있을 때는 배치 명령을 처리하지 않는다.
            if (_settingsForm != null)
            {
                return;
            }
            _windows.Apply(action);
        }

        /// <summary>설정에 있는 단축키를 등록하고, 실패한 것이 있으면 알려 준다.</summary>
        private int ApplyHotkeys(bool notifyFailures)
        {
            List<string> failed = _hotkeys.RegisterAll(_config);
            if (!notifyFailures || failed.Count == 0)
            {
                return failed.Count;
            }

            string message = "";
            int shown = failed.Count > 5 ? 5 : failed.Count;
            for (int i = 0; i < shown; i++)
            {
                message += failed[i] + "\n";
            }
            if (failed.Count > shown)
            {
                message += "외 " + (failed.Count - shown) + "개";
            }

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

            // 설정 창에서 키 조합을 눌러 볼 수 있도록 전역 단축키를 잠시 해제한다.
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

        /// <summary>실행 파일에 박힌 아이콘을 쓰고, 없으면 직접 그린다.</summary>
        private static Icon LoadIcon()
        {
            try
            {
                Icon icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (icon != null)
                {
                    return icon;
                }
            }
            catch (Exception)
            {
            }

            Bitmap bitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.None;
                g.Clear(Color.Transparent);
                using (SolidBrush fill = new SolidBrush(Color.FromArgb(230, 240, 240, 240)))
                {
                    g.FillRectangle(fill, 3, 5, 26, 22);
                }
                using (Pen pen = new Pen(Color.FromArgb(255, 40, 40, 40), 2f))
                {
                    g.DrawRectangle(pen, 3, 5, 26, 22);
                    g.DrawLine(pen, 16, 5, 16, 27);
                }
                using (SolidBrush accent = new SolidBrush(Color.FromArgb(255, 40, 40, 40)))
                {
                    g.FillRectangle(accent, 4, 6, 12, 20);
                }
            }
            return Icon.FromHandle(bitmap.GetHicon());
        }
    }
}
