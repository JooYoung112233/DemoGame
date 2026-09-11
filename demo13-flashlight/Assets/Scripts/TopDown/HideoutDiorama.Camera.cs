using UnityEngine;

/// <summary>
/// **카메라** — 방을 어디에 담고 얼마나 물러날지.
///
/// 디오라마는 무대다: 무대(카메라)는 고정이고 배우(캐릭터)가 움직인다.
/// 시설을 누르면 도크가 차지하고 남은 화면 영역(<see cref="HideoutDockPanel.FreeScreenRect"/>)의
/// 가운데로 방을 옮기고, 그 영역에 들어갈 만큼 줌아웃한다.
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

    /// <summary>화면 전체를 쓰는 기본 프레이밍(아무것도 안 눌렀을 때). 상단 띠만 피한다.</summary>
    void FrameRoom() => FrameInto(HideoutDockPanel.FreeScreenRect(null, HideoutFacilityAnchor.Dock.None));

    /// <summary>도크가 차지하고 남은 화면 영역에 방을 맞춘다.
    ///
    /// 편향과 줌을 손으로 따로 맞추면 도크 크기를 바꿀 때마다 방이 UI 밑으로 들어가거나
    /// 화면 밖으로 잘린다. 남는 영역의 **중심으로 옮기고, 그 영역에 들어갈 만큼 줌아웃**한다.</summary>
    void FrameInto(Rect free, Vector2 overrideBias = default)
    {
        Vector2 center = new Vector2(free.x + free.width * 0.5f, free.y + free.height * 0.5f);
        _wantedBias = overrideBias != Vector2.zero ? overrideBias : center - new Vector2(0.5f, 0.5f);
        _framed     = _wantedBias.sqrMagnitude > 0.000001f;

        // 방이 기본 시야에서 차지하는 반지름 비율(가로/세로)을 기준으로 필요한 오소를 구한다.
        float needX = RoomOrtho * RoomHalfX / Mathf.Max(free.width  * 0.5f, 0.08f);
        float needY = RoomOrtho * RoomHalfY / Mathf.Max(free.height * 0.5f, 0.08f);
        _wantedOrtho = Mathf.Max(RoomOrtho, Mathf.Max(needX, needY));

        _focusApplied = false;
        TryApplyFocus();
    }

    /// <summary>⚠️ CameraFollow는 PlayerRig와 함께 나중에 생길 수 있다.
    /// Start에서 한 번만 시도하면 조용히 실패해 카메라가 계속 캐릭터를 따라간다 — 붙을 때까지 재시도.</summary>
    void TryApplyFocus()
    {
        if (_focusApplied || _roomFocus == null) return;
        var cf = CameraFollow.Instance;
        if (cf == null) return;
        cf.SetFocus(_roomFocus, _wantedBias, _wantedOrtho);
        _focusApplied = true;
    }

}
