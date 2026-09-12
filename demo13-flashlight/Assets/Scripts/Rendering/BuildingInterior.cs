using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 걸어 들어가는 건물 내부 — **플레이어가 안에 들어오면 지붕을 끈다.**
///
/// 3D 전환 결정(2026-09-08): 건물 내부를 별도 씬으로 전환하지 않고 **같은 맵 안에서
/// 걸어 들어간다.** 쿼터뷰라 지붕이 있으면 내부가 안 보이므로, 진입 판정 시 지붕
/// 렌더러를 끈다. 로딩이 없어 레이드의 긴장이 끊기지 않는 것이 이 방식의 요점이다.
///
/// 부착: 건물 루트. 트리거 볼륨(<see cref="BoxCollider"/> isTrigger)이 내부 공간을 덮게 한다.
/// 지붕은 <see cref="roof"/>에 넣거나, 이름이 "Roof"로 시작하는 자식을 자동으로 찾는다.
///
/// 설계: docs/building-interior.md
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class BuildingInterior : MonoBehaviour
{
    [Tooltip("끌 지붕 오브젝트. 비우면 이름이 'Roof'로 시작하는 자식을 자동 수집.")]
    [SerializeField] List<GameObject> roof = new List<GameObject>();

    [Tooltip("안에 들어왔을 때만 켜는 실내 조명(전구 등). 비우면 자식 Light를 전부 자동 수집.")]
    [SerializeField] List<Light> interiorLights = new List<Light>();

    [Tooltip("켜면 지붕이 꺼질 때 실내 조명도 함께 켜진다.")]
    [SerializeField] bool lightsFollowRoof = true;

    readonly List<Renderer> _roofRenderers = new List<Renderer>();
    readonly HashSet<Collider> _playerColliders = new HashSet<Collider>();
    bool _inside;

    public bool PlayerInside => _inside;

    void Awake()
    {
        var box = GetComponent<BoxCollider>();
        box.isTrigger = true;

        if (roof.Count == 0)
        {
            foreach (Transform t in transform)
                if (t.name.StartsWith("Roof")) roof.Add(t.gameObject);
        }
        foreach (var go in roof)
        {
            if (go == null) continue;
            _roofRenderers.AddRange(go.GetComponentsInChildren<Renderer>(true));
        }

        // 실내등도 지붕과 같이 자동 수집한다. 이게 없으면 방 안 전구가 **밖에서도 켜진 채**로
        // 남는다 — 점광은 벽을 뚫고 나가(추가 라이트 그림자를 끄고 쓴다) 건물 주변 바닥에
        // 이유 없는 밝은 원을 만든다. 안에 있을 때만 켜면 새는 순간 자체가 없어진다.
        if (interiorLights.Count == 0)
            interiorLights.AddRange(GetComponentsInChildren<Light>(true));

        SetInside(false);   // 시작은 지붕 있음 + 실내등 꺼짐
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerColliders.Add(other);
        SetInside(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!_playerColliders.Remove(other)) return;
        _playerColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        if (_playerColliders.Count == 0) SetInside(false);
    }

    void OnDisable()
    {
        _playerColliders.Clear();
        SetInside(false);
    }

    static bool IsPlayer(Collider c)
        => c.CompareTag("Player") || c.GetComponentInParent<TopDownPlayer>() != null;

    void SetInside(bool inside)
    {
        _inside = inside;
        foreach (var r in _roofRenderers)
            if (r != null) r.enabled = !inside;

        if (!lightsFollowRoof) return;
        foreach (var l in interiorLights)
            if (l != null) l.enabled = inside;
    }
}
