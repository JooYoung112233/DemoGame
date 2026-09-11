using UnityEngine;

/// <summary>
/// 도크가 화면에서 **차지할 자리**를 정한다 — 도크 크기, 여백, 상단 띠, 남는 영역.
///
/// 여기만 고치면 UI 크기와 카메라 프레이밍이 같이 따라온다.
/// (<see cref="HideoutDiorama"/>가 <see cref="FreeScreenRect"/>로 방을 어디에 담을지 계산한다.)
/// 수치는 <see cref="GameTuning"/>에서 읽고, 에셋이 없으면 아래 기본값으로 떨어진다.
///
/// 설계: docs/hideout-3d.md
/// </summary>
public partial class HideoutDockPanel
{
    // ── 도크가 화면에서 차지할 자리 ──────────────────────────────────
    // 실측(2026-09-08, 1920 기준): 제작 520×480 / 침대 720×460 / 파견 조작부 360×964
    //                              라디오 540+1276 2열 / 창고 643(격자)
    //
    // ⚠️ 도크를 좁게(560) 잡으면 1920 기준으로 짠 패널이 통째로 줄어들어 글씨를 못 읽는다.
    //    캐릭터만 안 가리면 되므로 화면의 43% 정도까지 넉넉히 준다. 나머지에 방을 담고
    //    카메라가 그만큼 방을 왼쪽으로 민다(FreeScreenRect → HideoutDiorama).
    //
    // 값은 **Control Panel의 "은신처 화면" 항목**(GameTuning)에서 만진다.
    // 에셋이 없으면 아래 기본값으로 떨어진다 — 크래시 없이 그대로 돈다.

    public const float RefWidth  = 1920f;
    public const float RefHeight = 1080f;

    const float DefDockW = 840f, DefDockH = 960f, DefMargin = 32f, DefTopBand = 180f;

    static GameTuning Tune => GameTuning.Instance;

    /// <summary>도크와 화면 오른쪽 사이 여백.</summary>
    public static float RightMargin => Tune != null ? Tune.hideoutDockMargin : DefMargin;

    /// <summary>하단 바와 화면 아래 사이 여백.</summary>
    public const float BottomMargin = 28f;

    /// <summary>우측 도크 크기. **전 시설 동일** — 시설마다 다르면 열 때마다 화면이
    /// 들쭉날쭉해 산만하다. 시설별로 갈라야 할 일이 생기면 여기서만 분기하면 된다.</summary>
    public static Vector2 RightDockSize(string moduleKey) =>
        Tune != null ? new Vector2(Tune.hideoutDockWidth, Tune.hideoutDockHeight)
                     : new Vector2(DefDockW, DefDockH);

    /// <summary>도크가 가장 넓을 때의 폭. 상단 제목·시설 바가 이 밑으로 들어가면 안 된다.</summary>
    public static float MaxRightDockWidth => RightDockSize(null).x;

    /// <summary>하단 바 높이. 지금은 쓰는 시설이 없다 — 침대까지 우측 도크로 통일했다.
    /// (수면창 720×460이 커서 하단 바를 높이면 방이 다른 시설보다 작게 잡혔다.)
    /// Dock.Bottom 자체는 살려 둔다 — 짧은 상태 표시용으로는 여전히 맞는 자리다.</summary>
    public static float BottomDockHeight(string moduleKey) => 220f;

    /// <summary>제목 + 안내 + 시설 바가 쓰는 화면 상단 띠 높이.</summary>
    public static float TopBandHeight => Tune != null ? Tune.hideoutTopBand : DefTopBand;

    /// <summary>도크와 상단 띠를 뺀, **방을 담을 수 있는 화면 영역**(정규화 0~1, y는 아래가 0).
    /// 카메라 프레이밍은 여기서 나온다 — 편향과 줌을 따로 손으로 맞추면 도크 크기를 바꿀 때마다
    /// 방이 UI 밑으로 들어가거나 화면 밖으로 잘린다.</summary>
    public static Rect FreeScreenRect(string moduleKey, HideoutFacilityAnchor.Dock dock)
    {
        float top = TopBandHeight / RefHeight;

        if (dock == HideoutFacilityAnchor.Dock.Bottom)
        {
            float b = (BottomDockHeight(moduleKey) + BottomMargin) / RefHeight;
            return new Rect(0f, b, 1f, Mathf.Max(1f - top - b, 0.1f));
        }

        // 도크가 없으면(대기 상태) 상단 띠만 피하고 화면 전체를 쓴다.
        float r = dock == HideoutFacilityAnchor.Dock.None
                ? 0f
                : (RightDockSize(moduleKey).x + RightMargin) / RefWidth;
        // Leave the existing quick slots and bottom navigation usable without covering the room.
        float bottom = 112f / RefHeight;
        return new Rect(0f, bottom, Mathf.Max(1f - r, 0.1f), Mathf.Max(1f - top - bottom, 0.1f));
    }
}
