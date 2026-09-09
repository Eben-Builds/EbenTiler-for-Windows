using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
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
        private ContextMenuStrip _menu;
        private SettingsForm _settingsForm;

        public TrayApp()
        {
            bool firstRun = !System.IO.File.Exists(Config.FilePath);
            _config = Config.Load();
            _windows = new WindowManager(_config);

            _hotkeys = new HotkeyManager();
            _hotkeys.HotkeyPressed += OnHotkeyPressed;

            // 체크 표시가 그려질 왼쪽 여백을 남겨 둔다. 이걸 끄면 체크가 보이지 않는다.
            // 체크 전용 여백까지 켜면 칸이 두 겹으로 생기므로 이미지 여백 하나만 쓴다.
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

            // 메뉴를 열 때마다 실제 상태를 다시 읽어 체크를 맞춘다.
            // 설정 창이나 다른 곳에서 값이 바뀌었을 수 있기 때문이다.
            menu.Opening += delegate { _startupItem.Checked = Startup.IsEnabled(); };

            _menu = menu;

            _tray = new NotifyIcon();
            _tray.Icon = AppIcon.LoadSmall();
            _tray.Text = "Rectangle for Windows";
            _tray.ContextMenuStrip = menu;
            _tray.Visible = true;
            _tray.MouseUp += OnTrayMouseUp;

            int failedCount = ApplyHotkeys(true);

            // 처음 실행이면 설정 파일을 만들어 둔다. 사용자가 파일을 직접 열어
            // 단축키를 고칠 수 있어야 하므로, 등록 실패가 있어도 저장은 한다.
            if (firstRun)
            {
                _config.Save();
            }

            // 알림 영역 아이콘은 숨김 목록에 들어가기 쉬워서 어디 있는지 한 번 알려 준다.
            // 등록 실패 안내가 이미 떠 있으면 그쪽을 우선한다.
            if (firstRun && failedCount == 0)
            {
                _tray.BalloonTipTitle = "Rectangle for Windows 실행 중";
                _tray.BalloonTipText =
                    "Ctrl+Alt+방향키로 창을 절반씩 배치하고, Ctrl+Alt+U/I/J/K로 사분면에 붙입니다.\n" +
                    "아이콘은 작업표시줄 오른쪽 숨김(∧) 안에 있을 수 있습니다.";
                _tray.BalloonTipIcon = ToolTipIcon.Info;
                _tray.ShowBalloonTip(7000);
            }
        }

        /// <summary>
        /// 아이콘을 왼쪽으로 눌러도 메뉴가 뜨게 한다.
        /// 오른쪽 클릭은 NotifyIcon 이 알아서 처리한다.
        /// </summary>
        private void OnTrayMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            // NotifyIcon 의 내부 메뉴 표시 로직을 그대로 쓴다.
            // 이렇게 해야 메뉴 밖을 눌렀을 때 정상적으로 닫힌다.
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
            catch (Exception)
            {
            }

            if (_menu != null)
            {
                _menu.Show(Control.MousePosition);
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
    }
}
