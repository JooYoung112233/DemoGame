using UnityEngine;

/// <summary>
/// **카메라** — 방을 어디에 담고 얼마나 물러날지.
///
/// 디오라마는 무대다: 무대(카메라)는 고정이고 배우(캐릭터)가 움직인다.
/// 시설을 누르면 도크가 차지하고 남은 화면 영역(<see cref="HideoutDockPanel.FreeScreenRect"/>)의
/// 가운데로 방을 옮긴다. 기본 화면부터 확대하고 우측 시설 창에서는 선택한 시설에 중심을 맞춘다.
///
/// 확대·축소를 만지고 싶으면 여기(또는 Control Panel의 은신처 화면 항목)만 보면 된다.
/// </summary>
public partial class HideoutDiorama
{

    // ── 카메라 수치 ─────────────────────────────────────────────────
    // Control Panel의 "은신처 화면 — 카메라"에서 만진다(GameTuning). 에셋이 없으면 기본값.
    // 인스펙터의 roomOrthoSize는 씬을 다시 빌드하면 날아가므로 여기를 정본으로 둔다.

    static GameTuning Tune => GameTuning.Instance;

    /// <summary>방을 담는 오소 크기. 줄이면 방이 크게 보인다.</summary>
    float RoomOrtho => Tune != null ? Tune.hideoutRoomOrtho : roomOrthoSize;

    /// <summary>기본 시야에서 방이 차지하는 반지름 비율(가로/세로). 줌아웃 계산의 기준.</summary>
    float RoomHalfX => Tune != null ? Tune.hideoutRoomHalfX : RoomHalfFracX;
    float RoomHalfY => Tune != null ? Tune.hideoutRoomHalfY : RoomHalfFracY;
    /// <summary>방 중심 포커스 오브젝트 — 시설 앵커들의 평균 위치. 카메라가 여기에 붙는다.</summary>
    void SetupRoomFocus()
    {
        if (roomCenter != null) { _roomFocus = roomCenter; return; }
        Vector3 sum = Vector3.zero; int n = 0;
        foreach (var a in _anchors) { if (a == null) continue; sum += a.transform.position; n++; }
        Vector3 c = n > 0 ? sum / n : _player.transform.position;
        c.y = 0f;
        var go = new GameObject("RoomFocus");
        go.transform.SetParent(transform, false);
        go.transform.position = c;
        _roomFocus = go.transform;
    }

    bool _framed;   // 현재 UI 자리만큼 밀려 있는가

    Vector2 _wantedBias;
    float   _wantedOrtho;
    bool    _focusApplied;
    int _frameWidth, _frameHeight;
    Quaternion _frameRotation;
    CameraFollow _focusRecipient;
    GameObject _closeupMask;
    RectTransform _closeupTop, _closeupBottom;

    /// <summary>전체 폭을 쓰는 기본 확대 화면. 상하단 UI 영역을 피한다.</summary>
    void FrameRoom() => FrameInto(HideoutDockPanel.FreeScreenRect(null, HideoutFacilityAnchor.Dock.None));

    /// <summary>도크가 차지하고 남은 화면 영역에 방을 맞춘다.
    ///
    /// 전체 방이 들어가는 크기를 기준으로 공통 배율을 적용하고,
    /// 도크가 열려 있으면 선택 시설을 남는 영역의 중심으로 옮긴다.</summary>
    void FrameInto(Rect free, Vector2 overrideBias = default)
    {
        Vector2 center = new Vector2(free.x + free.width * 0.5f, free.y + free.height * 0.5f);
        Vector2 projectedCenter = Vector2.zero;
        _wantedBias = overrideBias != Vector2.zero ? overrideBias : center - new Vector2(0.5f, 0.5f);
        // The default room also has a vertical bias because of HUD margins; it is not an open dock.
        _framed = free.width < .999f || free.yMin > 112f / HideoutDockPanel.RefHeight + .001f;
        _frameWidth = Screen.width; _frameHeight = Screen.height;
        if (_cam != null) _frameRotation = _cam.transform.rotation;

        // 방이 기본 시야에서 차지하는 반지름 비율(가로/세로)을 기준으로 필요한 오소를 구한다.
        float needX = RoomOrtho * RoomHalfX / Mathf.Max(free.width  * 0.5f, 0.08f);
        float needY = RoomOrtho * RoomHalfY / Mathf.Max(free.height * 0.5f, 0.08f);
        _wantedOrtho = Mathf.Max(RoomOrtho, Mathf.Max(needX, needY));

        // Fit the actual art silhouette, including the high back wall, instead of estimates from the old room.
        if (_cam != null && _roomFocus != null)
        {
            Transform art = null;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Hideout02_Placed") { art = t; break; }
            if (art != null)
            {
                Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
                Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
                foreach (var mf in art.GetComponentsInChildren<MeshFilter>())
                {
                    var renderer = mf.GetComponent<Renderer>();
                    if (mf.sharedMesh == null || renderer == null || !renderer.enabled) continue;
                    var bounds = mf.sharedMesh.bounds;
                    for (int corner = 0; corner < 8; corner++)
                    {
                        Vector3 sign = new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1);
                        Vector3 world = mf.transform.TransformPoint(bounds.center + Vector3.Scale(bounds.extents, sign)) - _roomFocus.position;
                        Vector2 projected = new Vector2(Vector3.Dot(world, _cam.transform.right), Vector3.Dot(world, _cam.transform.up));
                        min = Vector2.Min(min, projected); max = Vector2.Max(max, projected);
                    }
                }
                if (!float.IsInfinity(min.x))
                {
                    float aspect = Mathf.Max(.1f, _cam.aspect);
                    // Small margin around the complete silhouette, within the UI-free rectangle.
                    _wantedOrtho = Mathf.Max((max.x - min.x) / (2f * aspect * Mathf.Max(.1f, free.width)),
                        (max.y - min.y) / (2f * Mathf.Max(.1f, free.height))) * 1.06f;
                    projectedCenter = (min + max) * .5f;
                }
            }
        }

