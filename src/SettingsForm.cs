using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
            return true;
        }
    }

    /// <summary>단축키와 옵션을 바꾸는 설정 창.</summary>
    public sealed class SettingsForm : Form
    {
        private readonly Config _config;
        private readonly float _scale;
        private Bitmap _headerIcon;

        private ListView _list;
        private HotkeyCaptureBox _capture;
        private CheckBox _winModifier;
        private CheckBox _cycleHalves;
        private NumericUpDown _gap;
        private PreviewPanel _preview;
        private Label _previewNote;

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
            Text = "EbenTiler for Windows - 단축키 설정";
            ShowIcon = true;
            Icon = AppIcon.LoadLarge();
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = UiPalette.Canvas;
            ForeColor = UiPalette.Text;
            Font = MakeFont(9f, FontStyle.Regular);
            ClientSize = new Size(S(800), S(620));
            KeyPreview = true;
            DoubleBuffered = true;

            Icon visibleIcon = AppIcon.LoadLarge();
            _headerIcon = visibleIcon.ToBitmap();
            visibleIcon.Dispose();

            PictureBox iconBox = new PictureBox();
            iconBox.Location = new Point(S(22), S(20));
            iconBox.Size = new Size(S(44), S(44));
            iconBox.SizeMode = PictureBoxSizeMode.Zoom;
            iconBox.Image = _headerIcon;
            iconBox.AccessibleName = "EbenTiler 앱 아이콘";
            Controls.Add(iconBox);

            Label title = MakeLabel("EbenTiler 단축키 설정", 82, 17, 450, 30, 16f, FontStyle.Bold, UiPalette.Text);
            Controls.Add(title);

            Label subtitle = MakeLabel(
                "기능을 고르고 원하는 키 조합을 지정하세요. 변경 내용은 저장할 때 적용됩니다.",
                82, 48, 540, 24, 9f, FontStyle.Regular, UiPalette.TextMuted);
            Controls.Add(subtitle);

            Label platformBadge = MakeLabel("Windows 10 · 11", 650, 25, 124, 24, 8.5f, FontStyle.Bold, UiPalette.Primary);
            platformBadge.TextAlign = ContentAlignment.MiddleCenter;
            platformBadge.BackColor = UiPalette.PrimarySoft;
            platformBadge.AccessibleName = "지원 운영체제";
            Controls.Add(platformBadge);

            Label listTitle = MakeLabel("배치 기능", 34, 108, 180, 24, 11f, FontStyle.Bold, UiPalette.Text);
            Controls.Add(listTitle);

            Label listLabel = MakeLabel(
                "기능을 선택하면 오른쪽에서 배치 결과를 미리 볼 수 있습니다.",
                34, 132, 430, 20, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            Controls.Add(listLabel);

            _list = new ListView();
            _list.Location = new Point(S(34), S(158));
            _list.Size = new Size(S(444), S(310));
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.MultiSelect = false;
            _list.HideSelection = false;
            _list.BorderStyle = BorderStyle.None;
            _list.BackColor = UiPalette.Surface;
            _list.ForeColor = UiPalette.Text;
            _list.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _list.AccessibleName = "기능 목록";
            _list.TabIndex = 0;
            _list.Columns.Add("기능", S(190));
            _list.Columns.Add("단축키", S(165));
            _list.Columns.Add("분류", S(74));
            _list.SelectedIndexChanged += OnSelectionChanged;
            Controls.Add(_list);

            Label captureLabel = MakeLabel("새 단축키", 34, 486, 76, 26, 9f, FontStyle.Bold, UiPalette.Text);
            captureLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(captureLabel);

            _capture = new HotkeyCaptureBox();
            _capture.Location = new Point(S(112), S(484));
            _capture.Size = new Size(S(244), S(28));
            _capture.AccessibleName = "새 단축키 입력";
            _capture.TabIndex = 1;
            Controls.Add(_capture);

            _winModifier = new CheckBox();
            _winModifier.Text = "Win 포함";
            _winModifier.Location = new Point(S(366), S(486));
            _winModifier.Size = new Size(S(100), S(24));
            _winModifier.FlatStyle = FlatStyle.System;
            _winModifier.ForeColor = UiPalette.Text;
            Controls.Add(_winModifier);

            Button assign = MakeButton("이 단축키로 지정", 34, 522, 146, true);
            assign.Click += OnAssign;
            Controls.Add(assign);

            Button clear = MakeButton("단축키 지우기", 190, 522, 126, false);
            clear.Click += OnClear;
            Controls.Add(clear);

            Button reset = MakeButton("전체 기본값 복원", 326, 522, 152, false);
            reset.Click += OnResetDefaults;
            Controls.Add(reset);

            Label previewTitle = MakeLabel("배치 미리보기", 526, 108, 160, 24, 11f, FontStyle.Bold, UiPalette.Text);
            Controls.Add(previewTitle);

            Label previewHelp = MakeLabel(
                "선택한 기능이 실제 화면에 어떻게 배치되는지 보여 줍니다.",
                526, 132, 242, 40, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            Controls.Add(previewHelp);

            _preview = new PreviewPanel();
            _preview.Location = new Point(S(526), S(176));
            _preview.Size = new Size(S(242), S(176));
            _preview.AccessibleName = "배치 미리보기";
            Controls.Add(_preview);

            _previewNote = MakeLabel("", 526, 362, 242, 64, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            _previewNote.AccessibleName = "배치 설명";
            Controls.Add(_previewNote);

            _cycleHalves = new CheckBox();
            _cycleHalves.Text = "같은 단축키를 연달아 누르면\r\n1/2 → 1/3 → 2/3 으로 폭 바꾸기";
            _cycleHalves.Location = new Point(S(526), S(432));
            _cycleHalves.Size = new Size(S(242), S(48));
            _cycleHalves.FlatStyle = FlatStyle.System;
            _cycleHalves.ForeColor = UiPalette.Text;
            _cycleHalves.Checked = _config.CycleHalves;
            Controls.Add(_cycleHalves);

            Label gapLabel = MakeLabel("창 사이 여백", 526, 494, 110, 24, 9f, FontStyle.Bold, UiPalette.Text);
            gapLabel.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(gapLabel);

            Label gapUnit = MakeLabel("픽셀", 718, 494, 44, 24, 8.5f, FontStyle.Regular, UiPalette.TextMuted);
            gapUnit.TextAlign = ContentAlignment.MiddleLeft;
            Controls.Add(gapUnit);

            _gap = new NumericUpDown();
            _gap.Location = new Point(S(642), S(492));
            _gap.Size = new Size(S(70), S(26));
            _gap.Minimum = 0;
            _gap.Maximum = 100;
            _gap.Value = Math.Max(0, Math.Min(100, _config.Gap));
            _gap.BorderStyle = BorderStyle.FixedSingle;
            _gap.BackColor = UiPalette.Surface;
            _gap.ForeColor = UiPalette.Text;
            _gap.AccessibleName = "창 사이 여백";
            _gap.ValueChanged += delegate { _preview.Invalidate(); };
            Controls.Add(_gap);

            Button save = MakeButton("저장", 594, 574, 90, true);
            save.Click += OnSave;
            Controls.Add(save);

            Button cancel = MakeButton("취소", 694, 574, 84, false);
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            CancelButton = cancel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _headerIcon != null)
            {
                _headerIcon.Dispose();
                _headerIcon = null;
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            DrawCard(e.Graphics, new RectangleF(S(16), S(92), S(480), S(466)), 14f * _scale);
            DrawCard(e.Graphics, new RectangleF(S(508), S(92), S(276), S(466)), 14f * _scale);

            using (Pen accent = new Pen(UiPalette.Primary, Math.Max(2f, 2f * _scale)))
            {
                e.Graphics.DrawLine(accent, S(34), S(151), S(478), S(151));
                e.Graphics.DrawLine(accent, S(526), S(169), S(768), S(169));
            }
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

        private Button MakeButton(string text, int x, int y, int width, bool primary)
        {
            RoundedButton button = new RoundedButton();
            button.Text = text;
            button.Location = new Point(S(x), S(y));
            button.Size = new Size(S(width), S(34));
            button.PrimaryStyle = primary;
            button.CornerRadius = S(8);
            button.Font = MakeFont(9f, FontStyle.Bold);
            return button;
        }

        private void FillList()
        {
            string selectedName = null;
            if (_list.SelectedItems.Count > 0)
            {
                selectedName = (string)_list.SelectedItems[0].Tag;
            }

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

                if (selectedName != null && selectedName == action.ToString())
                {
                    item.Selected = true;
                }
            }

            if (_list.SelectedItems.Count == 0 && _list.Items.Count > 0)
            {
                _list.Items[0].Selected = true;
            }
            _list.EndUpdate();
        }

        private bool TryGetSelectedAction(out SnapAction action)
        {
            action = SnapAction.LeftHalf;
            if (_list.SelectedItems.Count == 0)
            {
                return false;
            }
            return SnapActions.TryParse((string)_list.SelectedItems[0].Tag, out action);
        }

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            SnapAction action;
            if (!TryGetSelectedAction(out action))
            {
                return;
            }
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
                MessageBox.Show(this, "먼저 위 목록에서 기능을 하나 고르세요.", "EbenTiler for Windows",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Hotkey source = _capture.Captured;
            if (source.IsEmpty)
            {
                MessageBox.Show(this, "입력 상자를 누른 뒤 원하는 키 조합을 눌러 주세요.", "EbenTiler for Windows",
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
                    "EbenTiler for Windows", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<SnapAction> conflicts = new List<SnapAction>();
            foreach (KeyValuePair<SnapAction, Hotkey> pair in _config.Hotkeys)
            {
                if (pair.Key != action && pair.Value != null && pair.Value.SameAs(hotkey))
                {
                    conflicts.Add(pair.Key);
                }
            }

            if (conflicts.Count > 0)
            {
                string names = "";
                for (int i = 0; i < conflicts.Count; i++)
                {
                    if (i > 0) { names += ", "; }
                    names += SnapActions.Label(conflicts[i]);
                }

                DialogResult answer = MessageBox.Show(this,
                    hotkey.ToDisplayString() + " 는 이미 " + names + " 에 쓰이고 있습니다.\n" +
                    "그쪽 단축키를 비우고 이 기능에 지정할까요?",
                    "EbenTiler for Windows", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return;
                }

                for (int i = 0; i < conflicts.Count; i++)
                {
                    _config.Hotkeys[conflicts[i]] = new Hotkey();
                }
            }

            _config.Hotkeys[action] = hotkey;
            FillList();
        }

        private void OnClear(object sender, EventArgs e)
        {
            SnapAction action;
            if (!TryGetSelectedAction(out action))
            {
                return;
            }
            _config.Hotkeys[action] = new Hotkey();
            _capture.Captured = new Hotkey();
            _winModifier.Checked = false;
            FillList();
        }

        private void OnResetDefaults(object sender, EventArgs e)
        {
            DialogResult answer = MessageBox.Show(this,
                "모든 단축키를 처음 상태로 되돌릴까요?", "EbenTiler for Windows",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
            {
                return;
            }

            Config defaults = Config.CreateDefault();
            _config.Hotkeys = defaults.Hotkeys;
            FillList();
        }

        private void OnSave(object sender, EventArgs e)
        {
            _config.CycleHalves = _cycleHalves.Checked;
            _config.Gap = (int)_gap.Value;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
