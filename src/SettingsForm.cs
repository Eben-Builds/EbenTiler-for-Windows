using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace EbenTilerWindows
{
    /// <summary>누른 키 조합을 그대로 받아 적는 입력 상자.</summary>
    public sealed class HotkeyCaptureBox : TextBox
    {
        private Hotkey _captured = new Hotkey();

        public event EventHandler CapturedChanged;

        public HotkeyCaptureBox()
        {
            ReadOnly = true;
            BorderStyle = BorderStyle.FixedSingle;
            TextAlign = HorizontalAlignment.Center;
            Cursor = Cursors.Hand;
            BackColor = UiPalette.Surface;
            ForeColor = UiPalette.Text;
            Text = "여기를 누른 뒤 원하는 키 조합을 누르세요";
        }

        public Hotkey Captured
        {
            get { return _captured; }
            set
            {
                _captured = value != null ? value : new Hotkey();
                Text = _captured.IsEmpty ? "여기를 누른 뒤 원하는 키 조합을 누르세요" : _captured.ToDisplayString();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            Keys code = keyData & Keys.KeyCode;
            if (code == Keys.Escape)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            Hotkey hotkey = Hotkey.FromKeyData(keyData);
            if (!hotkey.IsEmpty)
            {
                _captured = hotkey;
                Text = hotkey.ToDisplayString();
                EventHandler handler = CapturedChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }
            return true;
        }
    }

    /// <summary>Tessdeck의 일반, 단축키, 레이아웃, 모니터, 정보를 관리하는 설정 창.</summary>
    public sealed class SettingsForm : Form
    {
        private readonly Config _config;
        private readonly float _scale;
        private readonly Dictionary<string, Panel> _pages = new Dictionary<string, Panel>();
        private readonly Dictionary<string, NavigationButton> _navButtons = new Dictionary<string, NavigationButton>();

        private Bitmap _headerIcon;
        private Bitmap _aboutIcon;
        private ImageList _monitorImages;
        private Panel _pageHost;
        private Panel _navHost;

        private ListView _list;
        private HotkeyCaptureBox _capture;
        private CheckBox _winModifier;
        private CheckBox _cycleHalves;
        private NumericUpDown _cycleRatio1;
        private NumericUpDown _cycleRatio2;
        private NumericUpDown _cycleRatio3;
        private NumericUpDown _gap;
        private PreviewPanel _preview;
        private Label _previewNote;
        private CheckBox _startupToggle;
        private ListView _monitorList;

        public Config ResultConfig { get { return _config; } }

        public SettingsForm(Config current)
        {
            _config = current.Clone();

            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                _scale = g.DpiX / 96f;
            }

            BuildUi();
            FillList();
            FillMonitors();
            ShowPage("hotkeys");
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
            Text = "Tessdeck for Windows - 설정";
            ShowIcon = true;
            Icon = AppIcon.LoadLarge();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiPalette.Canvas;
            ForeColor = UiPalette.Text;
            Font = MakeFont(9f, FontStyle.Regular);
            ClientSize = new Size(S(960), S(660));
            KeyPreview = true;
            DoubleBuffered = true;

            int headerIconPixels = S(48);
            Icon visibleIcon = AppIcon.LoadSized(headerIconPixels);
            _headerIcon = visibleIcon.ToBitmap();
            visibleIcon.Dispose();

            PictureBox iconBox = new PictureBox();
            iconBox.Location = new Point(S(22), S(18));
            iconBox.Size = new Size(headerIconPixels, headerIconPixels);
            iconBox.SizeMode = PictureBoxSizeMode.Normal;
            iconBox.Image = _headerIcon;
            iconBox.AccessibleName = "Tessdeck 앱 아이콘";
            Controls.Add(iconBox);

            Controls.Add(MakeLabel("Tessdeck 설정", 82, 17, 450, 30, 16f, FontStyle.Bold, UiPalette.Text));
            Controls.Add(MakeLabel(
                "창 배치 방식과 단축키, 모니터 동작을 한곳에서 관리하세요.",
                82, 48, 570, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            Label platformBadge = MakeLabel("Windows 10 · 11", 808, 25, 126, 24, 8.5f, FontStyle.Bold, UiPalette.Primary);
            platformBadge.TextAlign = ContentAlignment.MiddleCenter;
            platformBadge.BackColor = UiPalette.Canvas;
            platformBadge.AccessibleName = "지원 운영체제";
            Controls.Add(platformBadge);

            _navHost = new Panel();
            _navHost.Location = new Point(S(24), S(105));
            _navHost.Size = new Size(S(154), S(454));
            _navHost.BackColor = UiPalette.Surface;
            Controls.Add(_navHost);

            AddNavigation("general", "일반", "●", 0);
            AddNavigation("hotkeys", "단축키", "⌨", 1);
            AddNavigation("layout", "레이아웃", "▦", 2);
            AddNavigation("monitors", "모니터", "▣", 3);
            AddNavigation("about", "정보", "ⓘ", 4);

            _pageHost = new Panel();
            _pageHost.Location = new Point(S(212), S(108));
            _pageHost.Size = new Size(S(716), S(450));
            _pageHost.BackColor = UiPalette.Surface;
            Controls.Add(_pageHost);

            BuildGeneralPage();
            BuildHotkeysPage();
            BuildLayoutPage();
            BuildMonitorsPage();
            BuildAboutPage();

            Button save = MakeButton("저장", 754, 606, 90, true, false);
            save.Click += OnSave;
            Controls.Add(save);

            Button cancel = MakeButton("취소", 854, 606, 84, false, false);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);
            CancelButton = cancel;
        }

        private void AddNavigation(string key, string text, string glyph, int index)
        {
            NavigationButton button = new NavigationButton();
            button.Text = text;
            button.Glyph = glyph;
            button.Location = new Point(S(4), S(8 + index * 52));
            button.Size = new Size(S(146), S(44));
            button.Font = MakeFont(9.5f, FontStyle.Bold);
            button.AccessibleName = text + " 설정";
            button.Click += delegate { ShowPage(key); };
            _navHost.Controls.Add(button);
            _navButtons[key] = button;
        }

        private Panel CreatePage(string key)
        {
            Panel page = new Panel();
            page.Dock = DockStyle.Fill;
            page.BackColor = UiPalette.Surface;
            page.Visible = false;
            _pageHost.Controls.Add(page);
            _pages[key] = page;
            return page;
        }

        private void ShowPage(string key)
        {
            foreach (KeyValuePair<string, Panel> pair in _pages)
            {
                pair.Value.Visible = pair.Key == key;
            }
            foreach (KeyValuePair<string, NavigationButton> pair in _navButtons)
            {
                pair.Value.Selected = pair.Key == key;
            }
            if (_pages.ContainsKey(key)) _pages[key].BringToFront();
        }

        private void BuildGeneralPage()
        {
            Panel page = CreatePage("general");
            page.Controls.Add(MakeLabel("일반", 0, 0, 240, 30, 15f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "Windows 시작 동작과 기본 설정을 관리합니다.",
                0, 32, 560, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("시작", 0, 78, 120, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            _startupToggle = new CheckBox();
            _startupToggle.Text = "Windows 시작 시 Tessdeck 자동 실행";
            _startupToggle.Location = new Point(S(4), S(110));
            _startupToggle.Size = new Size(S(340), S(28));
            _startupToggle.FlatStyle = FlatStyle.System;
            _startupToggle.ForeColor = UiPalette.Text;
            _startupToggle.Checked = Startup.IsEnabled();
            _startupToggle.AccessibleName = "Windows 시작 시 자동 실행";
            page.Controls.Add(_startupToggle);
            page.Controls.Add(MakeLabel(
                "현재 사용자 계정에만 적용되며 언제든 다시 끌 수 있습니다.",
                24, 140, 520, 22, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeSeparator(0, 184, 690));
            page.Controls.Add(MakeLabel("설정 파일", 0, 208, 140, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(Config.FilePath, 0, 240, 545, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            Button openConfig = MakePageButton("설정 폴더 열기", 566, 232, 126, false);
            openConfig.Click += delegate { OpenTarget(Config.Directory); };
            page.Controls.Add(openConfig);

            page.Controls.Add(MakeSeparator(0, 292, 690));
            page.Controls.Add(MakeLabel("초기화", 0, 316, 120, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "단축키와 레이아웃 설정을 처음 상태로 되돌립니다.",
                0, 346, 470, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            Button resetAll = MakePageButton("앱 설정 초기화", 548, 336, 144, false);
            resetAll.Click += OnResetAllSettings;
            page.Controls.Add(resetAll);
        }

        private void BuildHotkeysPage()
        {
            Panel page = CreatePage("hotkeys");
            page.Controls.Add(MakeLabel("단축키", 0, 0, 220, 30, 15f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "기능을 선택하고 원하는 키 조합을 지정하세요.",
                0, 32, 520, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("배치 기능", 0, 70, 180, 24, 10.5f, FontStyle.Bold, UiPalette.Text));

            _list = new ListView();
            _list.Location = new Point(S(0), S(98));
            _list.Size = new Size(S(430), S(268));
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.MultiSelect = false;
            _list.HideSelection = false;
            _list.Scrollable = true;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.BackColor = UiPalette.Surface;
            _list.ForeColor = UiPalette.Text;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.AccessibleName = "기능 목록";
            _list.TabIndex = 0;

            int[] listColumnWidths = new int[] { S(178), S(154), S(72) };
            _list.Columns.Add("기능", listColumnWidths[0]);
            _list.Columns.Add("단축키", listColumnWidths[1]);
            _list.Columns.Add("분류", listColumnWidths[2]);
            _list.ColumnWidthChanging += delegate(object sender, ColumnWidthChangingEventArgs e)
            {
                if (e.ColumnIndex >= 0 && e.ColumnIndex < listColumnWidths.Length)
                {
                    e.NewWidth = listColumnWidths[e.ColumnIndex];
                    e.Cancel = true;
                }
            };
            _list.SelectedIndexChanged += OnSelectionChanged;
            page.Controls.Add(_list);

            Label captureLabel = MakeLabel("새 단축키", 0, 382, 82, 26, 9f, FontStyle.Bold, UiPalette.Text);
            captureLabel.TextAlign = ContentAlignment.MiddleLeft;
            page.Controls.Add(captureLabel);

            _capture = new HotkeyCaptureBox();
            _capture.Location = new Point(S(84), S(380));
            _capture.Size = new Size(S(238), S(28));
            _capture.AccessibleName = "새 단축키 입력";
            _capture.TabIndex = 1;
            page.Controls.Add(_capture);

            _winModifier = new CheckBox();
            _winModifier.Text = "Win 포함";
            _winModifier.Location = new Point(S(330), S(382));
            _winModifier.Size = new Size(S(96), S(24));
            _winModifier.FlatStyle = FlatStyle.System;
            _winModifier.ForeColor = UiPalette.Text;
            page.Controls.Add(_winModifier);

            Button assign = MakePageButton("이 단축키로 지정", 0, 416, 136, true);
            assign.Click += OnAssign;
            page.Controls.Add(assign);

            Button clear = MakePageButton("단축키 지우기", 144, 416, 122, false);
            clear.Click += OnClear;
            page.Controls.Add(clear);

            Button reset = MakePageButton("기본값 복원", 274, 416, 120, false);
            reset.Click += OnResetDefaults;
            page.Controls.Add(reset);

            page.Controls.Add(MakeLabel("배치 미리보기", 456, 70, 170, 24, 10.5f, FontStyle.Bold, UiPalette.Text));
            _preview = new PreviewPanel();
            _preview.Location = new Point(S(456), S(98));
            _preview.Size = new Size(S(244), S(176));
            _preview.AccessibleName = "배치 미리보기";
            page.Controls.Add(_preview);

            _previewNote = MakeLabel("", 456, 286, 244, 88, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            _previewNote.AccessibleName = "배치 설명";
            page.Controls.Add(_previewNote);

            page.Controls.Add(MakeLabel(
                "창 사이 여백과 반복 배치 방식은 ‘레이아웃’에서 설정할 수 있습니다.",
                456, 392, 244, 48, 8f, FontStyle.Regular, UiPalette.TextMuted));
        }

        private void BuildLayoutPage()
        {
            Panel page = CreatePage("layout");
            page.Controls.Add(MakeLabel("레이아웃", 0, 0, 240, 30, 15f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "절반, 사분면, 3분할 배치의 간격과 반복 동작을 조정합니다.",
                0, 32, 620, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("절반", 0, 70, 120, 22, 9f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel("사분면", 238, 70, 120, 22, 9f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel("3분할", 476, 70, 120, 22, 9f, FontStyle.Bold, UiPalette.Text));

            PreviewPanel half = new PreviewPanel();
            half.Location = new Point(S(0), S(96));
            half.Size = new Size(S(214), S(132));
            half.SetAction(SnapAction.LeftHalf);
            page.Controls.Add(half);

            PreviewPanel quadrant = new PreviewPanel();
            quadrant.Location = new Point(S(238), S(96));
            quadrant.Size = new Size(S(214), S(132));
            quadrant.SetAction(SnapAction.TopLeft);
            page.Controls.Add(quadrant);

            PreviewPanel thirds = new PreviewPanel();
            thirds.Location = new Point(S(476), S(96));
            thirds.Size = new Size(S(214), S(132));
            thirds.SetAction(SnapAction.FirstThird);
            page.Controls.Add(thirds);

            page.Controls.Add(MakeSeparator(0, 252, 690));
            page.Controls.Add(MakeLabel("반복 배치", 0, 274, 130, 22, 10.5f, FontStyle.Bold, UiPalette.Text));

            _cycleHalves = new CheckBox();
            _cycleHalves.Text = "같은 방향 단축키를 연달아 누르면 설정한 비율로 크기 순환";
            _cycleHalves.Location = new Point(S(4), S(306));
            _cycleHalves.Size = new Size(S(560), S(28));
            _cycleHalves.FlatStyle = FlatStyle.System;
            _cycleHalves.ForeColor = UiPalette.Text;
            _cycleHalves.Checked = _config.CycleHalves;
            _cycleHalves.CheckedChanged += delegate { UpdateCycleRatioEnabledState(); };
            page.Controls.Add(_cycleHalves);

            page.Controls.Add(MakeLabel("순환 비율", 0, 348, 120, 24, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel("누를 때 순서대로 적용", 132, 350, 166, 22, 8.2f, FontStyle.Regular, UiPalette.TextMuted));

            _cycleRatio1 = MakeRatioInput(_config.CycleRatio1, "첫 번째 순환 비율", 322, 344);
            _cycleRatio2 = MakeRatioInput(_config.CycleRatio2, "두 번째 순환 비율", 446, 344);
            _cycleRatio3 = MakeRatioInput(_config.CycleRatio3, "세 번째 순환 비율", 570, 344);
            page.Controls.Add(_cycleRatio1);
            page.Controls.Add(_cycleRatio2);
            page.Controls.Add(_cycleRatio3);
            page.Controls.Add(MakeLabel("%  →", 394, 350, 48, 22, 8.5f, FontStyle.Regular, UiPalette.TextMuted));
            page.Controls.Add(MakeLabel("%  →", 518, 350, 48, 22, 8.5f, FontStyle.Regular, UiPalette.TextMuted));
            page.Controls.Add(MakeLabel("%", 642, 350, 28, 22, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("창 사이 여백", 0, 400, 140, 24, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "배치된 창 사이에 둘 여백을 픽셀 단위로 지정합니다.",
                148, 402, 322, 22, 8.2f, FontStyle.Regular, UiPalette.TextMuted));

            _gap = new NumericUpDown();
            _gap.Location = new Point(S(548), S(396));
            _gap.Size = new Size(S(82), S(26));
            _gap.Minimum = 0;
            _gap.Maximum = 100;
            _gap.Value = Math.Max(0, Math.Min(100, _config.Gap));
            _gap.BorderStyle = BorderStyle.FixedSingle;
            _gap.BackColor = UiPalette.Surface;
            _gap.ForeColor = UiPalette.Text;
            _gap.AccessibleName = "창 사이 여백";
            page.Controls.Add(_gap);
            Label unit = MakeLabel("픽셀", 638, 398, 46, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            page.Controls.Add(unit);

            UpdateCycleRatioEnabledState();
        }

        private NumericUpDown MakeRatioInput(int value, string accessibleName, int x, int y)
        {
            NumericUpDown input = new NumericUpDown();
            input.Location = new Point(S(x), S(y));
            input.Size = new Size(S(66), S(26));
            input.Minimum = 20;
            input.Maximum = 80;
            input.Value = Math.Max(20, Math.Min(80, value));
            input.DecimalPlaces = 0;
            input.TextAlign = HorizontalAlignment.Center;
            input.BorderStyle = BorderStyle.FixedSingle;
            input.BackColor = UiPalette.Surface;
            input.ForeColor = UiPalette.Text;
            input.AccessibleName = accessibleName;
            return input;
        }

        private void UpdateCycleRatioEnabledState()
        {
            bool enabled = _cycleHalves != null && _cycleHalves.Checked;
            if (_cycleRatio1 != null) _cycleRatio1.Enabled = enabled;
            if (_cycleRatio2 != null) _cycleRatio2.Enabled = enabled;
            if (_cycleRatio3 != null) _cycleRatio3.Enabled = enabled;
        }

        private void BuildMonitorsPage()
        {
            Panel page = CreatePage("monitors");
            page.Controls.Add(MakeLabel("모니터", 0, 0, 240, 30, 15f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "현재 연결된 디스플레이와 Tessdeck의 모니터 이동 방식을 확인합니다.",
                0, 32, 520, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            Button refresh = MakePageButton("새로 고침", 548, 20, 144, false);
            refresh.Click += delegate { FillMonitors(); };
            page.Controls.Add(refresh);

            _monitorList = new ListView();
            _monitorList.Location = new Point(S(0), S(78));
            _monitorList.Size = new Size(S(690), S(210));
            _monitorList.View = View.Details;
            _monitorList.FullRowSelect = true;
            _monitorList.MultiSelect = false;
            _monitorList.BorderStyle = BorderStyle.FixedSingle;
            _monitorList.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _monitorList.BackColor = UiPalette.Surface;
            _monitorList.ForeColor = UiPalette.Text;
            _monitorImages = CreateMonitorImageList();
            _monitorList.SmallImageList = _monitorImages;
            _monitorList.Columns.Add("디스플레이", S(194));
            _monitorList.Columns.Add("번호", S(54));
            _monitorList.Columns.Add("해상도", S(120));
            _monitorList.Columns.Add("작업 영역", S(154));
            _monitorList.Columns.Add("상태", S(110));
            page.Controls.Add(_monitorList);

            PreviewPanel monitorPreview = new PreviewPanel();
            monitorPreview.Location = new Point(S(0), S(312));
            monitorPreview.Size = new Size(S(260), S(130));
            monitorPreview.SetAction(SnapAction.NextDisplay);
            page.Controls.Add(monitorPreview);

            page.Controls.Add(MakeLabel("모니터 이동 방식", 286, 316, 220, 24, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(
                "다음/이전 모니터로 이동할 때 현재 창이 화면에서 차지하던 위치와 크기 비율을 유지합니다. 서로 다른 배율의 모니터에서도 같은 위치에 자연스럽게 옮기도록 적용합니다.",
                286, 350, 396, 82, 8.8f, FontStyle.Regular, UiPalette.TextMuted));
        }

        private void BuildAboutPage()
        {
            Panel page = CreatePage("about");
            int aboutPixels = S(64);
            Icon icon = AppIcon.LoadSized(aboutPixels);
            _aboutIcon = icon.ToBitmap();
            icon.Dispose();

            PictureBox appIcon = new PictureBox();
            appIcon.Location = new Point(S(0), S(4));
            appIcon.Size = new Size(aboutPixels, aboutPixels);
            appIcon.SizeMode = PictureBoxSizeMode.Normal;
            appIcon.Image = _aboutIcon;
            page.Controls.Add(appIcon);

            page.Controls.Add(MakeLabel("Tessdeck for Windows", 82, 4, 430, 30, 15f, FontStyle.Bold, UiPalette.Text));
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            page.Controls.Add(MakeLabel("버전 " + version, 82, 38, 260, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeSeparator(0, 94, 690));
            page.Controls.Add(MakeLabel("지원 환경", 0, 118, 140, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel("Windows 10 · 11  /  .NET Framework 4.8", 0, 150, 520, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("프로그램 위치", 0, 198, 150, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(Application.ExecutablePath, 0, 228, 690, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("설정 위치", 0, 274, 150, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel(Config.FilePath, 0, 304, 690, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted));

            page.Controls.Add(MakeLabel("오픈소스", 0, 350, 140, 22, 10.5f, FontStyle.Bold, UiPalette.Text));
            page.Controls.Add(MakeLabel("MIT 라이선스", 0, 380, 180, 24, 9f, FontStyle.Regular, UiPalette.TextMuted));

            Button github = MakePageButton("GitHub 열기", 548, 370, 144, false);
            github.Click += delegate { OpenTarget("https://github.com/Eben-Builds/Tessdeck-for-Windows"); };
            page.Controls.Add(github);
        }

        private Control MakeSeparator(int x, int y, int width)
        {
            Panel line = new Panel();
            line.Location = new Point(S(x), S(y));
            line.Size = new Size(S(width), Math.Max(1, S(1)));
            line.BackColor = UiPalette.Border;
            return line;
        }

        private Button MakePageButton(string text, int x, int y, int width, bool primary)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.Location = new Point(S(x), S(y));
            button.Size = new Size(S(width), S(34));
            button.PrimaryStyle = primary;
            button.CornerRadius = S(8);
            button.SurroundingBackColor = UiPalette.Surface;
            button.Font = MakeFont(9f, FontStyle.Bold);
            return button;
        }

        private Button MakeButton(string text, int x, int y, int width, bool primary, bool onCard)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.Location = new Point(S(x), S(y));
            button.Size = new Size(S(width), S(34));
            button.PrimaryStyle = primary;
            button.CornerRadius = S(8);
            button.SurroundingBackColor = onCard ? UiPalette.Surface : UiPalette.Canvas;
            button.Font = MakeFont(9f, FontStyle.Bold);
            return button;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_headerIcon != null)
                {
                    _headerIcon.Dispose();
                    _headerIcon = null;
                }
                if (_aboutIcon != null)
                {
                    _aboutIcon.Dispose();
                    _aboutIcon = null;
                }
                if (_monitorImages != null)
                {
                    _monitorImages.Dispose();
                    _monitorImages = null;
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawCard(e.Graphics, new RectangleF(S(16), S(92), S(170), S(482)), 14f * _scale);
            DrawCard(e.Graphics, new RectangleF(S(196), S(92), S(748), S(482)), 14f * _scale);
        }

        private void DrawCard(Graphics g, RectangleF rect, float radius)
        {
            RectangleF shadow = rect;
            shadow.Offset(0f, Math.Max(1f, 2f * _scale));
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(12, 30, 73, 120)))
            {
                UiDrawing.FillRoundedRectangle(g, shadowBrush, shadow, radius);
            }
            using (SolidBrush fill = new SolidBrush(UiPalette.Surface))
            using (Pen border = new Pen(UiPalette.Border, 1f))
            {
                UiDrawing.FillRoundedRectangle(g, fill, rect, radius);
                UiDrawing.DrawRoundedRectangle(g, border, rect, radius);
            }
        }

        private void FillList()
        {
            if (_list == null) return;

            string selectedName = null;
            if (_list.SelectedItems.Count > 0) selectedName = (string)_list.SelectedItems[0].Tag;

            _list.BeginUpdate();
            _list.Items.Clear();
            SnapAction[] ordered = SnapActions.Ordered;
            for (int i = 0; i < ordered.Length; i++)
            {
                SnapAction action = ordered[i];
                Hotkey hotkey = _config.Get(action);
                ListViewItem item = new ListViewItem(SnapActions.Label(action));
                item.SubItems.Add(hotkey.IsEmpty ? "(없음)" : hotkey.ToDisplayString());
                item.SubItems.Add(SnapActions.Group(action));
                item.Tag = action.ToString();
                _list.Items.Add(item);
                if (selectedName != null && selectedName == action.ToString()) item.Selected = true;
            }
            if (_list.SelectedItems.Count == 0 && _list.Items.Count > 0) _list.Items[0].Selected = true;
            _list.EndUpdate();
        }

        private void FillMonitors()
        {
            if (_monitorList == null) return;
            _monitorList.BeginUpdate();
            _monitorList.Items.Clear();
            Screen[] screens = Screen.AllScreens;
            Array.Sort(screens, delegate(Screen a, Screen b)
            {
                if (a.Bounds.X != b.Bounds.X) return a.Bounds.X.CompareTo(b.Bounds.X);
                return a.Bounds.Y.CompareTo(b.Bounds.Y);
            });

            for (int i = 0; i < screens.Length; i++)
            {
                Screen screen = screens[i];
                ListViewItem item = new ListViewItem(FriendlyDisplayName(screen.DeviceName), 0);
                item.SubItems.Add((i + 1).ToString());
                item.SubItems.Add(screen.Bounds.Width + " × " + screen.Bounds.Height);
                item.SubItems.Add(screen.WorkingArea.Width + " × " + screen.WorkingArea.Height);
                item.SubItems.Add(screen.Primary ? "주 모니터" : "연결됨");
                _monitorList.Items.Add(item);
            }
            _monitorList.EndUpdate();
        }

        private string FriendlyDisplayName(string deviceName)
        {
            if (string.IsNullOrWhiteSpace(deviceName)) return "DISPLAY";
            const string devicePrefix = "\\\\.\\";
            string name = deviceName.StartsWith(devicePrefix, StringComparison.OrdinalIgnoreCase)
                ? deviceName.Substring(devicePrefix.Length)
                : deviceName.TrimStart('\\', '.', '/');
            return string.IsNullOrWhiteSpace(name) ? "DISPLAY" : name;
        }

        private ImageList CreateMonitorImageList()
        {
            int pixels = Math.Max(16, S(18));
            ImageList images = new ImageList();
            images.ColorDepth = ColorDepth.Depth32Bit;
            images.ImageSize = new Size(pixels, pixels);
            images.TransparentColor = Color.Transparent;
            images.Images.Add(CreateMonitorListIcon(pixels));
            return images;
        }

        private Bitmap CreateMonitorListIcon(int pixels)
        {
            Bitmap bitmap = new Bitmap(pixels, pixels, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                float stroke = Math.Max(1.25f, pixels / 13f);
                using (Pen pen = new Pen(UiPalette.PrimaryDark, stroke))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    RectangleF screen = new RectangleF(
                        stroke, stroke,
                        pixels - stroke * 2f,
                        pixels * 0.62f);
                    UiDrawing.DrawRoundedRectangle(g, pen, screen, Math.Max(2f, pixels * 0.12f));
                    float center = pixels / 2f;
                    float standTop = screen.Bottom;
                    float standBottom = pixels - stroke * 1.5f;
                    g.DrawLine(pen, center, standTop, center, standBottom - stroke * 1.8f);
                    g.DrawLine(pen, center - pixels * 0.20f, standBottom, center + pixels * 0.20f, standBottom);
                }
            }
            return bitmap;
        }

        private bool TryGetSelectedAction(out SnapAction action)
        {
            action = SnapAction.LeftHalf;
            if (_list == null || _list.SelectedItems.Count == 0) return false;
            return SnapActions.TryParse((string)_list.SelectedItems[0].Tag, out action);
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            SnapAction action;
            if (!TryGetSelectedAction(out action)) return;
            Hotkey hotkey = _config.Get(action);
            _capture.Captured = hotkey;
            _winModifier.Checked = hotkey.Win;
            _preview.SetAction(action);
            _previewNote.Text = SnapActions.Label(action) + "\r\n" + PreviewPanel.Describe(action);
        }

        private void OnAssign(object sender, EventArgs e)
        {
            SnapAction action;
            if (!TryGetSelectedAction(out action))
            {
                MessageBox.Show(this, "먼저 위 목록에서 기능을 하나 고르세요.", "Tessdeck for Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Hotkey source = _capture.Captured;
            if (source.IsEmpty)
            {
                MessageBox.Show(this, "입력 상자를 누른 뒤 원하는 키 조합을 눌러 주세요.", "Tessdeck for Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Hotkey hotkey = new Hotkey();
            hotkey.Ctrl = source.Ctrl;
            hotkey.Alt = source.Alt;
            hotkey.Shift = source.Shift;
            hotkey.Key = source.Key;
            hotkey.Win = _winModifier.Checked;

            if (!hotkey.HasModifier)
            {
                MessageBox.Show(this,
                    "Ctrl, Alt, Shift, Win 중 하나 이상을 함께 눌러야 합니다.\n" +
                    "보조키 없이 등록하면 다른 프로그램에서 그 키를 아예 쓸 수 없게 됩니다.",
                    "Tessdeck for Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<SnapAction> conflicts = new List<SnapAction>();
            foreach (KeyValuePair<SnapAction, Hotkey> pair in _config.Hotkeys)
            {
                if (pair.Key != action && pair.Value != null && pair.Value.SameAs(hotkey)) conflicts.Add(pair.Key);
            }

            if (conflicts.Count > 0)
            {
                string names = "";
                for (int i = 0; i < conflicts.Count; i++)
                {
                    if (i > 0) names += ", ";
                    names += SnapActions.Label(conflicts[i]);
                }

                DialogResult answer = MessageBox.Show(this,
                    hotkey.ToDisplayString() + " 는 이미 " + names + " 에 쓰이고 있습니다.\n" +
                    "그쪽 단축키를 비우고 이 기능에 지정할까요?",
                    "Tessdeck for Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;

                for (int i = 0; i < conflicts.Count; i++) _config.Hotkeys[conflicts[i]] = new Hotkey();
            }

            _config.Hotkeys[action] = hotkey;
            FillList();
        }

        private void OnClear(object sender, EventArgs e)
        {
            SnapAction action;
            if (!TryGetSelectedAction(out action)) return;
            _config.Hotkeys[action] = new Hotkey();
            _capture.Captured = new Hotkey();
            _winModifier.Checked = false;
            FillList();
        }

        private void OnResetDefaults(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(this,
                "모든 단축키를 처음 상태로 되돌릴까요?", "Tessdeck for Windows",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            Config defaults = Config.CreateDefault();
            _config.Hotkeys = defaults.Hotkeys;
            FillList();
        }

        private void OnResetAllSettings(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(this,
                "단축키와 레이아웃 설정을 모두 처음 상태로 되돌릴까요?",
                "Tessdeck for Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes) return;

            Config defaults = Config.CreateDefault();
            _config.Hotkeys = defaults.Hotkeys;
            _config.Gap = defaults.Gap;
            _config.CycleHalves = defaults.CycleHalves;
            _config.CycleRatio1 = defaults.CycleRatio1;
            _config.CycleRatio2 = defaults.CycleRatio2;
            _config.CycleRatio3 = defaults.CycleRatio3;
            if (_gap != null) _gap.Value = defaults.Gap;
            if (_cycleHalves != null) _cycleHalves.Checked = defaults.CycleHalves;
            if (_cycleRatio1 != null) _cycleRatio1.Value = defaults.CycleRatio1;
            if (_cycleRatio2 != null) _cycleRatio2.Value = defaults.CycleRatio2;
            if (_cycleRatio3 != null) _cycleRatio3.Value = defaults.CycleRatio3;
            UpdateCycleRatioEnabledState();
            FillList();
        }

        private void OnSave(object sender, EventArgs e)
        {
            int ratio1 = _cycleRatio1 != null ? (int)_cycleRatio1.Value : 50;
            int ratio2 = _cycleRatio2 != null ? (int)_cycleRatio2.Value : 33;
            int ratio3 = _cycleRatio3 != null ? (int)_cycleRatio3.Value : 67;

            if (_cycleRatio1 != null) _cycleRatio1.BackColor = UiPalette.Surface;
            if (_cycleRatio2 != null) _cycleRatio2.BackColor = UiPalette.Surface;
            if (_cycleRatio3 != null) _cycleRatio3.BackColor = UiPalette.Surface;

            bool duplicate12 = ratio1 == ratio2;
            bool duplicate13 = ratio1 == ratio3;
            bool duplicate23 = ratio2 == ratio3;
            if (duplicate12 || duplicate13 || duplicate23)
            {
                Color invalid = Color.FromArgb(255, 238, 238);
                if (_cycleRatio1 != null && (duplicate12 || duplicate13)) _cycleRatio1.BackColor = invalid;
                if (_cycleRatio2 != null && (duplicate12 || duplicate23)) _cycleRatio2.BackColor = invalid;
                if (_cycleRatio3 != null && (duplicate13 || duplicate23)) _cycleRatio3.BackColor = invalid;

                MessageBox.Show(this,
                    "순환 비율은 서로 다른 값으로 설정해 주세요.",
                    "Tessdeck for Windows", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (_cycleRatio1 != null && (duplicate12 || duplicate13)) _cycleRatio1.Focus();
                else if (_cycleRatio2 != null && duplicate23) _cycleRatio2.Focus();
                return;
            }

            _config.CycleHalves = _cycleHalves != null && _cycleHalves.Checked;
            _config.CycleRatio1 = ratio1;
            _config.CycleRatio2 = ratio2;
            _config.CycleRatio3 = ratio3;
            _config.Gap = _gap != null ? (int)_gap.Value : 0;

            if (_startupToggle != null && _startupToggle.Checked != Startup.IsEnabled())
            {
                if (!Startup.SetEnabled(_startupToggle.Checked))
                {
                    MessageBox.Show(this,
                        "Windows 시작 프로그램 설정을 변경하지 못했습니다. 다시 시도해 주세요.",
                        "Tessdeck for Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void OpenTarget(string target)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(target)) return;
                if (!target.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                    && !Directory.Exists(target)
                    && !File.Exists(target))
                {
                    return;
                }

                ProcessStartInfo info = new ProcessStartInfo();
                info.FileName = target;
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception)
            {
                MessageBox.Show(this, "해당 위치를 열지 못했습니다.", "Tessdeck for Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
