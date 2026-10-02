using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RumiFlowWindows
{
    /// <summary>전역 단축키를 등록하고, 눌렸을 때 알려 주는 숨은 창.</summary>
    public sealed class HotkeyManager : NativeWindow, IDisposable
    {
        private readonly Dictionary<int, SnapAction> _registered = new Dictionary<int, SnapAction>();
        private int _nextId = 0xB100;

        /// <summary>단축키가 눌렸을 때 호출된다.</summary>
        public event Action<SnapAction> HotkeyPressed;

        public HotkeyManager()
        {
            CreateHandle(new CreateParams());
        }

        /// <summary>설정에 있는 단축키를 모두 등록한다. 등록에 실패한 항목 목록을 돌려준다.</summary>
        public List<string> RegisterAll(Config config)
        {
            UnregisterAll();

            List<string> failed = new List<string>();
            SnapAction[] ordered = SnapActions.Ordered;

            for (int i = 0; i < ordered.Length; i++)
            {
                SnapAction action = ordered[i];
                Hotkey hotkey = config.Get(action);
                if (hotkey.IsEmpty || !hotkey.HasModifier)
                {
                    continue;
                }

                int id = _nextId++;
                bool ok = Native.RegisterHotKey(Handle, id, hotkey.Modifiers | Native.MOD_NOREPEAT,
                    (uint)hotkey.Key);
                if (ok)
                {
                    _registered[id] = action;
                }
                else
                {
                    failed.Add(SnapActions.Label(action) + " (" + hotkey.ToDisplayString() + ")");
                }
            }

            return failed;
        }

        public void UnregisterAll()
        {
            foreach (KeyValuePair<int, SnapAction> pair in _registered)
            {
                Native.UnregisterHotKey(Handle, pair.Key);
            }
            _registered.Clear();
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                SnapAction action;
                if (_registered.TryGetValue(id, out action))
                {
                    Action<SnapAction> handler = HotkeyPressed;
                    if (handler != null)
                    {
                        handler(action);
                    }
                    return;
                }
            }
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            UnregisterAll();
            if (Handle != IntPtr.Zero)
            {
                DestroyHandle();
            }
        }
    }
}
