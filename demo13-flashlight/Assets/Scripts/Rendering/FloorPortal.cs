using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 층 전환 — **계단을 실제로 오르지 않는다.**
///
/// 3D 전환 결정(2026-09-08): 2층 건물은 계단을 걸어 올라가는 대신 **어느 지점에 서면
/// 층이 전환된다.** 카메라·플레이어가 그 층으로 옮겨지고, 보여줄 층만 켠다.
/// 계단 오르기를 실제로 구현하면 카메라 각도·오클루전·AI 경로가 전부 다층화되어
/// 비용이 급증한다 — 쿼터뷰에서는 층을 "갈아끼우는" 편이 읽기도 쉽다.
///
/// 부착: 계단·사다리 자리의 트리거. <see cref="targetPortal"/>에 반대편 층의 포탈을 연결한다.
/// 층 묶음(<see cref="floorRoot"/>)은 그 층에 속한 오브젝트들의 부모.
///
/// 설계: docs/building-interior.md
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class FloorPortal : MonoBehaviour
{
    [Tooltip("이 포탈이 속한 층의 오브젝트 묶음.")]
    [SerializeField] GameObject floorRoot;

    [Tooltip("반대편 층의 포탈. 여기로 플레이어를 옮긴다.")]
    [SerializeField] FloorPortal targetPortal;

    [Tooltip("도착 시 포탈에서 밀어낼 거리(m) — 즉시 재전환되는 왕복 루프 방지.")]
    [SerializeField] float exitOffset = 1.2f;

    [Tooltip("같은 건물의 모든 층 묶음. 전환 시 도착 층만 켠다.")]
    [SerializeField] List<GameObject> allFloors = new List<GameObject>();

    bool _suppress;   // 방금 도착한 포탈이 즉시 재발동하지 않게

    public GameObject FloorRoot => floorRoot;

    void Awake() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (_suppress) return;
        if (!(other.CompareTag("Player") || other.GetComponentInParent<TopDownPlayer>() != null)) return;
        if (targetPortal == null) { Debug.LogWarning("[FloorPortal] targetPortal 미연결", this); return; }

        var player = other.GetComponentInParent<TopDownPlayer>();
        if (player == null) return;

        // 도착 지점 = 반대편 포탈에서 조금 밀어낸 곳
        Vector3 dest = targetPortal.transform.position
                     + targetPortal.transform.forward * exitOffset;

        var rb = player.GetComponent<Rigidbody>();
        if (rb != null) { rb.position = dest; rb.linearVelocity = Vector3.zero; }
        player.transform.position = dest;
        Physics.SyncTransforms();

        // 보여줄 층만 켠다
        var floors = allFloors.Count > 0 ? allFloors : targetPortal.allFloors;
        foreach (var f in floors)
            if (f != null) f.SetActive(f == targetPortal.floorRoot);

        // 카메라는 "슉~" 미끄러지지 않게 즉시 스냅한다(층 전환은 순간이동이다).
        if (CameraFollow.Instance != null) CameraFollow.Instance.SnapToTarget();

        targetPortal.SuppressOnce();
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<TopDownPlayer>() != null)
            _suppress = false;
    }

    /// <summary>도착 직후 1회 발동 억제 — 플레이어가 이 트리거를 벗어날 때 풀린다.</summary>
    public void SuppressOnce() => _suppress = true;
}
