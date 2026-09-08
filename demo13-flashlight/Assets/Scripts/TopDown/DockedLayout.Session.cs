using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 한 번의 입양 동안 **건드린 것 전부**를 기억했다가 원상복구한다.
///
/// ⚠️ 남의 패널을 옮기고 늘리고 감추는 작업이라, 되돌리지 않으면 은신처 밖
/// (레이드 중 인벤토리 등)에서 그 UI가 찌그러진 채로 뜬다.
/// </summary>
public static partial class DockedLayout
{
    // ─────────────────────────────────────────────────────────────
    // 되돌리기
    // ─────────────────────────────────────────────────────────────

    /// <summary>한 번의 입양 동안 건드린 것 전부. <see cref="Restore"/>로 원상복구한다.</summary>
    public class Session
    {
        struct Saved
        {
            public RectTransform rt;
            public Transform parent;
            public int sibling;
            public Vector2 aMin, aMax, pivot, offMin, offMax;
            public Vector3 scale;
            public bool active;
        }

        readonly List<Saved> _saved = new List<Saved>();
        readonly List<GameObject> _created = new List<GameObject>();

        /// <summary>UI가 스스로 켜고 끄는 노드. 도크를 닫을 때 이걸 꺼야 UI가 닫힌 것이 된다.</summary>
        public RectTransform Root { get; internal set; }

        /// <summary>도크 안에 실제로 넣을 노드.</summary>
        public RectTransform Placed { get; internal set; }

        /// <summary>스크롤로 감쌌는가 — 그렇다면 도크 영역을 꽉 채워야 한다.</summary>
        public bool Scrolled { get; internal set; }

        /// <summary>스크롤 안 내용의 폭. 도크가 옆으로 넓어도 이 폭까지만 차지한다.</summary>
        public float ContentWidth { get; internal set; }

        /// <summary>도크가 늘리지 말고 **제 크기·제 배율 그대로** 가운데 두어야 하는가.
        /// 가로 유지형(창고)은 이미 균일 축소로 맞춰 놨으므로 다시 늘리면 배치가 깨진다.</summary>
        public bool KeepsOwnSize { get; internal set; }

        internal void Record(RectTransform rt)
        {
            if (rt == null) return;
            for (int i = 0; i < _saved.Count; i++)
                if (_saved[i].rt == rt) return;          // 처음 상태만 남긴다

            _saved.Add(new Saved
            {
                rt = rt, parent = rt.parent, sibling = rt.GetSiblingIndex(),
                aMin = rt.anchorMin, aMax = rt.anchorMax, pivot = rt.pivot,
                offMin = rt.offsetMin, offMax = rt.offsetMax,
                scale = rt.localScale, active = rt.gameObject.activeSelf,
            });
        }

        internal void Created(GameObject go) { if (go != null) _created.Add(go); }

        readonly List<Graphic> _muted = new List<Graphic>();

        /// <summary>지나친 껍데기의 배경 그림. 끈 것만 기억했다가 되돌린다.</summary>
        internal void RecordGraphic(Graphic g) { if (g != null) _muted.Add(g); }

        public void Restore()
        {
            // 나중에 건드린 것부터 되돌린다 — 부모를 되돌리기 전에 자식을 먼저 빼야 하는 경우가 있다.
            for (int i = _saved.Count - 1; i >= 0; i--)
            {
                var s = _saved[i];
                if (s.rt == null) continue;

                if (s.rt.parent != s.parent && s.parent != null)
                {
                    s.rt.SetParent(s.parent, false);
                    s.rt.SetSiblingIndex(s.sibling);
                }
                s.rt.anchorMin = s.aMin;
                s.rt.anchorMax = s.aMax;
                s.rt.pivot     = s.pivot;
                // offsetMin/Max는 앵커가 무엇이든 위치·크기를 함께 결정한다 — 이것만 되돌리면 된다.
                s.rt.offsetMin = s.offMin;
                s.rt.offsetMax = s.offMax;
                s.rt.localScale = s.scale;
                s.rt.gameObject.SetActive(s.active);
            }
            _saved.Clear();

            foreach (var go in _created)
                if (go != null) Object.Destroy(go);
            _created.Clear();

            foreach (var g in _muted) if (g != null) g.enabled = true;
            _muted.Clear();
        }
    }
}
