using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace RectangleWindows
{
    internal static class Program
    {
        private const string MutexName = "Global\\RectangleWindows.SingleInstance";

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        private const int AttachParentProcess = -1;

        private static readonly StringBuilder _output = new StringBuilder();
        private static string _outputPath;

        [STAThread]
        private static int Main(string[] args)
        {
            // 창 좌표를 실제 픽셀로 다루려면 DPI 인식을 가장 먼저 켜야 한다.
            Native.EnableDpiAwareness();

            if (args != null && args.Length > 0)
            {
                AttachConsole(AttachParentProcess);
                int code = RunCommandLine(args);
                FlushOutput();
                return code;
            }

            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "Rectangle for Windows 는 이미 실행 중입니다.\n알림 영역(작업표시줄 오른쪽) 아이콘을 확인하세요.",
                        "Rectangle for Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
                return 0;
            }
        }

        /// <summary>
        /// 화면 표시용 출력. 창 프로그램이라 콘솔로 파이프 연결이 안 되는 경우가 있어,
        /// --out 으로 파일에도 남길 수 있게 해 두었다.
        /// </summary>
        private static void Emit(string line)
        {
            _output.AppendLine(line);
            try
            {
                Console.WriteLine(line);
            }
            catch (IOException)
            {
            }
        }

        private static void FlushOutput()
        {
            if (string.IsNullOrEmpty(_outputPath))
            {
                return;
            }
            try
            {
                File.WriteAllText(_outputPath, _output.ToString(), new UTF8Encoding(false));
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        /// <summary>명령줄로 배치 명령 하나만 실행하는 모드. 스크립트와 자동 검증에 쓴다.</summary>
        private static int RunCommandLine(string[] args)
        {
            string actionName = null;
            IntPtr hwnd = IntPtr.Zero;
            bool infoOnly = false;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "--help" || arg == "-h" || arg == "/?")
                {
                    PrintHelp();
                    return 0;
                }

                if (arg == "--list")
                {
                    SnapAction[] ordered = SnapActions.Ordered;
                    for (int k = 0; k < ordered.Length; k++)
                    {
                        Emit(ordered[k].ToString() + "\t" + SnapActions.Label(ordered[k]));
                    }
                    return 0;
                }

                if (arg == "--info")
                {
                    infoOnly = true;
                    continue;
                }

                if (arg == "--settings")
                {
                    return ShowSettingsOnly();
                }

                if (arg == "--out" && i + 1 < args.Length)
                {
                    _outputPath = args[i + 1];
                    i++;
                    continue;
                }

                if (arg == "--apply" && i + 1 < args.Length)
                {
                    actionName = args[i + 1];
                    i++;
                    continue;
                }

                if (arg == "--hwnd" && i + 1 < args.Length)
                {
                    string value = args[i + 1];
                    i++;

                    long parsed;
                    bool ok;
                    if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        ok = long.TryParse(value.Substring(2), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture, out parsed);
                    }
                    else
                    {
                        ok = long.TryParse(value, NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out parsed);
                    }

                    if (!ok)
                    {
                        Emit("창 핸들 값을 읽지 못했습니다: " + value);
                        return 2;
                    }
                    hwnd = new IntPtr(parsed);
                    continue;
                }
            }

            if (infoOnly)
            {
                if (hwnd == IntPtr.Zero)
                {
                    hwnd = WindowManager.GetTargetWindow();
                }
                if (hwnd == IntPtr.Zero)
                {
                    Emit("창을 찾지 못했습니다.");
                    return 1;
                }

                System.Drawing.Rectangle workArea = Screen.FromHandle(hwnd).WorkingArea;
                System.Drawing.Rectangle visual = WindowManager.GetVisualRect(hwnd);
                Emit("work=" + workArea.Left + "," + workArea.Top + "," +
                    workArea.Width + "," + workArea.Height);
                Emit("window=" + visual.Left + "," + visual.Top + "," +
                    visual.Width + "," + visual.Height);
                Emit("maximized=" + (Native.IsZoomed(hwnd) ? "1" : "0"));
                return 0;
            }

            if (actionName == null)
            {
                PrintHelp();
                return 2;
            }

            SnapAction action;
            if (!SnapActions.TryParse(actionName, out action))
            {
                Emit("모르는 명령입니다: " + actionName + "  (--list 로 목록 확인)");
                return 2;
            }

            if (hwnd == IntPtr.Zero)
            {
                hwnd = WindowManager.GetTargetWindow();
            }

            if (hwnd == IntPtr.Zero)
            {
                Emit("배치할 창을 찾지 못했습니다.");
                return 1;
            }

            Config config = Config.Load();
            WindowManager windows = new WindowManager(config);
            bool applied = windows.ApplyTo(action, hwnd);

            if (!applied)
            {
                Emit("배치하지 못했습니다: " + action.ToString());
                return 1;
            }

            System.Drawing.Rectangle rect = WindowManager.GetVisualRect(hwnd);
            Emit("window=" + rect.Left + "," + rect.Top + "," + rect.Width + "," + rect.Height);
            return 0;
        }

        /// <summary>상주 중인 본체 없이 설정 창만 연다. 저장하면 설정 파일에 바로 반영된다.</summary>
        private static int ShowSettingsOnly()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Config config = Config.Load();
            using (SettingsForm form = new SettingsForm(config))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    config.CopyFrom(form.ResultConfig);
                    config.Save();
                    Emit("설정을 저장했습니다: " + Config.FilePath);
                }
                else
                {
                    Emit("설정을 저장하지 않고 닫았습니다.");
                }
            }
            return 0;
        }

        private static void PrintHelp()
        {
            Emit("Rectangle for Windows");
            Emit("  인수 없이 실행하면 알림 영역에 상주하며 전역 단축키를 받는다.");
            Emit("");
            Emit("  --apply <명령>       현재 활성 창에 배치 명령을 한 번 적용");
            Emit("  --hwnd <핸들>        대상 창을 직접 지정 (10진수 또는 0x16진수)");
            Emit("  --info               대상 창의 현재 위치와 화면 작업 영역 출력");
            Emit("  --out <파일>         출력을 파일로도 저장 (스크립트에서 읽기 편하도록)");
            Emit("  --settings           설정 창만 열기");
            Emit("  --list               쓸 수 있는 명령 목록 출력");
            Emit("  --help               이 도움말");
        }
    }
}
