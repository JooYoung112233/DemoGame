using UnityEngine;

/// <summary>
/// 카메라의 Transparency Sort Axis를 Y축 기반으로 설정.
/// 메인 카메라에 붙이면 됨.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraSortSetup : MonoBehaviour
{
    void Awake()
    {
        var cam = GetComponent<Camera>();
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0, 1, 0);
    }
}
