using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 내비게이션 HUD — 시계 나침반(상시, 하단 중앙) + 들어올려 보는 미니맵(M 홀드).
/// 자체 Canvas 생성(GameHUD 패턴). 자동 스폰 싱글톤(DontDestroyOnLoad).
/// - 나침반: 초침이 타겟(튜토리얼>퀘스트>스토리>탈출구) 방향을 가리킴. 거리값은 비공개, 가까우면 맥동.
/// - 미니맵: M 홀드 시 발견한 구역 + 플레이어 + 탈출구 + 통로 주석을 큰 패널로.
/// - 사망 시: 초침이 거꾸로 도는 '시간역행' 암시 연출(설명 없음).
/// 설계: docs/navigation.md §1·§2
/// </summary>
public class NavigationHUD : MonoBehaviour
{
    public static NavigationHUD Instance { get; private set; }

    [Header("나침반")]
    [SerializeField] float compassSize = 84f;
    [SerializeField] KeyCode raiseMapKey = KeyCode.M;   // Tab은 인벤토리(UIManager) 사용중 → M
    [SerializeField] float nearDistance = 4f;           // 이 거리 이하 = '가까움' 맥동 피드백

    const float MapW = 760f, MapH = 520f;

    // Canvas (프리팹 베이크 시 직렬화 보존되는 영속 스켈레톤)
    [SerializeField] Canvas canvas;
    [SerializeField] RectTransform compassRoot;
    [SerializeField] RectTransform needle;
    [SerializeField] Image needleImg;
    [SerializeField] Text targetLabel;

    // Overlay (들어올려 보기) — 영속 골격만 직렬화
    [SerializeField] RectTransform overlayRoot;
    [SerializeField] RectTransform mapArea;
    // playerBlip / _mapPooled 는 RebuildMap에서 마커별로 동적 생성 → 직렬화 안 함
    RectTransform playerBlip;
    readonly List<GameObject> _mapPooled = new List<GameObject>();
    Rect _worldBounds;

    // refs / state
    Health _playerHealth;
    float _rewindTimer;   // 사망 시간역행 암시
    bool _overlayOpen;

