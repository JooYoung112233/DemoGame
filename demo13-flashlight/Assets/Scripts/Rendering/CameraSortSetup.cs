using UnityEngine;

/// <summary>
/// 카메라의 Transparency Sort Axis를 시점 깊이축(카메라 시선)으로 설정.
/// 메인 카메라에 붙이면 됨. (탑다운 전환: Y축 → 시선축)
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraSortSetup : MonoBehaviour
{
    void Awake()
    {
        var cam = GetComponent<Camera>();
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        // 탑다운 2D: Y축(화면 상하)으로 정렬 — Y가 낮을수록 앞(화면 아래 = 카메라에 가까운 쪽).
        cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
    }
}
