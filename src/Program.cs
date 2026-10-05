using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    internal static class Program
    {
        private const string MutexName = "Local\\EbenTiler.SingleInstance"; // Keep legacy mutex so old/new builds cannot run together.

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int processId);

        private const int AttachParentProcess = -1;

        private static readonly StringBuilder _output = new StringBuilder();
        private static string _outputPath;

        [STAThread]
        private static int Main(string[] args)
        {
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
                        "Tessdeck for Windows 는 이미 실행 중입니다.\n알림 영역(작업표시줄 오른쪽) 아이콘을 확인하세요.",
                        "Tessdeck for Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return 0;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
                return 0;
            }
        }

        private static void Emit(string line)
        {
            _output.AppendLine(line);
            try { Console.WriteLine(line); } catch (IOException) { }
        }

        private static void FlushOutput()
        {
            if (string.IsNullOrEmpty(_outputPath)) return;
            try { File.WriteAllText(_outputPath, _output.ToString(), new UTF8Encoding(false)); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        private static int RunCommandLine(string[] args)
        {
            string actionName = null;
            IntPtr hwnd = IntPtr.Zero;
            bool infoOnly = false;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--out")
                {
                    _outputPath = args[i + 1];
                    break;
                }
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if (arg == "--help" || arg == "-h" || arg == "/?") { PrintHelp(); return 0; }
                if (arg == "--list")
                {
                    SnapAction[] ordered = SnapActions.Ordered;
                    for (int k = 0; k < ordered.Length; k++) Emit(ordered[k].ToString() + "\t" + SnapActions.Label(ordered[k]));
                    return 0;
                }
                if (arg == "--info") { infoOnly = true; continue; }
                if (arg == "--window-filter-info") return PrintManageableWindows();
                if (arg == "--layout-snapshot-info") return PrintLayoutSnapshotInfo();
                if (arg == "--layout-save") return SaveLayoutSnapshot();
                if (arg == "--layout-read-info") return ReadLayoutSnapshotInfo();
                if (arg == "--layout-match-info") return PrintLayoutMatchInfo();
                if (arg == "--layout-restore-plan") return PrintLayoutRestorePlan();
                if (arg == "--layout-restore") return RestoreLayoutSnapshot();
                if (arg == "--layout-read-file" && i + 1 < args.Length)
                {
                    string snapshotPath = args[i + 1];
                    i++;
                    return ReadLayoutSnapshotInfo(snapshotPath);
                }
                if (arg == "--settings") return ShowSettingsOnly();
                if (arg == "--check") return CheckHotkeys();

                if (arg == "--startup" && i + 1 < args.Length)
                {
                    string mode = args[i + 1]; i++;
                    bool changed = true;
                    if (string.Equals(mode, "on", StringComparison.OrdinalIgnoreCase)) changed = Startup.SetEnabled(true);
                    else if (string.Equals(mode, "off", StringComparison.OrdinalIgnoreCase)) changed = Startup.SetEnabled(false);
                    else if (!string.Equals(mode, "status", StringComparison.OrdinalIgnoreCase))
                    {
                        Emit("--startup 에는 on, off, status 중 하나를 적어 주세요.");
                        return 2;
                    }

                    bool enabled = Startup.IsEnabled();
                    Emit("startup=" + (enabled ? "on" : "off"));
                    if (!changed)
                    {
                        Emit("Windows 시작 프로그램 설정을 변경하지 못했습니다.");
                        return 1;
                    }
                    return 0;
                }

                if (arg == "--out" && i + 1 < args.Length) { _outputPath = args[i + 1]; i++; continue; }
                if (arg == "--apply" && i + 1 < args.Length) { actionName = args[i + 1]; i++; continue; }

                if (arg == "--hwnd" && i + 1 < args.Length)
                {
                    string value = args[i + 1]; i++;
                    long parsed;
                    bool ok = value.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                        ? long.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed)
                        : long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed);
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
                if (hwnd == IntPtr.Zero) hwnd = WindowManager.GetTargetWindow();
                if (hwnd == IntPtr.Zero) { Emit("창을 찾지 못했습니다."); return 1; }

                System.Drawing.Rectangle workArea = Screen.FromHandle(hwnd).WorkingArea;
                System.Drawing.Rectangle visual = WindowManager.GetVisualRect(hwnd);
                Emit("work=" + workArea.Left + "," + workArea.Top + "," + workArea.Width + "," + workArea.Height);
                Emit("window=" + visual.Left + "," + visual.Top + "," + visual.Width + "," + visual.Height);
                Emit("maximized=" + (Native.IsZoomed(hwnd) ? "1" : "0"));
                return 0;
            }

            if (actionName == null) { PrintHelp(); return 2; }

            SnapAction action;
            if (!SnapActions.TryParse(actionName, out action))
            {
                Emit("모르는 명령입니다: " + actionName + "  (--list 로 목록 확인)");
                return 2;
            }

            if (hwnd == IntPtr.Zero) hwnd = WindowManager.GetTargetWindow();
            if (hwnd == IntPtr.Zero) { Emit("배치할 창을 찾지 못했습니다."); return 1; }

            Config config = Config.Load();
            WindowManager windows = new WindowManager(config);
            bool applied = windows.ApplyTo(action, hwnd);
            if (!applied) { Emit("배치하지 못했습니다: " + action.ToString()); return 1; }

            System.Drawing.Rectangle rect = WindowManager.GetVisualRect(hwnd);
            Emit("window=" + rect.Left + "," + rect.Top + "," + rect.Width + "," + rect.Height);
            return 0;
        }

        private static int PrintManageableWindows()
        {
            System.Collections.Generic.List<IntPtr> windows = WindowManager.EnumerateManageableWindows();
            Emit("count=" + windows.Count);

            for (int i = 0; i < windows.Count; i++)
            {
                IntPtr hwnd = windows[i];
                uint processId;
                Native.GetWindowThreadProcessId(hwnd, out processId);

                string processName = "?";
                if (processId != 0)
                {
                    try
                    {
                        using (System.Diagnostics.Process process =
                            System.Diagnostics.Process.GetProcessById((int)processId))
                        {
                            processName = process.ProcessName;
                        }
                    }
                    catch (ArgumentException) { }
                    catch (InvalidOperationException) { }
                    catch (System.ComponentModel.Win32Exception) { }
                }

                StringBuilder className = new StringBuilder(256);
                Native.GetClassName(hwnd, className, className.Capacity);
                System.Drawing.Rectangle rect = WindowManager.GetVisualRect(hwnd);

                Emit("window=0x" + hwnd.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " pid=" + processId
                    + " process=" + processName
                    + " class=" + className
                    + " rect=" + rect.Left + "," + rect.Top + "," + rect.Width + "," + rect.Height);
            }

            return 0;
        }

        private static int PrintLayoutSnapshotInfo()
        {
            LayoutSnapshotData snapshot = LayoutSnapshot.CaptureCurrent();
            Emit("captured=" + snapshot.CapturedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            Emit("count=" + snapshot.Windows.Count);

            for (int i = 0; i < snapshot.Windows.Count; i++)
            {
                WindowSnapshotEntry entry = snapshot.Windows[i];
                System.Drawing.Rectangle rect = entry.VisualRect;

                Emit("snapshot=" + i
                    + " process=" + entry.ProcessName
                    + " class=" + entry.WindowClass
                    + " instance=" + entry.InstanceIndex
                    + " monitor=" + entry.MonitorDeviceName
                    + " rect=" + rect.Left + "," + rect.Top + "," + rect.Width + "," + rect.Height
                    + " normalized=" + entry.NormalizedX + "," + entry.NormalizedY + ","
                        + entry.NormalizedWidth + "," + entry.NormalizedHeight
                    + " maximized=" + (entry.Maximized ? "1" : "0"));
            }

            return 0;
        }

        private static int SaveLayoutSnapshot()
        {
            LayoutSnapshotSaveResult result = LayoutSnapshot.SaveCurrent();

            Emit("saved=" + (result.Saved ? "1" : "0"));
            Emit("count=" + result.WindowCount);
            Emit("path=" + result.FilePath);

            if (result.Saved)
            {
                return 0;
            }

            if (result.SkippedEmpty)
            {
                Emit("reason=no-manageable-windows");
                Emit("기존 snapshot은 변경하지 않았습니다.");
                return 1;
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                Emit("error=" + result.Error);
            }
            return 1;
        }

        private static int ReadLayoutSnapshotInfo()
        {
            return PrintLayoutSnapshotReadResult(LayoutSnapshot.ReadSaved());
        }

        private static int ReadLayoutSnapshotInfo(string path)
        {
            return PrintLayoutSnapshotReadResult(LayoutSnapshot.ReadFromFile(path));
        }

        private static int PrintLayoutSnapshotReadResult(LayoutSnapshotReadResult result)
        {
            Emit("path=" + result.FilePath);
            Emit("exists=" + (result.FileExists ? "1" : "0"));
            Emit("loaded=" + (result.Loaded ? "1" : "0"));

            if (!result.FileExists)
            {
                Emit("reason=snapshot-missing");
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Emit("error=" + result.Error);
                }
                return 1;
            }

            if (!result.Loaded || result.Snapshot == null)
            {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Emit("error=" + result.Error);
                }
                return 1;
            }

            Emit("captured=" + result.Snapshot.CapturedAtUtc.ToString("o", CultureInfo.InvariantCulture));
            Emit("declared=" + result.DeclaredWindowCount);
            Emit("valid=" + result.Snapshot.Windows.Count);
            Emit("skipped=" + result.SkippedWindowCount);

            for (int i = 0; i < result.Snapshot.Windows.Count; i++)
            {
                WindowSnapshotEntry entry = result.Snapshot.Windows[i];
                Emit("saved=" + i
                    + " process=" + entry.ProcessName
                    + " class=" + entry.WindowClass
                    + " instance=" + entry.InstanceIndex
                    + " monitor=" + entry.MonitorDeviceName
                    + " normalized=" + entry.NormalizedX + "," + entry.NormalizedY + ","
                        + entry.NormalizedWidth + "," + entry.NormalizedHeight
                    + " maximized=" + (entry.Maximized ? "1" : "0"));
            }

            return 0;
        }

        private static int PrintLayoutMatchInfo()
        {
            LayoutSnapshotMatchResult result = LayoutSnapshot.MatchSavedToCurrent();

            Emit("path=" + result.FilePath);
            Emit("exists=" + (result.FileExists ? "1" : "0"));
            Emit("ready=" + (result.Ready ? "1" : "0"));

            if (!result.Ready)
            {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Emit("error=" + result.Error);
                }
                return 1;
            }

            Emit("saved=" + result.SavedWindowCount);
            Emit("current=" + result.CurrentWindowCount);
            Emit("matched=" + result.Matched.Count);
            Emit("missing=" + result.Missing.Count);
            Emit("current-only=" + result.CurrentOnly.Count);
            Emit("skipped-saved=" + result.SkippedSavedWindowCount);

            for (int i = 0; i < result.Matched.Count; i++)
            {
                LayoutSnapshotMatchedWindow item = result.Matched[i];
                WindowSnapshotEntry saved = item.Saved;
                WindowSnapshotEntry current = item.Current.Entry;

                Emit("match=" + i
                    + " hwnd=0x" + item.Current.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex
                    + " saved-monitor=" + saved.MonitorDeviceName
                    + " current-monitor=" + current.MonitorDeviceName);
            }

            for (int i = 0; i < result.Missing.Count; i++)
            {
                WindowSnapshotEntry item = result.Missing[i];
                Emit("missing-item=" + i
                    + " process=" + item.ProcessName
                    + " class=" + item.WindowClass
                    + " instance=" + item.InstanceIndex
                    + " saved-monitor=" + item.MonitorDeviceName);
            }

            for (int i = 0; i < result.CurrentOnly.Count; i++)
            {
                CurrentWindowSnapshot item = result.CurrentOnly[i];
                WindowSnapshotEntry entry = item.Entry;
                Emit("current-only-item=" + i
                    + " hwnd=0x" + item.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " process=" + entry.ProcessName
                    + " class=" + entry.WindowClass
                    + " instance=" + entry.InstanceIndex
                    + " current-monitor=" + entry.MonitorDeviceName);
            }

            return 0;
        }

        private static int PrintLayoutRestorePlan()
        {
            LayoutRestorePlanResult result = LayoutSnapshot.BuildRestorePlan();

            Emit("path=" + result.FilePath);
            Emit("exists=" + (result.FileExists ? "1" : "0"));
            Emit("ready=" + (result.Ready ? "1" : "0"));

            if (!result.Ready)
            {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Emit("error=" + result.Error);
                }
                return 1;
            }

            Emit("saved=" + result.SavedWindowCount);
            Emit("current=" + result.CurrentWindowCount);
            Emit("planned=" + result.Planned.Count);
            Emit("missing-window=" + result.MissingWindows.Count);
            Emit("skipped=" + result.Skipped.Count);
            Emit("current-only=" + result.CurrentOnly.Count);

            for (int i = 0; i < result.Planned.Count; i++)
            {
                LayoutRestorePlanItem item = result.Planned[i];
                WindowSnapshotEntry saved = item.Match.Saved;
                System.Drawing.Rectangle work = item.TargetWorkArea;
                System.Drawing.Rectangle target = item.TargetRect;

                Emit("plan=" + i
                    + " hwnd=0x" + item.Match.Current.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex
                    + " monitor=" + item.TargetMonitorDeviceName
                    + " work=" + work.Left + "," + work.Top + "," + work.Width + "," + work.Height
                    + " target=" + target.Left + "," + target.Top + "," + target.Width + "," + target.Height
                    + " maximized=" + (saved.Maximized ? "1" : "0"));
            }

            for (int i = 0; i < result.MissingWindows.Count; i++)
            {
                WindowSnapshotEntry item = result.MissingWindows[i];
                Emit("missing-window-item=" + i
                    + " process=" + item.ProcessName
                    + " class=" + item.WindowClass
                    + " instance=" + item.InstanceIndex
                    + " monitor=" + item.MonitorDeviceName);
            }

            for (int i = 0; i < result.Skipped.Count; i++)
            {
                LayoutRestorePlanSkippedItem item = result.Skipped[i];
                WindowSnapshotEntry saved = item.Match.Saved;
                Emit("skipped-item=" + i
                    + " reason=" + item.Reason
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex
                    + " monitor=" + saved.MonitorDeviceName);
            }

            return 0;
        }

        private static int RestoreLayoutSnapshot()
        {
            LayoutRestoreResult result = LayoutSnapshot.RestoreSaved();

            Emit("path=" + result.FilePath);
            Emit("exists=" + (result.FileExists ? "1" : "0"));
            Emit("ready=" + (result.Ready ? "1" : "0"));

            if (!result.Ready)
            {
                if (!string.IsNullOrEmpty(result.Error))
                {
                    Emit("error=" + result.Error);
                }
                return 1;
            }

            Emit("saved=" + result.SavedWindowCount);
            Emit("current=" + result.CurrentWindowCount);
            Emit("applied=" + result.Applied.Count);
            Emit("failed=" + result.Failed.Count);
            Emit("missing-window=" + result.MissingWindows.Count);
            Emit("skipped=" + result.Skipped.Count);
            Emit("current-only=" + result.CurrentOnly.Count);

            for (int i = 0; i < result.Applied.Count; i++)
            {
                LayoutRestoreAppliedItem item = result.Applied[i];
                WindowSnapshotEntry saved = item.Plan.Match.Saved;
                System.Drawing.Rectangle actual = item.ActualRect;

                Emit("applied-item=" + i
                    + " hwnd=0x" + item.Plan.Match.Current.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex
                    + " monitor=" + item.Plan.TargetMonitorDeviceName
                    + " actual=" + actual.Left + "," + actual.Top + "," + actual.Width + "," + actual.Height
                    + " maximized=" + (item.Maximized ? "1" : "0"));
            }

            for (int i = 0; i < result.Failed.Count; i++)
            {
                LayoutRestoreFailedItem item = result.Failed[i];
                WindowSnapshotEntry saved = item.Plan.Match.Saved;
                Emit("failed-item=" + i
                    + " reason=" + item.Reason
                    + " hwnd=0x" + item.Plan.Match.Current.Handle.ToInt64().ToString("X", CultureInfo.InvariantCulture)
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex);
            }

            for (int i = 0; i < result.MissingWindows.Count; i++)
            {
                WindowSnapshotEntry item = result.MissingWindows[i];
                Emit("missing-window-item=" + i
                    + " process=" + item.ProcessName
                    + " class=" + item.WindowClass
                    + " instance=" + item.InstanceIndex);
            }

            for (int i = 0; i < result.Skipped.Count; i++)
            {
                LayoutRestorePlanSkippedItem item = result.Skipped[i];
                WindowSnapshotEntry saved = item.Match.Saved;
                Emit("skipped-item=" + i
                    + " reason=" + item.Reason
                    + " process=" + saved.ProcessName
                    + " class=" + saved.WindowClass
                    + " instance=" + saved.InstanceIndex);
            }

            return result.Failed.Count == 0 ? 0 : 1;
        }

        private static int CheckHotkeys()
        {
            Config config = Config.Load();
            using (HotkeyManager manager = new HotkeyManager())
            {
                System.Collections.Generic.List<string> failed = manager.RegisterAll(config);
                SnapAction[] ordered = SnapActions.Ordered;
                int registered = 0;
                int empty = 0;
                for (int i = 0; i < ordered.Length; i++)
                {
                    Hotkey hotkey = config.Get(ordered[i]);
                    if (hotkey.IsEmpty || !hotkey.HasModifier) { empty++; continue; }
                    registered++;
                }

                Hotkey[] quickLayoutHotkeys = new Hotkey[]
                {
                    config.QuickLayoutSaveHotkey,
                    config.QuickLayoutRestoreHotkey
                };
                for (int i = 0; i < quickLayoutHotkeys.Length; i++)
                {
                    Hotkey hotkey = quickLayoutHotkeys[i];
                    if (hotkey == null || hotkey.IsEmpty || !hotkey.HasModifier)
                    {
                        empty++;
                        continue;
                    }
                    registered++;
                }

                Emit("total=" + (ordered.Length + quickLayoutHotkeys.Length));
                Emit("assigned=" + registered);
                Emit("unassigned=" + empty);
                Emit("failed=" + failed.Count);
                for (int i = 0; i < failed.Count; i++) Emit("conflict=" + failed[i]);
            }
            return 0;
        }

        private static int ShowSettingsOnly()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Config config = Config.Load();
            using (SettingsForm form = new SettingsForm(config))
            {
                SettingsWelcomeGuide.Attach(form);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    config.CopyFrom(form.ResultConfig);
                    config.Save();
                    Emit("설정을 저장했습니다: " + Config.FilePath);
                }
                else Emit("설정을 저장하지 않고 닫았습니다.");
            }
            return 0;
        }

        private static void PrintHelp()
        {
            Emit("Tessdeck for Windows");
            Emit("  인수 없이 실행하면 알림 영역에 상주하며 전역 단축키를 받는다.");
            Emit("");
            Emit("  --apply <명령>       현재 활성 창에 배치 명령을 한 번 적용");
            Emit("  --hwnd <핸들>        대상 창을 직접 지정 (10진수 또는 0x16진수)");
            Emit("  --info               대상 창의 현재 위치와 화면 작업 영역 출력");
            Emit("  --window-filter-info 관리 가능한 일반 앱 창 목록 출력 (창 제목 제외)");
            Emit("  --layout-snapshot-info 현재 창들을 메모리 snapshot으로 변환해 출력");
            Emit("  --layout-save        현재 관리 가능한 창 배치를 quick-layout.ini 에 저장");
            Emit("  --layout-read-info   저장된 quick-layout.ini 를 읽고 검증 결과 출력");
            Emit("  --layout-match-info  저장된 snapshot과 현재 열린 창의 매칭 결과 출력");
            Emit("  --layout-restore-plan 실제 이동 없이 복원 대상 모니터/좌표 계산");
            Emit("  --layout-restore     현재 열려 있고 매칭된 창을 저장 위치로 복원");
            Emit("  --layout-read-file <파일>  지정 snapshot 파일을 읽어 검증 (테스트용)");
            Emit("  --out <파일>         출력을 파일로도 저장 (스크립트에서 읽기 편하도록)");
            Emit("  --settings           설정 창만 열기");
            Emit("  --check              단축키가 다른 프로그램과 겹치는지 확인");
            Emit("  --startup on|off|status   Windows 시작 시 자동 실행 등록/해제/확인");
            Emit("  --list               쓸 수 있는 명령 목록 출력");
            Emit("  --help               이 도움말");
        }
    }
}
