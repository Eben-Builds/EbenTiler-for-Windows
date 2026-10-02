using System;
using System.Collections.Generic;

namespace RumiFlowWindows
{
    /// <summary>창을 어떻게 배치할지 나타내는 명령 목록.</summary>
    public enum SnapAction
    {
        LeftHalf,
        RightHalf,
        TopHalf,
        BottomHalf,

        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,

        FirstThird,
        CenterThird,
        LastThird,
        FirstTwoThirds,
        LastTwoThirds,

        Maximize,
        MaximizeHeight,
        Center,
        Larger,
        Smaller,
        Restore,

        NextDisplay,
        PreviousDisplay
    }

    /// <summary>명령의 순서, 한국어 이름, 기본 단축키를 한곳에서 관리한다.</summary>
    public static class SnapActions
    {
        private static readonly SnapAction[] _ordered = new SnapAction[]
        {
            SnapAction.LeftHalf,
            SnapAction.RightHalf,
            SnapAction.TopHalf,
            SnapAction.BottomHalf,
            SnapAction.TopLeft,
            SnapAction.TopRight,
            SnapAction.BottomLeft,
            SnapAction.BottomRight,
            SnapAction.FirstThird,
            SnapAction.CenterThird,
            SnapAction.LastThird,
            SnapAction.FirstTwoThirds,
            SnapAction.LastTwoThirds,
            SnapAction.Maximize,
            SnapAction.MaximizeHeight,
            SnapAction.Center,
            SnapAction.Larger,
            SnapAction.Smaller,
            SnapAction.Restore,
            SnapAction.NextDisplay,
            SnapAction.PreviousDisplay
        };

        private static readonly Dictionary<SnapAction, string> _labels = BuildLabels();
        private static readonly Dictionary<SnapAction, string> _groups = BuildGroups();

        public static SnapAction[] Ordered { get { return _ordered; } }

        private static Dictionary<SnapAction, string> BuildLabels()
        {
            Dictionary<SnapAction, string> map = new Dictionary<SnapAction, string>();
            map[SnapAction.LeftHalf] = "왼쪽 절반";
            map[SnapAction.RightHalf] = "오른쪽 절반";
            map[SnapAction.TopHalf] = "위쪽 절반";
            map[SnapAction.BottomHalf] = "아래쪽 절반";
            map[SnapAction.TopLeft] = "왼쪽 위 1/4";
            map[SnapAction.TopRight] = "오른쪽 위 1/4";
            map[SnapAction.BottomLeft] = "왼쪽 아래 1/4";
            map[SnapAction.BottomRight] = "오른쪽 아래 1/4";
            map[SnapAction.FirstThird] = "왼쪽 1/3";
            map[SnapAction.CenterThird] = "가운데 1/3";
            map[SnapAction.LastThird] = "오른쪽 1/3";
            map[SnapAction.FirstTwoThirds] = "왼쪽 2/3";
            map[SnapAction.LastTwoThirds] = "오른쪽 2/3";
            map[SnapAction.Maximize] = "전체 화면(최대화)";
            map[SnapAction.MaximizeHeight] = "세로만 최대";
            map[SnapAction.Center] = "가운데 정렬";
            map[SnapAction.Larger] = "크기 키우기";
            map[SnapAction.Smaller] = "크기 줄이기";
            map[SnapAction.Restore] = "원래 크기 복원";
            map[SnapAction.NextDisplay] = "다음 모니터로";
            map[SnapAction.PreviousDisplay] = "이전 모니터로";
            return map;
        }

        private static Dictionary<SnapAction, string> BuildGroups()
        {
            Dictionary<SnapAction, string> map = new Dictionary<SnapAction, string>();
            map[SnapAction.LeftHalf] = "절반";
            map[SnapAction.RightHalf] = "절반";
            map[SnapAction.TopHalf] = "절반";
            map[SnapAction.BottomHalf] = "절반";
            map[SnapAction.TopLeft] = "사분면";
            map[SnapAction.TopRight] = "사분면";
            map[SnapAction.BottomLeft] = "사분면";
            map[SnapAction.BottomRight] = "사분면";
            map[SnapAction.FirstThird] = "3분할";
            map[SnapAction.CenterThird] = "3분할";
            map[SnapAction.LastThird] = "3분할";
            map[SnapAction.FirstTwoThirds] = "3분할";
            map[SnapAction.LastTwoThirds] = "3분할";
            map[SnapAction.Maximize] = "크기";
            map[SnapAction.MaximizeHeight] = "크기";
            map[SnapAction.Center] = "크기";
            map[SnapAction.Larger] = "크기";
            map[SnapAction.Smaller] = "크기";
            map[SnapAction.Restore] = "크기";
            map[SnapAction.NextDisplay] = "모니터";
            map[SnapAction.PreviousDisplay] = "모니터";
            return map;
        }

        public static string Label(SnapAction action)
        {
            string value;
            if (_labels.TryGetValue(action, out value))
            {
                return value;
            }
            return action.ToString();
        }

        public static string Group(SnapAction action)
        {
            string value;
            if (_groups.TryGetValue(action, out value))
            {
                return value;
            }
            return "기타";
        }

        /// <summary>기본 단축키 표. Ctrl+방향키는 텍스트 편집의 단어 이동과 겹치므로 Ctrl+Alt를 쓴다.</summary>
        public static Dictionary<SnapAction, string> DefaultHotkeys()
        {
            Dictionary<SnapAction, string> map = new Dictionary<SnapAction, string>();
            map[SnapAction.LeftHalf] = "Ctrl+Alt+Left";
            map[SnapAction.RightHalf] = "Ctrl+Alt+Right";
            map[SnapAction.TopHalf] = "Ctrl+Alt+Up";
            map[SnapAction.BottomHalf] = "Ctrl+Alt+Down";
            map[SnapAction.TopLeft] = "Ctrl+Alt+U";
            map[SnapAction.TopRight] = "Ctrl+Alt+I";
            map[SnapAction.BottomLeft] = "Ctrl+Alt+J";
            map[SnapAction.BottomRight] = "Ctrl+Alt+K";
            map[SnapAction.FirstThird] = "Ctrl+Alt+D";
            map[SnapAction.CenterThird] = "Ctrl+Alt+F";
            // Ctrl+Alt+G 는 다른 프로그램(게임 오버레이 등)이 선점하는 경우가 잦아 H 를 쓴다.
            map[SnapAction.LastThird] = "Ctrl+Alt+H";
            map[SnapAction.FirstTwoThirds] = "Ctrl+Alt+E";
            map[SnapAction.LastTwoThirds] = "Ctrl+Alt+T";
            map[SnapAction.Maximize] = "Ctrl+Alt+Enter";
            map[SnapAction.MaximizeHeight] = "Ctrl+Alt+Shift+Up";
            map[SnapAction.Center] = "Ctrl+Alt+C";
            map[SnapAction.Larger] = "Ctrl+Alt+Oemplus";
            map[SnapAction.Smaller] = "Ctrl+Alt+OemMinus";
            map[SnapAction.Restore] = "Ctrl+Alt+Back";
            map[SnapAction.NextDisplay] = "Ctrl+Alt+Shift+Right";
            map[SnapAction.PreviousDisplay] = "Ctrl+Alt+Shift+Left";
            return map;
        }

        public static bool TryParse(string name, out SnapAction action)
        {
            action = SnapAction.LeftHalf;
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            for (int i = 0; i < _ordered.Length; i++)
            {
                if (string.Equals(_ordered[i].ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    action = _ordered[i];
                    return true;
                }
            }
            return false;
        }
    }
}
