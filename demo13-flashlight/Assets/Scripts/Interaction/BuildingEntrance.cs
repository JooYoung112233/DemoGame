using UnityEngine;

/// <summary>
/// 건물 트리거 전환 — 플레이어가 트리거에 '들어오면' 자동으로 다른 씬으로 전환한다(페이드+additive).
/// (docs/safehouse.md '건물 전환 모델' — 전당포/은신처 입구·전당포 출구)
///
/// • E키 상호작용(InteractableObject.ExitPoint)이 아니라, Collider2D(isTrigger) 위를 밟으면 자동 발동.
/// • 진입·퇴장 둘 다 같은 컴포넌트(둘 다 트리거). isExit는 표시/세이브 체크포인트 분기용.
/// • 전환은 SceneTransitionManager.TransitionTo(targetScene, spawnPointId) — 캐릭터/시스템(Systems 씬)은 유지.
/// • 세이브 체크포인트(트랙 A) — 진입 시 BuildingEntered(), 퇴장 시 BuildingExited() (있을 때만, ?. 가드).
///
/// 주의(하이드아웃 예외): 하이드아웃은 '캐릭터 없는 클릭 화면'이라 트리거 퇴장이 불가능 —
/// 진입만 BuildingEntrance로 처리하고 퇴장은 HideoutController의 UI '나가기'/ESC가 담당한다.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BuildingEntrance : MonoBehaviour
{
    [Header("── 전환 대상 ──")]
    [Tooltip("이동할 씬 이름 (Build Settings 등록 필수). 비우면 아무것도 안 함.")]
    [SerializeField] string targetScene;

    [Tooltip("도착 씬에서 플레이어가 스폰될 SpawnPoint의 ID")]
    [SerializeField] string spawnPointId = "default";

    [Tooltip("표시/세이브용. true=건물에서 나가는 출구(BuildingExited), false=건물로 들어가는 입구(BuildingEntered)")]
    [SerializeField] bool isExit = false;

    // 중복 발동 방지(전환이 시작되면 다시 트리거에 닿아도 무시).
    bool _fired;

    public string TargetScene => targetScene;
    public string SpawnPointId => spawnPointId;
    public bool IsExit => isExit;

    /// <summary>빌더/런타임에서 한 번에 설정.</summary>
    public void Configure(string scene, string spawnId, bool exit)
    {
        targetScene = scene;
        spawnPointId = spawnId;
        isExit = exit;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_fired) return;
        if (string.IsNullOrEmpty(targetScene)) return;
        if (!other.CompareTag("Player")) return;

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[BuildingEntrance] SceneTransitionManager가 없습니다.");
            return;
        }

        // 전환 중이면(다른 BuildingEntrance가 이미 발동) 무시.
        _fired = true;

        // 세이브 체크포인트(트랙 A — 없을 수 있으니 ?. 가드).
        if (isExit) SaveCheckpoints.Instance?.BuildingExited();
        else        SaveCheckpoints.Instance?.BuildingEntered();

        SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        // 같은 트리거를 벗어나면(전환이 취소됐거나 도착 직후 스폰이 트리거 밖이면) 재발동 허용.
        if (!other.CompareTag("Player")) return;
        _fired = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = isExit ? new Color(0.3f, 0.85f, 1f, 0.5f) : new Color(1f, 0.7f, 0.2f, 0.5f);
        var col = GetComponent<Collider2D>();
        if (col != null) Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
    }
}
