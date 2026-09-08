using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 플레이어의 상호작용 감지 + 프롬프트 UI.
/// Player 오브젝트에 부착.
/// 가장 가까운 InteractableObject를 탐지하고 E키로 실행.
/// </summary>
public class InteractionSystem : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] KeyCode interactKey = KeyCode.E;

    /// <summary>상호작용 입력 잠금 — UI가 닫히는 프레임의 같은 입력으로 곧바로 재발동하는 것을 막는다.</summary>
    const float InteractLock = 0.22f;
    float _interactLockUntil;
    [SerializeField] float maxDetectRadius = 4f;

    [Header("Prompt Style")]
    [SerializeField] int fontSize = 16;
    [SerializeField] Color textColor = new Color(1f, 0.9f, 0.4f);
    [SerializeField] Color bgColor = new Color(0, 0, 0, 0.75f);

    InteractableObject currentTarget;
    Health playerHealth;

    // UI
    GUIStyle promptStyle;
    Texture2D bgTex;
    Camera mainCam;

    void Awake()
    {
        playerHealth = GetComponent<Health>();
    }

    void Start()
    {
        mainCam = Camera.main;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCam = Camera.main;
    }

    void Update()
    {
        // 사망 시 상호작용 불가
        if (playerHealth != null && playerHealth.IsDead) return;

        // UI가 열려있으면 상호작용·프롬프트 차단 (타겟·하이라이트 해제 → OnGUI 프롬프트도 사라짐)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen())
        {
            if (currentTarget != null)
            {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
            // ★ 2026-07-29 (사용자: "E 눌러서 UI 작동 중인데 또 E 누르면 또 UI가 뜬다").
            //   UI를 E로 닫으면 **그 UI의 Update가 먼저 돌아** 닫히고, 뒤늦게 이 Update가
            //   같은 프레임의 GetKeyDown을 보고 곧바로 다시 연다(스크립트 실행 순서 문제).
            //   UI가 열려 있는 동안 계속 잠가 두면, 닫힌 직후의 그 입력은 이미 만료된다.
            _interactLockUntil = Time.unscaledTime + InteractLock;
            return;
        }

        // 채널(탐색·치료 등) 진행 중에도 잠근다 — 안 그러면 같은 탐색이 겹쳐 시작된다.
        if (UseActionManager.Instance != null && UseActionManager.Instance.IsBusy)
        {
            _interactLockUntil = Time.unscaledTime + InteractLock;
            return;
        }

        // 감지는 항상 수행 (UI 표시용)
        UpdateDetection();

        // 전투/구르기 중엔 상호작용 입력 차단 (감지는 위에서 수행됨)
        if (TopDownPlayer.Instance != null && TopDownPlayer.Instance.CurrentState != TopDownPlayer.CombatState.Idle)
            return;

        // E키 입력
        if (currentTarget != null && Time.unscaledTime >= _interactLockUntil
            && GameInput.GetKeyDown(interactKey))
        {
            _interactLockUntil = Time.unscaledTime + InteractLock;   // 연타·같은 프레임 재발동 차단
            currentTarget.Interact(gameObject);

            // 일회용 오브���트가 비활성화되었으면 타겟 해제
            if (!currentTarget.gameObject.activeInHierarchy || !currentTarget.CanInteract)
            {
                currentTarget.SetHighlight(false);
                currentTarget = null;
            }
        }
    }

    void UpdateDetection()
    {
        InteractableObject closest = null;
        float closestDist = float.MaxValue;

        Vector3 playerPos = transform.position;

        // 정적 리스트에서 탐색 (FindObjectsOfType 대신)
        for (int i = InteractableObject.All.Count - 1; i >= 0; i--)
        {
            var obj = InteractableObject.All[i];
            if (obj == null) continue;
            if (!obj.CanInteract) continue;

            // XY 평면 거리 (top-down 2D)
            Vector2 diff = Plan3D.ToPlan(obj.transform.position) - Plan3D.ToPlan(playerPos);
            float dist = diff.magnitude;

            // 히스테리시스: 현재 타겟은 이탈 범위(1.3배)로, 새 타겟은 진입 범위로 판정
            float range = Mathf.Min(obj.InteractRange, maxDetectRadius);
            float effectiveRange = (obj == currentTarget) ? range * 1.3f : range;
            if (dist > effectiveRange) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = obj;
            }
        }

        // 타겟 변경 시 하이라이트 갱신
        if (closest != currentTarget)
        {
            if (currentTarget != null)
                currentTarget.SetHighlight(false);

            currentTarget = closest;

            if (currentTarget != null)
                currentTarget.SetHighlight(true);
        }
    }

    #region 프롬프트 UI (OnGUI)

    void OnGUI()
    {
        if (currentTarget == null || mainCam == null) return;
        // UI 열려있으면 프롬프트 숨김 (인벤 등 위로 뚫고 나오는 것 방지)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        InitStyles();

        // 오브젝트 머리 위 → 화면 좌표
        Vector3 worldPos = currentTarget.transform.position + Vector3.up * 1.2f;
        Vector3 screenPos = mainCam.WorldToScreenPoint(worldPos);

        if (screenPos.z <= 0) return; // 카메라 뒤쪽이면 표시 안 함

        string text = $"[E] {currentTarget.PromptText}";
        Vector2 size = promptStyle.CalcSize(new GUIContent(text));

        float x = screenPos.x - size.x * 0.5f;
        float y = Screen.height - screenPos.y - size.y * 0.5f;

        // 배경 박스
        float padX = 10f, padY = 5f;
        Rect bgRect = new Rect(x - padX, y - padY, size.x + padX * 2, size.y + padY * 2);
        GUI.DrawTexture(bgRect, bgTex);

        // 테두리 (밝은 선)
        Color prevColor = GUI.color;
        GUI.color = new Color(1f, 0.9f, 0.4f, 0.4f);
        GUI.Box(bgRect, GUIContent.none);
        GUI.color = prevColor;

        // 텍스트
        GUI.Label(new Rect(x, y, size.x, size.y), text, promptStyle);
    }

    void InitStyles()
    {
        if (promptStyle != null) return;

        bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, bgColor);
        bgTex.Apply();

        promptStyle = new GUIStyle(GUI.skin.label);
        promptStyle.fontSize = fontSize;
        promptStyle.fontStyle = FontStyle.Bold;
        promptStyle.normal.textColor = textColor;
        promptStyle.alignment = TextAnchor.MiddleCenter;
    }

    #endregion
}
