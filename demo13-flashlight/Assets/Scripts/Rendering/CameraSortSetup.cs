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
        // 탑다운: 같은 sortingOrder 스프라이트는 카메라 시선 깊이로 정렬.
        // (iso 시절엔 세워진 빌보드라 (0,1,0) Y축으로 정렬했음.)
        cam.transparencySortAxis = TopDownMapEditor.TopDownGrid.ViewDir;
    }
}
