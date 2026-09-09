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

    [Header("── 트리거 크기 ──")]
    [Tooltip("진입 트리거(문) 크기. 너무 넓으면 문에 도달 전/건물 앞을 지나가다 발동한다. 기본 1.3 x 1.0(문 한 칸). Awake에서 BoxCollider2D에 강제 적용.")]
    [SerializeField] Vector2 triggerSize = new Vector2(1.3f, 1.0f);

    // 중복 발동 방지(전환이 시작되면 다시 트리거에 닿아도 무시).
    bool _fired;

    // 2026-07-11: 씬 로드 직후 **스폰 지점이 발판과 겹치면 즉시 재발동**해 왕복 루프가 된다
    //   (발판을 문간으로 옮기면서 복귀 좌표와 겹칠 여지가 커졌다). 로드 후 잠깐은 무시하고,
    //   그 뒤엔 '한 번 벗어났다 다시 들어올 때'만 발동한다(OnTriggerEnter2D 특성 그대로).
    const float ArmDelay = 0.6f;
    float _armedAt;

    void OnEnable() => _armedAt = Time.time + ArmDelay;

    void Awake()
    {
        // 베이크된 씬의 넓은 트리거(예: 2.5x1)도 런타임에 문 크기로 강제 → 재베이크 없이 교정.
        var box = GetComponent<BoxCollider2D>();
        if (box != null && triggerSize.x > 0f && triggerSize.y > 0f)
            box.size = triggerSize;
    }

    public string TargetScene => targetScene;
    public string SpawnPointId => spawnPointId;
    public bool IsExit => isExit;

    /// <summary>빌더/런타임에서 한 번에 설정(트리거 크기는 기본값 유지).</summary>
    public void Configure(string scene, string spawnId, bool exit)
    {
        targetScene = scene;
        spawnPointId = spawnId;
        isExit = exit;
    }

    /// <summary>트리거 크기까지 함께 지정. 2026-07-11: 빌더가 BoxCollider2D.size를 넓게 잡아도
    /// Awake가 triggerSize(기본 1.3×1.0)로 **덮어써서 조용히 좁아지던** 문제 때문에 추가.
    /// 내부 씬 출구처럼 "문 폭 전체가 발판"이어야 하는 곳은 이걸 써야 옆으로 새지 않는다.</summary>
    public void Configure(string scene, string spawnId, bool exit, Vector2 size)
    {
        Configure(scene, spawnId, exit);
        if (size.x > 0f && size.y > 0f) triggerSize = size;
    }

    [Header("── 발동 방식 ──")]
    [Tooltip("true면 **E 상호작용**으로만 들어간다(2026-07-11 사용자: \"모든 문은 상호작용해야 내부로 들어가도록\").\n" +
             "밟기만 해도 넘어가면 ① 길 가다 실수로 들어가고 ② 잠금(열쇠·비밀번호) 문과 규칙이 어긋난다.\n" +
             "문은 상호작용, 실내 바닥 발판 같은 것만 false로.")]
    [SerializeField] bool requireInteract = true;

    /// <summary>E 상호작용 진입점 — `InteractableObject`(Door)가 호출.</summary>
    public void Interact(GameObject playerGO)
    {
        var col = playerGO != null ? playerGO.GetComponent<Collider2D>() : null;
        Fire(playerGO, col);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (requireInteract) return;        // 문은 밟는 게 아니라 여는 것
        if (!other.CompareTag("Player")) return;
        Fire(other.gameObject, other);
    }

    void Fire(GameObject playerGO, Collider2D playerCol)
    {
        if (_fired) return;
        if (Time.time < _armedAt) return;   // 로드 직후 스폰이 발판 위여도 즉시 되돌아가지 않게
        if (string.IsNullOrEmpty(targetScene)) return;
        if (playerGO == null) return;

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

        // 2026-07-11: **진입 시 외부 좌표 기억** — 여러 건물이 공용 내부(Int_Generic) 하나를
        //   돌려 쓰므로, 나올 때 고정 스폰을 쓰면 엉뚱한 건물 앞에 나온다.
        //   출구가 spawnPointId="__back__"이면 BuildingReturn이 이 자리로 되돌린다.
        //   2026-07-11: 발판이 **문간**으로 옮겨져서, 트리거 좌표를 그대로 기억하면
        //   나올 때 발판 한가운데에 서게 되고 → 곧바로 재발동해 왕복 루프가 된다.
        //   그래서 '플레이어가 들어온 방향'으로 트리거 밖까지 밀어낸 지점을 기억한다.
        if (!isExit)
            BuildingReturn.Remember(gameObject.scene.name, ReturnPointFor(playerCol, playerGO));

        SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
    }

    /// <summary>이 트리거 '밖'의 복귀 지점 — 플레이어가 들어온 방향으로 트리거 반경 + 여유만큼 밀어낸 자리.</summary>
    Vector3 ReturnPointFor(Collider2D playerCol, GameObject playerGO)
    {
        Vector2 here = Plan3D.ToPlan(transform.position);
        Transform pt = playerCol != null ? playerCol.transform : playerGO.transform;
        Vector2 away = Plan3D.ToPlan(pt.position) - here;
        if (away.sqrMagnitude < 0.0001f) away = Vector2.down;   // 정확히 겹쳤으면 남쪽(관례상 바깥)
        float clear = Mathf.Max(triggerSize.x, triggerSize.y) * 0.5f + 0.7f;
        return here + away.normalized * clear;
    }

    /// <summary>빌더가 발동 방식을 지정. 문=상호작용(true), 실내 바닥 발판=밟기(false).</summary>
    public void SetRequireInteract(bool v) => requireInteract = v;

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
