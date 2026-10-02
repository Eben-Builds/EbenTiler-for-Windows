using System;
using System.Collections.Generic;
using System.Drawing;
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

        private void BuildUi()
        {
            Text = "EbenTiler for Windows - 단축키 설정";
            ShowIcon = true;
            Icon = AppIcon.LoadLarge();
            // FixedDialog 로 두면 제목 표시줄에 아이콘이 나오지 않는다.
            // 크기 조절은 막으면서 아이콘은 보이는 FixedSingle 을 쓴다.
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = new Font("Malgun Gothic", 9f * _scale, GraphicsUnit.Point);
            ClientSize = new Size(S(660), S(560));
            KeyPreview = true;

            Label listLabel = new Label();
            listLabel.Text = "기능을 고른 뒤 아래에서 새 단축키를 눌러 지정하세요.";
            listLabel.Location = new Point(S(14), S(12));
            listLabel.Size = new Size(S(392), S(20));
            Controls.Add(listLabel);

            _list = new ListView();
            _list.Location = new Point(S(14), S(36));
            _list.Size = new Size(S(392), S(404));
            _list.View = View.Details;
            _list.FullRowSelect = true;
            _list.MultiSelect = false;
            _list.HideSelection = false;
            _list.BorderStyle = BorderStyle.FixedSingle;
            _list.AccessibleName = "기능 목록";
            _list.TabIndex = 0;
            _list.Columns.Add("기능", S(160));
            _list.Columns.Add("단축키", S(150));
            _list.Columns.Add("분류", S(58));
            _list.SelectedIndexChanged += OnSelectionChanged;
            Controls.Add(_list);

            Label previewLabel = new Label();
            previewLabel.Text = "미리보기";
            previewLabel.Location = new Point(S(418), S(12));
            previewLabel.Size = new Size(S(228), S(20));
            Controls.Add(previewLabel);

            _preview = new PreviewPanel();
            _preview.Location = new Point(S(418), S(36));
            _preview.Size = new Size(S(228), S(178));
            _preview.AccessibleName = "배치 미리보기";
            Controls.Add(_preview);

            _previewNote = new Label();
            _previewNote.Location = new Point(S(418), S(222));
            _previewNote.Size = new Size(S(228), S(96));
            _previewNote.ForeColor = Color.FromArgb(90, 100, 115);
            Controls.Add(_previewNote);

            _cycleHalves = new CheckBox();
            _cycleHalves.Text = "같은 단축키를 연달아 누르면 1/2 → 1/3 → 2/3 으로 폭 바꾸기";
            _cycleHalves.Location = new Point(S(418), S(326));
            _cycleHalves.Size = new Size(S(228), S(48));
            _cycleHalves.FlatStyle = FlatStyle.Flat;
            _cycleHalves.Checked = _config.CycleHalves;
            Controls.Add(_cycleHalves);

            Label gapLabel = new Label();
            gapLabel.Text = "창 사이 여백(픽셀)";
            gapLabel.Location = new Point(S(418), S(382));
            gapLabel.Size = new Size(S(140), S(22));
            Controls.Add(gapLabel);

            _gap = new NumericUpDown();
            _gap.Location = new Point(S(418), S(406));
            _gap.Size = new Size(S(70), S(24));
            _gap.Minimum = 0;
            _gap.Maximum = 100;
            _gap.Value = Math.Max(0, Math.Min(100, _config.Gap));
            _gap.BorderStyle = BorderStyle.FixedSingle;
            _gap.AccessibleName = "창 사이 여백";
            _gap.ValueChanged += delegate { _preview.Invalidate(); };
            Controls.Add(_gap);

            Label captureLabel = new Label();
            captureLabel.Text = "새 단축키";
            captureLabel.Location = new Point(S(14), S(452));
            captureLabel.Size = new Size(S(70), S(22));
            Controls.Add(captureLabel);

            _capture = new HotkeyCaptureBox();
            _capture.Location = new Point(S(84), S(449));
            _capture.Size = new Size(S(230), S(26));
            _capture.AccessibleName = "새 단축키 입력";
            _capture.TabIndex = 1;
            Controls.Add(_capture);

            _winModifier = new CheckBox();
            _winModifier.Text = "Win 포함";
            _winModifier.Location = new Point(S(322), S(451));
            _winModifier.Size = new Size(S(84), S(22));
            _winModifier.FlatStyle = FlatStyle.Flat;
            Controls.Add(_winModifier);

            Button assign = MakeButton("이 단축키로 지정", S(14), S(484), S(130));
            assign.Click += OnAssign;
            Controls.Add(assign);

            Button clear = MakeButton("단축키 지우기", S(152), S(484), S(110));
            clear.Click += OnClear;
            Controls.Add(clear);

            Button reset = MakeButton("전체 기본값 복원", S(270), S(484), S(136));
            reset.Click += OnResetDefaults;
            Controls.Add(reset);

            Button save = MakeButton("저장", S(446), S(484), S(90));
            save.Click += OnSave;
            Controls.Add(save);

            Button cancel = MakeButton("취소", S(546), S(484), S(90));
            cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            CancelButton = cancel;
        }

        private Button MakeButton(string text, int x, int y, int width)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, S(30));
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            button.BackColor = Color.White;
            button.UseVisualStyleBackColor = false;
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