        float zoom = Mathf.Max(1f, Tune != null ? Tune.hideoutViewZoom : 2f);
        _wantedOrtho /= zoom;
        SetCloseupMask(zoom > 1f, free);
        if (_cam != null && _roomFocus != null)
        {
            if (free.width < .999f && _current != null && _current.dock == HideoutFacilityAnchor.Dock.Right)
            {
                // 방 전체 중심 대신 시설과 캐릭터가 자리 잡을 지점을 함께 담는다.
                Vector3 target = Vector3.Lerp(_current.transform.position, _current.StandPosition, .5f) + Vector3.up * .65f;
                Vector3 offset = target - _roomFocus.position;
                projectedCenter = new Vector2(Vector3.Dot(offset, _cam.transform.right), Vector3.Dot(offset, _cam.transform.up));
            }
            _wantedBias -= new Vector2(projectedCenter.x / (2f * _wantedOrtho * Mathf.Max(.1f, _cam.aspect)),
                projectedCenter.y / (2f * _wantedOrtho));
        }

        _focusApplied = false;
        TryApplyFocus();
    }

    void SetCloseupMask(bool visible, Rect free)
    {
        if (_closeupMask == null && visible)
        {
            _closeupMask = new GameObject("CloseupHudMargins", typeof(Canvas));
            _closeupMask.transform.SetParent(transform, false);
            var canvas = _closeupMask.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 34; // 방 위, 제목/시설/출구 UI(35) 아래
            foreach (bool top in new[] { true, false })
            {
                var go = new GameObject(top ? "Top" : "Bottom", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                go.transform.SetParent(_closeupMask.transform, false);
                var image = go.GetComponent<UnityEngine.UI.Image>();
                image.color = Color.black;
                image.raycastTarget = false;
                if (top) _closeupTop = image.rectTransform; else _closeupBottom = image.rectTransform;
            }
        }
        if (_closeupMask == null) return;
        _closeupMask.SetActive(visible);
        if (!visible) return;
        _closeupTop.anchorMin = new Vector2(0f, free.yMax);
        _closeupTop.anchorMax = new Vector2(free.xMax, 1f);
        _closeupBottom.anchorMin = Vector2.zero;
        _closeupBottom.anchorMax = new Vector2(free.xMax, free.yMin);
        _closeupTop.offsetMin = _closeupTop.offsetMax = Vector2.zero;
        _closeupBottom.offsetMin = _closeupBottom.offsetMax = Vector2.zero;
    }

    /// <summary>⚠️ CameraFollow는 PlayerRig와 함께 나중에 생길 수 있다.
    /// Start에서 한 번만 시도하면 조용히 실패해 카메라가 계속 캐릭터를 따라간다 — 붙을 때까지 재시도.</summary>
    void TryApplyFocus()
    {
        if (_roomFocus == null) return;
        var cf = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : CameraFollow.Instance;
        if (cf == null) return;
        // Systems may remove a duplicate rig after our Start; bind the surviving camera once ready.
        if (_focusApplied && _focusRecipient == cf) return;
        _cam = cf.GetComponent<Camera>();
        cf.SetFocus(_roomFocus, _wantedBias, _wantedOrtho);
        _focusRecipient = cf;
        _focusApplied = true;
    }

}
