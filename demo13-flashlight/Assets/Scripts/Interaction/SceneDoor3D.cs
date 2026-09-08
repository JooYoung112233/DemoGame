using UnityEngine;

/// <summary>
/// 3D 씬 전환 문 — 밟으면 다른 씬으로 넘어간다.
///
/// 2D판 <see cref="BuildingEntrance"/>의 3D 대응. 그쪽은 <c>BoxCollider2D</c>에
/// <c>triggerSize</c>(Vector2)를 강제하는 구조라 3D 맵에서 동작하지 않는다.
/// 전환 자체는 같은 <see cref="SceneTransitionManager"/>를 쓴다 — 캐릭터·Systems 씬은 유지.
///
/// ⚠️ **복귀 스폰은 이 트리거 밖에 두어야 한다.** 안에 두면 복귀하자마자 다시 밟아
/// 무한 재진입이 된다(2D판에서 실제로 겪은 함정 — safehouse.md 참조).
/// 설계: docs/3d-migration.md
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class SceneDoor3D : MonoBehaviour
{
    [Tooltip("이동할 씬 이름. 빌드 세팅에 등록돼 있어야 한다.")]
    [SerializeField] string targetScene;

    [Tooltip("도착 씬에서 설 스폰 지점 id.")]
    [SerializeField] string spawnPointId = "default";

    [Tooltip("한 번 밟으면 재진입을 막는 시간(초). 전환 페이드 중 중복 발동 방지.")]
    [SerializeField] float reentryLock = 1.5f;

    float _lockUntil;

    public string TargetScene => targetScene;
    public string SpawnPointId => spawnPointId;

    public void Configure(string scene, string spawnId)
    {
        targetScene = scene;
        spawnPointId = spawnId;
    }

    void Awake() => GetComponent<BoxCollider>().isTrigger = true;

    void OnTriggerEnter(Collider other)
    {
        if (Time.unscaledTime < _lockUntil) return;
        if (string.IsNullOrEmpty(targetScene)) return;
        if (!(other.CompareTag("Player") || other.GetComponentInParent<TopDownPlayer>() != null)) return;

        _lockUntil = Time.unscaledTime + reentryLock;

        if (SceneTransitionManager.Instance != null)
            SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
        else
            Debug.LogWarning($"[SceneDoor3D] SceneTransitionManager가 없다 — '{targetScene}' 전환 불가.", this);
    }
}
