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
    SceneDoor3D[] sceneDoors = System.Array.Empty<SceneDoor3D>();
    BuildingEntrance[] buildingDoors = System.Array.Empty<BuildingEntrance>();
    BuildingInterior[] interiors = System.Array.Empty<BuildingInterior>();
    GUIStyle entryStyle;
    readonly RaycastHit[] visibilityHits = new RaycastHit[64];

    void Awake()
    {
        playerHealth = GetComponent<Health>();
    }

    void Start()
    {
        mainCam = Camera.main;
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshEntrances();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (bgTex != null) Destroy(bgTex);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCam = Camera.main;
        RefreshEntrances();
    }

    void RefreshEntrances()
    {
        sceneDoors = FindObjectsByType<SceneDoor3D>();
        buildingDoors = FindObjectsByType<BuildingEntrance>();
        interiors = FindObjectsByType<BuildingInterior>();
    }

    void Update()
    {
        // 사망 시 상호작용 불가
        if (playerHealth != null && playerHealth.IsDead) { currentTarget = null; return; }

        // UI가 열려있으면 상호작용·프롬프트 차단 (타겟·하이라이트 해제 → OnGUI 프롬프트도 사라짐)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen())
        {
            if (currentTarget != null)
            {
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
            currentTarget = null;
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
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy || !currentTarget.CanInteract)
            {
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

            // XZ 바닥 평면 거리. NPC 루트는 몸 중앙, 플레이어 루트는 발바닥이다.
            Vector2 diff = Plan3D.ToPlan(obj.transform.position) - Plan3D.ToPlan(playerPos);
            float dist = diff.magnitude;

            float cap = GameTuning.Instance != null ? GameTuning.Instance.interactionDistance : 1.8f;
            float range = Mathf.Min(obj.InteractRange, Mathf.Min(maxDetectRadius, cap));
            if (dist > range || !HasAccess(obj.transform)) continue;

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = obj;
            }
        }

        // 안내 문구만 사용한다. 재질 색은 원래 만화 텍스처를 유지한다.
        if (closest != currentTarget)
        {
            currentTarget = closest;

        }
    }

    /// <summary>실제 E 입력과 프롬프트가 같은 거리/벽 판정을 사용한다.</summary>
    public bool HasAccess(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy) return false;
        var eye = transform.position + Vector3.up * 1.1f;
        var point = new Vector3(target.position.x, eye.y, target.position.z);
        foreach (var interior in interiors)
        {
            if (interior == null || !interior.isActiveAndEnabled || interior.PlayerInside) continue;
            var volume = interior.GetComponent<BoxCollider>();
            if (volume != null && volume.bounds.Contains(point)) return false;
        }
        return ClearRay(eye, point, target, false) && IsVisible(target, point);
    }

    bool IsVisible(Transform target, Vector3 point)
        => mainCam == null || ClearRay(mainCam.transform.position, point, target, true);

    bool ClearRay(Vector3 from, Vector3 to, Transform target, bool cameraRay)
    {
        Vector3 delta = to - from;
        if (delta.sqrMagnitude < .001f) return true;
        int count = Physics.RaycastNonAlloc(from, delta.normalized, visibilityHits,
            delta.magnitude, ~0, QueryTriggerInteraction.Ignore);
        if (count == visibilityHits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            var hit = visibilityHits[i].collider;
            if (hit == null || hit.transform.IsChildOf(transform) || hit.transform.IsChildOf(target)) continue;
            // 지붕을 숨긴 실내에서도 이동 벽은 남는다. 카메라 가림만 숨긴 렌더러를 무시한다.
            if (cameraRay)
            {
                var renderer = hit.GetComponent<Renderer>();
                if (renderer != null && !renderer.enabled) continue;
            }
            return false;
        }
        return true;
    }

    #region 프롬프트 UI (OnGUI)

    void OnGUI()
    {
        if (mainCam == null || TitleScreen.IsShowing || HideoutController.IsActive) return;
        // UI 열려있으면 프롬프트 숨김 (인벤 등 위로 뚫고 나오는 것 방지)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return;

        InitStyles();
        DrawEntranceMarkers();
        if (currentTarget == null || !HasAccess(currentTarget.transform)) return;

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
        entryStyle = new GUIStyle(promptStyle);
        entryStyle.fontSize = Mathf.Max(14, fontSize);
        entryStyle.normal.textColor = UITheme.TextBright;
    }

    void DrawEntranceMarkers()
    {
        foreach (var d in sceneDoors)
            if(d != null && d.isActiveAndEnabled && !string.IsNullOrEmpty(d.TargetScene))
                DrawEntry(d.transform, d.TargetScene == "Hideout" ? "하이드아웃 · 진입" : "입구 · 진입");
        foreach (var d in buildingDoors)
        {
            if(d == null || !d.isActiveAndEnabled || string.IsNullOrEmpty(d.TargetScene)) continue;
            var io=d.GetComponent<InteractableObject>();
            if(io == currentTarget && io != null) continue;
            var lockState=d.GetComponent<BlockedPassage>();
            string label=lockState != null && !lockState.IsOpen ? "문 · 잠김"
                : d.IsExit ? "출구" : "입장 가능";
            DrawEntry(d.transform,label);
        }
        foreach(var io in InteractableObject.All)
        {
            if(io == null || !io.CanInteract || io == currentTarget) continue;
            if(io.Type == InteractableObject.InteractType.MapBoard)
                DrawEntry(io.transform,"출전 준비");
            else if(io.Type == InteractableObject.InteractType.ExitPoint)
                DrawEntry(io.transform,io.PromptText);
        }
    }

    void DrawEntry(Transform target, string label)
    {
        if(Plan3D.PlanDistance(transform.position,target.position)>18f) return;
        if(!HasAccess(target)) return;
        var screen=mainCam.WorldToScreenPoint(target.position+Vector3.up*.5f);
        if(screen.z<=0 || screen.x<24 || screen.x>Screen.width-24 || screen.y<64 || screen.y>Screen.height-24) return;
        var size=entryStyle.CalcSize(new GUIContent(label));
        var rect=new Rect(screen.x-size.x*.5f-12,Screen.height-screen.y-size.y*.5f-6,size.x+24,size.y+12);
        var previous=GUI.color;
        GUI.color=new Color(.10f,.10f,.085f,.94f);GUI.DrawTexture(rect,Texture2D.whiteTexture);
        GUI.color=UITheme.AccentBright;GUI.DrawTexture(new Rect(rect.x,rect.y,3,rect.height),Texture2D.whiteTexture);
        GUI.color=previous;GUI.Label(rect,label,entryStyle);
    }

    #endregion
}
