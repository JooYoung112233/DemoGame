using UnityEngine;

/// <summary>
/// 은신처 시설 — **캐릭터가 그 시설에서 취할 자리·자세**와 UI 배치 규칙.
///
/// 2026-09-08 결정: 은신처는 걸어다니지 않는다(2D판의 타르코프식 클릭 유지). 다만 2D판처럼
/// **캐릭터를 숨기지 않고**, 클릭한 시설에 캐릭터가 가서 자세를 잡는 **디오라마**로 만든다.
///   • 침대 → 눕는다   • 요리대 → 앞에 선다   • 아무것도 안 누르면 → 의자에 앉아 있다
///
/// UI는 **캐릭터를 가리지 않는다.** 시설마다 UI가 붙을 자리를 <see cref="dock"/>로 정하고,
/// 카메라가 부드럽게 이동해 캐릭터를 반대쪽으로 밀어 자리를 비운다(<see cref="cameraBias"/>).
///
/// 설계: docs/hideout-3d.md
/// </summary>
public class HideoutFacilityAnchor : MonoBehaviour
{
    public enum Pose { Stand, Sit, Lie }
    public enum Dock { Right, Bottom, None }

    [Tooltip("이 시설의 모듈 키 — HideoutUI.Show(module)에 넘기는 값과 같아야 한다.")]
    public string moduleKey;

    [Tooltip("캐릭터가 설 자리. 비우면 이 오브젝트 앞쪽 standOffset 만큼 떨어진 곳.")]
    public Transform standPoint;

    [Tooltip("standPoint가 없을 때 시설 중심에서 떨어질 거리(m).")]
    public float standOffset = 1.1f;

    [Tooltip("그 자리에서 취할 자세.")]
    public Pose pose = Pose.Stand;

    [Tooltip("UI가 붙을 자리. 캐릭터를 가리지 않게 반대쪽으로 카메라가 이동한다.")]
    public Dock dock = Dock.Right;

    [Tooltip("화면 비율 편향. 비우면 dock에서 자동 산출(우측 도킹이면 캐릭터를 왼쪽으로).")]
    public Vector2 cameraBias = Vector2.zero;

    [Tooltip("이 시설을 볼 때의 오소 크기. 0이면 유지.")]
    public float cameraSize = 0f;

    /// <summary>캐릭터가 설 월드 좌표.</summary>
    public Vector3 StandPosition
        => standPoint != null ? standPoint.position
                              : transform.position - transform.forward * standOffset;

    /// <summary>캐릭터가 바라볼 방향(시설 쪽).</summary>
    public Vector3 FaceDirection
    {
        get
        {
            Vector3 d = transform.position - StandPosition;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : transform.forward;
        }
    }

    /// <summary>UI 자리를 비우기 위한 화면 편향. 직접 지정하지 않았으면 도크 크기에서 계산한다.
    ///
    /// 남는 자리(도크를 뺀 쪽)의 한가운데로 방을 옮기면 된다. 도크 폭이 W, 여백이 M이면
    /// 남는 자리의 중심은 화면 중앙에서 (W+M)/2 만큼 왼쪽이므로 편향 = -(W+M)/2/화면폭.
    /// ⚠️ 예전처럼 고정값(-0.18)을 쓰면 도크를 키운 순간 캐릭터가 도크 밑에 깔린다.</summary>
    public Vector2 ResolvedBias
    {
        get
        {
            if (cameraBias != Vector2.zero) return cameraBias;
            return dock switch
            {
                // 우측에 UI → 캐릭터를 왼쪽으로
                Dock.Right => new Vector2(
                    -(HideoutDockPanel.RightDockSize(moduleKey).x + HideoutDockPanel.RightMargin)
                    / (2f * HideoutDockPanel.RefWidth),
                    0f),
                // 하단에 UI → 캐릭터를 위로
                Dock.Bottom => new Vector2(
                    0f,
                    (HideoutDockPanel.BottomDockHeight(moduleKey) + HideoutDockPanel.BottomMargin)
                    / (2f * HideoutDockPanel.RefHeight)),
                _ => Vector2.zero,
            };
        }
    }
}