    static Font UiFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    /// <summary>프리팹 베이크 여부 — Canvas가 직렬화돼 있으면 코드 생성 생략.</summary>
    bool IsGenerated => canvas != null;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // UIManager 자식으로 배치 → 부모(UIManager)의 DontDestroyOnLoad를 따라 영속.
        if (!IsGenerated) BuildUI();   // 폴백: 프리팹 없이 코드로 생성
        else ReapplyProceduralSprites();   // 프리팹 경로: 절차 생성 스프라이트는 직렬화 불가 → 재적용
        if (GetComponent<TownMinimapHUD>() == null) gameObject.AddComponent<TownMinimapHUD>();
    }

    /// <summary>PlaceholderSprite.Circle 같은 런타임 생성 스프라이트는 에셋이 아니라 프리팹에 못 구워진다
    /// (나침반 다이얼/핀이 사각형으로 렌더 — 전체 검수 2026-07-07). 프리팹 경로에서 다시 꽂는다(코스메틱).</summary>
    void ReapplyProceduralSprites()
    {
        if (compassRoot == null) return;
        foreach (var img in compassRoot.GetComponentsInChildren<Image>(true))
        {
            if (img == null || img.sprite != null) continue;
            if (img.gameObject.name == "Dial" || img.gameObject.name == "Pin")
                img.sprite = PlaceholderSprite.Circle;
        }
    }

    void OnDestroy()
    {
        if (_playerHealth != null) _playerHealth.OnDeath -= OnPlayerDeath;
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (_playerHealth == null) TryBindHealth();

        bool inRaid = RaidManager.Instance != null && RaidManager.Instance.IsRaidActive;
        bool show = inRaid || _rewindTimer > 0f;
        if (compassRoot != null) compassRoot.gameObject.SetActive(show);

        if (_rewindTimer > 0f) _rewindTimer -= Time.unscaledDeltaTime;

        if (!show)
        {
            if (_overlayOpen) CloseOverlay();
            return;
        }

        UpdateCompass();

        // 미니맵은 레이드 중에만(역행 연출 중엔 입력 무시)
        if (inRaid) HandleOverlayInput();
        else if (_overlayOpen) CloseOverlay();
    }

    // ── 플레이어 바인딩 ──

    void TryBindHealth()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        if (go == null) return;
        var h = go.GetComponent<Health>();
        if (h == null) return;
        _playerHealth = h;
        _playerHealth.OnDeath -= OnPlayerDeath;
        _playerHealth.OnDeath += OnPlayerDeath;
    }

    void OnPlayerDeath() => _rewindTimer = 1.3f;   // 초침 역행 암시 지속

    // ── 나침반 ──

    void UpdateCompass()
    {
        if (needle == null || needleImg == null) return;

        // 사망 시간역행 암시: 타겟 무관, 초침 역행
        if (_rewindTimer > 0f)
        {
            needleImg.enabled = true;
            needleImg.color = new Color(0.7f, 0.8f, 1f);
            needle.localScale = Vector3.one;
            needle.localRotation = Quaternion.Euler(0f, 0f, Time.unscaledTime * -540f);
            if (targetLabel != null) targetLabel.text = "…";
            return;
        }

        var player = TopDownPlayer.Instance;
        if (player == null) { HideNeedle(); return; }

        Vector2 ppos = Plan3D.ToPlan(player.transform.position);
        if (!ResolveTarget(ppos, out Vector2 tpos, out string label)) { HideNeedle(); return; }

        needleImg.enabled = true;
        Vector2 dir = tpos - ppos;
        float dist = dir.magnitude;
        // 2026-09-11 대각선 쿼터뷰 — 월드 방향을 **화면 방향**으로 바꿔 잰다(화면 위 = 카메라 전방).
        //   예전엔 +Z가 곧 화면 위라 월드 각을 그대로 썼는데, 카메라가 45° 돌자 바늘이 45° 틀어졌다.
        Vector2 screen = dir;
        var camT = CameraFollow.Instance != null ? CameraFollow.Instance.transform : null;
        if (camT != null)
        {
            Vector3 f = camT.forward; f.y = 0f;
            Vector3 r = camT.right;   r.y = 0f;
            if (f.sqrMagnitude > 1e-4f && r.sqrMagnitude > 1e-4f)
            {
                f.Normalize(); r.Normalize();
                screen = new Vector2(dir.x * r.x + dir.y * r.z, dir.x * f.x + dir.y * f.z);
            }
        }
        float ang = Mathf.Atan2(screen.y, screen.x) * Mathf.Rad2Deg;
        needle.localRotation = Quaternion.Euler(0f, 0f, ang - 90f);   // 니들 기본은 +Y(위)를 향함

        bool near = dist <= nearDistance;
        float pulse = near ? 1f + Mathf.Sin(Time.unscaledTime * 10f) * 0.12f : 1f;
        needle.localScale = new Vector3(pulse, pulse, 1f);
        needleImg.color = near ? new Color(1f, 0.85f, 0.3f) : new Color(0.9f, 0.95f, 1f);

        if (targetLabel != null) targetLabel.text = label;  // 방향만 — 거리값 비공개(맵의 몫)
    }

    void HideNeedle()
    {
        if (needleImg != null) needleImg.enabled = false;
        if (targetLabel != null) targetLabel.text = "";
    }

    /// <summary>우선순위(Tutorial>Quest>Story)로 CompassTarget을 고르고, 없으면 최근접 탈출구 폴백.</summary>
    bool ResolveTarget(Vector2 from, out Vector2 pos, out string label)
    {
        pos = Vector2.zero; label = null;

        CompassTarget best = null;
        var targets = CompassTarget.All;
        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];
            if (t == null || !t.Active) continue;
            if (best == null || t.Priority > best.Priority ||
                (t.Priority == best.Priority &&
                 (t.Position - from).sqrMagnitude < (best.Position - from).sqrMagnitude))
                best = t;
        }
        if (best != null) { pos = best.Position; label = best.Label; return true; }

        // 폴백: 가장 가까운 탈출구
        InteractableObject nearestExit = null;
        float bestSq = float.MaxValue;
        var all = InteractableObject.All;
        for (int i = 0; i < all.Count; i++)
        {
            var io = all[i];
            if (io == null || io.Type != InteractableObject.InteractType.ExitPoint) continue;
            float sq = (Plan3D.ToPlan(io.transform.position) - from).sqrMagnitude;
            if (sq < bestSq) { bestSq = sq; nearestExit = io; }
        }
        if (nearestExit != null) { pos = nearestExit.transform.position; label = "탈출구"; return true; }
        return false;
    }

    // ── 미니맵 오버레이 (M 홀드) ──

    void HandleOverlayInput()
    {
        bool held = GameInput.GetKey(raiseMapKey);
        if (held && !_overlayOpen) OpenOverlay();
        else if (!held && _overlayOpen) CloseOverlay();
        if (_overlayOpen) UpdateOverlay();
    }

    void OpenOverlay()
    {
        _overlayOpen = true;
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(true);
        RebuildMap();
    }

    void CloseOverlay()
    {
        _overlayOpen = false;
        if (overlayRoot != null) overlayRoot.gameObject.SetActive(false);
    }

    void RebuildMap()
    {
        // 기존 동적 요소 정리
        for (int i = 0; i < _mapPooled.Count; i++)
            if (_mapPooled[i] != null) Destroy(_mapPooled[i]);
        _mapPooled.Clear();
        playerBlip = null;

        var m = RaidMapManager.InstanceIfExists;
        var zones = m != null ? m.Zones : null;

        ComputeWorldBounds(zones);

        // 발견한 구역만 렌더(fog)
        if (zones != null)
        {
            for (int i = 0; i < zones.Count; i++)
            {
                var z = zones[i];
                if (!z.discovered) continue;

                var rect = MakeMapImage("Zone_" + z.id, null, new Color(0.25f, 0.45f, 0.6f, 0.35f));
                rect.anchoredPosition = WorldToMap(z.Center);
                rect.sizeDelta = WorldSizeToMap(z.bounds.size);

                var lbl = MakeMapText("ZoneLbl_" + z.id, z.displayName, 13, new Color(0.85f, 0.92f, 1f));
                lbl.rectTransform.anchoredPosition = WorldToMap(z.Center);
            }

            // 탈출구 — 발견한 구역 안에 있는 것만
            var all = InteractableObject.All;
            for (int i = 0; i < all.Count; i++)
            {
                var io = all[i];
                if (io == null || io.Type != InteractableObject.InteractType.ExitPoint) continue;
                Vector2 wp = Plan3D.ToPlan(io.transform.position);
                if (!InsideDiscovered(zones, wp)) continue;

                var ex = MakeMapImage("Exit", PlaceholderSprite.Square, new Color(0.3f, 1f, 0.45f));
                ex.sizeDelta = new Vector2(14, 14);
                ex.anchoredPosition = WorldToMap(wp);
            }
        }

        // 통로 주석 — '알게 된' 것만
        var passages = PassageMarker.All;
        for (int i = 0; i < passages.Count; i++)
        {
            var pm = passages[i];
            if (pm == null || !pm.Known) continue;
            if (pm.State == PassageState.Open) continue;

            var mark = MakeMapImage("Passage", PlaceholderSprite.Square, PassageColor(pm.State));
            mark.sizeDelta = new Vector2(12, 12);
            mark.localRotation = Quaternion.Euler(0, 0, 45);   // 마름모
            mark.anchoredPosition = WorldToMap(pm.Position);
        }

        // 플레이어 블립(원)
        playerBlip = MakeMapImage("Player", PlaceholderSprite.Circle, new Color(1f, 0.85f, 0.2f));
        playerBlip.sizeDelta = new Vector2(14, 14);
    }

    void UpdateOverlay()
    {
        if (playerBlip == null) return;
        var p = TopDownPlayer.Instance;
        if (p == null) return;
        playerBlip.anchoredPosition = WorldToMap(Plan3D.ToPlan(p.transform.position));
    }

    static Color PassageColor(PassageState s)
    {
        switch (s)
        {
            case PassageState.Locked:  return new Color(1f, 0.3f, 0.3f);   // 빨강 자물쇠
            case PassageState.Blocked: return new Color(1f, 0.6f, 0.15f);  // 주황 잔해
            case PassageState.OneWay:  return new Color(0.4f, 0.8f, 1f);   // 하늘 일방통행
            default:                   return Color.white;
        }
    }

    bool InsideDiscovered(IReadOnlyList<MapZone> zones, Vector2 wp)
    {
        for (int i = 0; i < zones.Count; i++)
            if (zones[i].discovered && zones[i].Contains(wp)) return true;
        return false;
    }

    void ComputeWorldBounds(IReadOnlyList<MapZone> zones)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        int count = 0;
        if (zones != null)
            for (int i = 0; i < zones.Count; i++)
            {
                var b = zones[i].bounds;
                minX = Mathf.Min(minX, b.xMin); minY = Mathf.Min(minY, b.yMin);
                maxX = Mathf.Max(maxX, b.xMax); maxY = Mathf.Max(maxY, b.yMax);
                count++;
            }

        if (count == 0)
        {
            // 존이 없으면 플레이어 중심 기본 박스
            Vector2 c = TopDownPlayer.Instance != null ? (Vector2)TopDownPlayer.Instance.transform.position : Vector2.zero;
            _worldBounds = new Rect(c.x - 15f, c.y - 15f, 30f, 30f);
            return;
        }

        // 약간의 패딩
        float padX = (maxX - minX) * 0.08f + 1f;
        float padY = (maxY - minY) * 0.08f + 1f;
        _worldBounds = new Rect(minX - padX, minY - padY, (maxX - minX) + padX * 2f, (maxY - minY) + padY * 2f);
    }

    float MapScale()
    {
        float bw = Mathf.Max(_worldBounds.width, 0.01f);
        float bh = Mathf.Max(_worldBounds.height, 0.01f);
        return Mathf.Min(MapW / bw, MapH / bh) * 0.92f;
    }

    Vector2 WorldToMap(Vector2 w) => (w - _worldBounds.center) * MapScale();
    Vector2 WorldSizeToMap(Vector2 s) => s * MapScale();

    // ── UI 빌드 ──

    void BuildUI()
    {
        var canvasGO = new GameObject("Nav_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        var canvasRT = canvasGO.GetComponent<RectTransform>();

        BuildCompass(canvasRT);
        BuildOverlay(canvasRT);
    }

#if UNITY_EDITOR
    /// <summary>에디터 베이크 전용 — BuildUI를 1회 실행해 프리팹화할 계층을 만든다.</summary>
    public void EditorBake()
    {
        if (IsGenerated) return;
        BuildUI();
    }
#endif

    void BuildCompass(RectTransform canvasRT)
    {
        // 하단 중앙 루트 (빠른 슬롯 위 — 위치는 레이아웃 시 조정)
        var rootGO = new GameObject("Compass");
        rootGO.transform.SetParent(canvasRT, false);
        compassRoot = rootGO.AddComponent<RectTransform>();
        compassRoot.anchorMin = new Vector2(0.5f, 0f);
        compassRoot.anchorMax = new Vector2(0.5f, 0f);
        compassRoot.pivot = new Vector2(0.5f, 0f);
        compassRoot.anchoredPosition = new Vector2(0f, 96f);
        compassRoot.sizeDelta = new Vector2(compassSize, compassSize + 24f);

        // 다이얼 (원형 배경)
        var dial = MakeImage(compassRoot, "Dial", PlaceholderSprite.Circle, new Color(0.06f, 0.07f, 0.1f, 0.7f));
        dial.anchorMin = dial.anchorMax = new Vector2(0.5f, 1f);
        dial.pivot = new Vector2(0.5f, 1f);
        dial.anchoredPosition = Vector2.zero;
        dial.sizeDelta = new Vector2(compassSize, compassSize);

        // '위(화면 상단)' 기준 틱
        var tick = MakeImage(dial, "NorthTick", PlaceholderSprite.Square, new Color(0.6f, 0.7f, 0.8f, 0.8f));
        tick.anchorMin = tick.anchorMax = new Vector2(0.5f, 1f);
        tick.pivot = new Vector2(0.5f, 1f);
        tick.anchoredPosition = new Vector2(0f, -3f);
        tick.sizeDelta = new Vector2(3f, 7f);

        // 초침(니들) — 다이얼 중심에서 위로, 회전으로 방향 지시
        var center = new GameObject("NeedlePivot");
        center.transform.SetParent(dial, false);
        needle = center.AddComponent<RectTransform>();
        needle.anchorMin = needle.anchorMax = new Vector2(0.5f, 0.5f);
        needle.pivot = new Vector2(0.5f, 0f);  // 밑동 기준 회전(시계 바늘처럼)
        needle.anchoredPosition = Vector2.zero;
        needle.sizeDelta = new Vector2(4f, compassSize * 0.42f);

        needleImg = center.AddComponent<Image>();
        needleImg.sprite = PlaceholderSprite.Square;
        needleImg.color = new Color(0.9f, 0.95f, 1f);

        // 중심 핀
        var pin = MakeImage(dial, "Pin", PlaceholderSprite.Circle, new Color(0.8f, 0.85f, 0.95f));
        pin.anchorMin = pin.anchorMax = new Vector2(0.5f, 0.5f);
        pin.pivot = new Vector2(0.5f, 0.5f);
        pin.anchoredPosition = Vector2.zero;
        pin.sizeDelta = new Vector2(8f, 8f);

        // 타겟 라벨 (다이얼 아래)
        targetLabel = MakeText(compassRoot, "TargetLabel", "", 13, TextAnchor.UpperCenter);
        var lblRT = targetLabel.rectTransform;
        lblRT.anchorMin = new Vector2(0.5f, 0f);
        lblRT.anchorMax = new Vector2(0.5f, 0f);
        lblRT.pivot = new Vector2(0.5f, 0f);
        lblRT.anchoredPosition = new Vector2(0f, 0f);
        lblRT.sizeDelta = new Vector2(220f, 20f);
        targetLabel.color = new Color(0.85f, 0.9f, 1f);

        compassRoot.gameObject.SetActive(false);
    }

    void BuildOverlay(RectTransform canvasRT)
    {
        var rootGO = new GameObject("MapOverlay");
        rootGO.transform.SetParent(canvasRT, false);
        overlayRoot = rootGO.AddComponent<RectTransform>();
        overlayRoot.anchorMin = Vector2.zero;
        overlayRoot.anchorMax = Vector2.one;
        overlayRoot.offsetMin = Vector2.zero;
        overlayRoot.offsetMax = Vector2.zero;

        var dim = rootGO.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.55f);   // 월드 디밍

        // 중앙 맵 패널
        var panel = MakeImage(overlayRoot, "MapPanel", null, new Color(0.04f, 0.05f, 0.08f, 0.92f));
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(MapW + 40f, MapH + 96f);

        // 제목
        var title = MakeText(panel, "Title", "지 도", 22, TextAnchor.UpperCenter);
        title.fontStyle = FontStyle.Bold;
        var tRT = title.rectTransform;
        tRT.anchorMin = new Vector2(0.5f, 1f); tRT.anchorMax = new Vector2(0.5f, 1f);
        tRT.pivot = new Vector2(0.5f, 1f);
        tRT.anchoredPosition = new Vector2(0f, -12f);
        tRT.sizeDelta = new Vector2(MapW, 32f);

        // 맵 영역
        var areaGO = new GameObject("MapArea");
        areaGO.transform.SetParent(panel, false);
        mapArea = areaGO.AddComponent<RectTransform>();
        mapArea.anchorMin = mapArea.anchorMax = new Vector2(0.5f, 0.5f);
        mapArea.pivot = new Vector2(0.5f, 0.5f);
        mapArea.anchoredPosition = new Vector2(0f, -8f);
        mapArea.sizeDelta = new Vector2(MapW, MapH);
        var areaBg = areaGO.AddComponent<Image>();
        areaBg.color = new Color(0.02f, 0.03f, 0.05f, 0.6f);

        // 힌트
        var hint = MakeText(panel, "Hint", "M 키를 떼면 닫힘", 13, TextAnchor.LowerCenter);
        var hRT = hint.rectTransform;
        hRT.anchorMin = new Vector2(0.5f, 0f); hRT.anchorMax = new Vector2(0.5f, 0f);
        hRT.pivot = new Vector2(0.5f, 0f);
        hRT.anchoredPosition = new Vector2(0f, 12f);
        hRT.sizeDelta = new Vector2(MapW, 20f);
        hint.color = new Color(0.6f, 0.65f, 0.75f);

        overlayRoot.gameObject.SetActive(false);
    }

    // ── 헬퍼 ──

    RectTransform MakeImage(RectTransform parent, string name, Sprite sprite, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        if (sprite != null) img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;   // 나침반/맵 그래픽은 클릭(조준) 가로채지 않음
        return rt;
    }

    Text MakeText(RectTransform parent, string name, string content, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var txt = go.AddComponent<Text>();
        txt.font = UiFont;
        txt.fontSize = fontSize;
        txt.text = content;
        txt.alignment = align;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        txt.supportRichText = true;
        txt.raycastTarget = false;
        var sh = go.AddComponent<Shadow>();
        sh.effectColor = new Color(0, 0, 0, 0.8f);
        sh.effectDistance = new Vector2(1, -1);
        return txt;
    }

    // 맵 영역 자식용(부모 = mapArea, 중앙 앵커)
    RectTransform MakeMapImage(string name, Sprite sprite, Color color)
    {
        var rt = MakeImage(mapArea, name, sprite, color);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        _mapPooled.Add(rt.gameObject);
        return rt;
    }

    Text MakeMapText(string name, string content, int fontSize, Color color)
    {
        var txt = MakeText(mapArea, name, content, fontSize, TextAnchor.MiddleCenter);
        var rt = txt.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(120f, 18f);
        txt.color = color;
        _mapPooled.Add(txt.gameObject);
        return txt;
    }
}
