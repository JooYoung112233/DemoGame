using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 상호작용 가능한 오브젝트 기본 컴포넌트.
/// 씬에 배치 후 타입/프롬프트/범위 설정.
/// 정적 리스트로 관리 — FindObjectsOfType 불필요.
/// </summary>
public class InteractableObject : MonoBehaviour, IInteractable
{
    #region 정적 등록

    public static readonly List<InteractableObject> All = new List<InteractableObject>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    #endregion

    #region 열거형

    public enum InteractType
    {
        Generic,        // 범용 (이벤트만 발생)
        ExitPoint,      // 탈출구 (필드→안전가옥) / 진입구 (안전가옥→필드)
        Container,      // 상자/서랍 (루팅)
        NPC,            // NPC (대화)
        Note,           // 쪽지/문서 (읽기)
        Pickup,         // 바닥 아이템 (줍기)
        Bed,            // 침대 (휴식/저장)
        Workbench,      // 작업대 (제작)
        MapBoard,       // 지도판 (출전 선택)
    }

    #endregion

    #region 필드

    [Header("── 상호작용 기본 ──")]
    [Tooltip("오브젝트 종류. 타입에 따라 아래 세부 설정이 달라짐")]
    [SerializeField] InteractType type = InteractType.Generic;

    [Tooltip("플레이어에게 표시될 프롬프트 텍스트 (예: 조사하기, 줍기, 탈출하기)")]
    [SerializeField] string promptText = "조사하기";

    [Tooltip("플레이어가 이 거리 안에 있어야 상호작용 가능 (단위: 미터)")]
    [SerializeField] float interactRange = 1.5f;

    [Tooltip("true면 한 번만 사용 가능 (줍기, 쪽지 등). false면 반복 사용")]
    [SerializeField] bool oneShot = false;

    [Header("── 비주얼 ──")]
    [Tooltip("범위 내 진입 시 스프라이트에 적용되는 하이라이트 색")]
    [SerializeField] Color highlightColor = new Color(1f, 1f, 0.5f, 1f);

    [Header("── 탈출구/진입구 (ExitPoint 전용) ──")]
    [Tooltip("이동할 씬 이름 (Build Settings에 등록된 이름과 동일해야 함)")]
    [SerializeField] string targetScene;

    [Tooltip("도착 씬에서 플레이어가 스폰될 SpawnPoint의 ID")]
    [SerializeField] string spawnPointId;

    [Tooltip("탈출 대기 시간(초). 0이면 즉시 전환, 8이면 8초 대기 후 전환")]
    [SerializeField] float exitWaitTime = 0f;

    [Header("── 쪽지 (Note 전용) ──")]
    [Tooltip("읽을 때 표시될 쪽지 내용")]
    [SerializeField] [TextArea(3, 10)] string noteContent;

    [Header("── 줍기 (Pickup 전용) ──")]
    [Tooltip("인벤토리에 추가될 아이템 ID (ItemDatabase 기준)")]
    [SerializeField] string itemId;

    [Tooltip("줍는 수량")]
    [SerializeField] int itemCount = 1;

    bool used;
    SpriteRenderer spriteRenderer;
    Color originalColor = Color.white;
    bool isHighlighted;

    #endregion

    #region 프로퍼티 (IInteractable)

    public string PromptText => promptText;
    public bool CanInteract => !used || !oneShot;
    public float InteractRange => interactRange;
    public InteractType Type => type;
    public string TargetScene => targetScene;
    public string NoteContent => noteContent;
    public string ItemId => itemId;
    public int ItemCount => itemCount;

    /// <summary>상호작용 시 외부에서 구독 가능한 이벤트</summary>
    public event System.Action<PlayerController> OnInteracted;

    #endregion

    #region 유니티 라이프사이클

    void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    #endregion

    #region 상호작용 실행

    public void Interact(PlayerController player)
    {
        if (!CanInteract) return;

        if (oneShot) used = true;

        // 이벤트 발행
        OnInteracted?.Invoke(player);

        // 타입별 기본 처리
        switch (type)
        {
            case InteractType.ExitPoint:
                HandleExit(player);
                break;
            case InteractType.Note:
                HandleNote(player);
                break;
            case InteractType.Pickup:
                HandlePickup(player);
                break;
            case InteractType.Container:
                HandleContainer(player);
                break;
            case InteractType.MapBoard:
                HandleMapBoard(player);
                break;
            default:
                Debug.Log($"[Interact] {type}: {promptText}");
                break;
        }
    }

    void HandleExit(PlayerController player)
    {
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[ExitPoint] targetScene이 비어있음");
            return;
        }

        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogWarning("[ExitPoint] SceneTransitionManager가 씬에 없습니다.");
            return;
        }

        if (exitWaitTime > 0)
        {
            // 탈출 대기 (기획서: 8초 대기, 거리 이탈 시 취소)
            Debug.Log($"[ExitPoint] 탈출 대기 {exitWaitTime}초...");
            SceneTransitionManager.Instance.TransitionWithDelay(targetScene, spawnPointId, exitWaitTime, transform, interactRange * 2f);
        }
        else
        {
            SceneTransitionManager.Instance.TransitionTo(targetScene, spawnPointId);
        }
    }

    void HandleNote(PlayerController player)
    {
        Debug.Log($"[Note] {noteContent}");
        // TODO: 노트 UI 표시
    }

    void HandlePickup(PlayerController player)
    {
        Debug.Log($"[Pickup] {itemId} x{itemCount}");
        // TODO: 인벤토리에 추가
        if (oneShot)
            gameObject.SetActive(false);
    }

    void HandleContainer(PlayerController player)
    {
        Debug.Log($"[Container] {promptText} 열기");
        // TODO: 루팅 UI 열기
    }

    void HandleMapBoard(PlayerController player)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowMapSelect();
        }
        else
        {
            Debug.LogWarning("[MapBoard] UIManager가 없습니다.");
        }
    }

    #endregion

    #region 하이라이트

    public void SetHighlight(bool on)
    {
        isHighlighted = on;
        if (spriteRenderer == null) return;
        spriteRenderer.color = on ? highlightColor : originalColor;
    }

    #endregion

    #region 기즈모

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0, 1, 1, 0.3f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }

    #endregion
}
