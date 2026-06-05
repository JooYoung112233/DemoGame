using UnityEngine;

/// <summary>
/// 적 머리 위 말풍선 + 몸체 은신 (demo13 가시성 실험).
///
/// - 전역 토글 <see cref="Enabled"/>가 ON이면 적 몸체 스프라이트("EnemySprite")를 숨긴다.
///   → 플레이어는 적 몸을 못 보고, "말할 때" 뜨는 말풍선으로만 존재를 감지.
/// - 말풍선은 적이 "말할 때"만 표시 — 플레이어를 "만나는"(발견) 순간에만 또렷한 대사,
///   그 외 평소엔 순찰 중 근접 혼잣말 중얼거림만.
/// - 플레이어 시야 콘 안이면 <b>대사 전문</b>, 콘 밖이면 <b>"..."</b> 만. 말하는 도중 콘 안/밖이
///   바뀌면 실시간 갱신.
///
/// 토글 핫키(B)는 DebugTestUI가 단독으로 처리(여러 적이 동시에 키를 먹지 않도록).
/// 참고: NPCQuestMarker(머리 위 마커 절차 생성). 탑다운이라 빌보드 회전 불필요.
/// EnemyController.Awake가 자동 부착하므로 프리팹/씬 수정 불필요.
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemySpeechBubble : MonoBehaviour
{
    // ── 전역 토글 / 시야 콘 판정 (게임플레이용 — 라이트 렌더와 별개) ──────────
    /// <summary>ON: 적 몸체 숨김 + 말풍선 사용. OFF: 몸체 보임 + 말풍선 끔.</summary>
    public static bool  Enabled          = true;
    /// <summary>시야 콘 반각(도). 전체 시야각 = 2배.</summary>
    public static float ConeHalfAngleDeg = 45f;
    /// <summary>시야 콘 사거리(유닛).</summary>
    public static float ConeRange        = 8f;
    /// <summary>순찰 중얼거림이 들리는 거리(이보다 멀면 조용 → 위치 노출 방지).</summary>
    public static float Earshot          = 9f;

    /// <summary>월드 좌표가 플레이어 시야 콘(각도+사거리) 안인지.</summary>
    public static bool IsInPlayerCone(Vector3 worldPos)
    {
        var p = TopDownPlayer.Instance;
        if (p == null) return false;
        Vector2 to = (Vector2)(worldPos - p.transform.position);
        float dist = to.magnitude;
        if (dist > ConeRange) return false;
        if (dist < 0.05f) return true;
        float dot = Vector2.Dot(to / dist, p.FacingDirection);
        return dot >= Mathf.Cos(ConeHalfAngleDeg * Mathf.Deg2Rad);
    }

    // ── 대사 풀 (데이터화 — 적별로 인스펙터에서 교체 가능) ────────────────────
    [Header("대사 (콘 안: 전문 / 콘 밖: ...)")]
    [Tooltip("플레이어를 '만나는' 순간(발견)에만 또렷한 대사")]
    [SerializeField] string[] encounterLines = { "거기 누구냐?!", "찾았다!", "거기 서!", "멈춰!" };
    [Tooltip("평소(순찰) 혼잣말 중얼거림")]
    [SerializeField] string[] mutterLines    = { "...", "조용하군.", "누구 없나?", "어디 갔지?", "또 허탕인가.", "쯧, 지루하군." };

    [Header("말풍선")]
    [Tooltip("머리 위 높이 오프셋")]
    [SerializeField] float heightOffset = 1.2f;
    [Tooltip("한 줄 표시 시간(초)")]
    [SerializeField] float lineDuration = 2.0f;
    [SerializeField] Color panelColor   = new Color(0.08f, 0.08f, 0.10f, 0.86f);
    [SerializeField] Color textColor    = new Color(0.95f, 0.95f, 0.92f);
    [Tooltip("글자 크기(월드 스케일)")]
    [SerializeField] float textScale    = 0.45f;

    [Header("중얼거림 (평소 = 순찰 중, 근접 시)")]
    [SerializeField] bool    mutter         = true;
    [SerializeField] Vector2 mutterInterval = new Vector2(6f, 13f);

    enum Pri { Minor, Major }   // Minor: 말하는 중이면 무시 / Major: 항상 교체

    // ── 런타임 ──────────────────────────────────────────────────────────────
    EnemyController enemy;
    SpriteRenderer  bodySprite;

    GameObject     bubbleRoot;
    SpriteRenderer panel;
    TextMesh       text, textShadow;
    MeshRenderer   textMR;

    string currentFull = "";
    float  lineTimer;
    bool   speaking;
    int    shownCone = -1;   // -1=미설정, 0=콘밖, 1=콘안 (텍스트 갱신 판정)

    EnemyController.State lastState;
    float mutterTimer;

    const int SORT = 130;    // HP/그로기 바(100/101) 위, 화면 HUD 캔버스 아래

    void Start()
    {
        enemy      = GetComponent<EnemyController>();
        bodySprite = FindBodySprite();
        lastState  = enemy != null ? enemy.CurrentState : EnemyController.State.Patrol;
        BuildBubble();
        SetVisible(false);
        ResetMutterTimer();
    }

    /// <summary>몸체 스프라이트 찾기 (빌더 컨벤션상 "EnemySprite"). 바/말풍선은 제외.</summary>
    SpriteRenderer FindBodySprite()
    {
        var t = transform.Find("EnemySprite");
        if (t != null) { var sr = t.GetComponent<SpriteRenderer>(); if (sr != null) return sr; }
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            string n = sr.gameObject.name;
            if (n.Contains("Bar") || n.Contains("Bubble") || n.Contains("Panel")) continue;
            return sr;
        }
        return null;
    }

    void Update()
    {
        // 몸체 은신 토글 (이 스크립트만 bodySprite.enabled를 만짐)
        if (bodySprite != null && bodySprite.enabled == Enabled)
            bodySprite.enabled = !Enabled;

        if (!Enabled)
        {
            if (speaking) StopSpeak();
            return;
        }

        DetectTriggers();

        if (speaking)
        {
            lineTimer -= Time.deltaTime;
            if (lineTimer <= 0f) { StopSpeak(); return; }
            RefreshConeText();
        }
    }

    void DetectTriggers()
    {
        if (enemy == null) return;

        var s = enemy.CurrentState;

        // 플레이어를 "만나는" 순간(순찰 → 추격)에만 또렷한 대사.
        if (s != lastState)
        {
            if (s == EnemyController.State.Chase && lastState == EnemyController.State.Patrol)
                Speak(Pick(encounterLines), Pri.Major);
            lastState = s;
        }

        // 그 외 평소(순찰)엔 가끔 혼잣말 중얼거림 (멀면 침묵 → 위치 노출 방지).
        if (mutter && s == EnemyController.State.Patrol && !speaking)
        {
            mutterTimer -= Time.deltaTime;
            if (mutterTimer <= 0f)
            {
                if (WithinEarshot()) Speak(Pick(mutterLines), Pri.Minor);
                ResetMutterTimer();
            }
        }
    }

    void Speak(string line, Pri pri)
    {
        if (string.IsNullOrEmpty(line)) return;
        if (pri == Pri.Minor && speaking) return;   // 사소한 발화는 말하는 중이면 무시
        currentFull = line;
        lineTimer   = lineDuration;
        speaking    = true;
        shownCone   = -1;                            // 강제 갱신
        RefreshConeText();
        SetVisible(true);
    }

    void StopSpeak()
    {
        speaking = false;
        SetVisible(false);
    }

    /// <summary>콘 안=대사 전문 / 콘 밖="..." — 상태가 바뀔 때만 텍스트 교체.</summary>
    void RefreshConeText()
    {
        int inCone = IsInPlayerCone(transform.position + Vector3.up * heightOffset) ? 1 : 0;
        if (inCone == shownCone) return;
        shownCone = inCone;
        string shown = inCone == 1 ? currentFull : "...";
        if (text != null)       text.text       = shown;
        if (textShadow != null) textShadow.text = shown;
        ResizePanel(shown);
    }

    bool WithinEarshot()
    {
        var p = TopDownPlayer.Instance;
        return p != null && Vector2.Distance(transform.position, p.transform.position) <= Earshot;
    }

    void ResetMutterTimer() => mutterTimer = Random.Range(mutterInterval.x, mutterInterval.y);

    static string Pick(string[] pool)
        => (pool == null || pool.Length == 0) ? null : pool[Random.Range(0, pool.Length)];

    // ── 비주얼 ───────────────────────────────────────────────────────────────
    void SetVisible(bool v) { if (bubbleRoot != null) bubbleRoot.SetActive(v); }

    void BuildBubble()
    {
        bubbleRoot = new GameObject("SpeechBubble");
        bubbleRoot.transform.SetParent(transform, false);
        bubbleRoot.transform.localPosition = new Vector3(0, heightOffset, 0);

        // 패널은 조명 영향 안 받게 Unlit (어둠 속에서도 읽힘). 없으면 패널 생략.
        Material unlit = null;
        var sh = Shader.Find("Sprites/Default");
        if (sh != null) unlit = new Material(sh);

        if (unlit != null && PlaceholderSprite.Square != null)
        {
            panel = MakeSprite("Panel", PlaceholderSprite.Square, panelColor, SORT, unlit);

            // 꼬리 (아래쪽 작은 마름모)
            var tail = MakeSprite("Tail", PlaceholderSprite.Square, panelColor, SORT, unlit);
            tail.transform.localPosition = new Vector3(0f, -0.16f, 0f);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            tail.transform.localScale    = new Vector3(0.16f, 0.16f, 1f);
        }

        // 텍스트(그림자 + 본문). 한글은 프로젝트 표준 동적폰트.
        textShadow = MakeText("TextShadow", new Color(0f, 0f, 0f, 0.7f), SORT + 1);
        textShadow.transform.localPosition = new Vector3(0.02f, -0.02f, 0f);
        text       = MakeText("Text", textColor, SORT + 2);
        textMR     = text.GetComponent<MeshRenderer>();
    }

    SpriteRenderer MakeSprite(string name, Sprite sprite, Color color, int order, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(bubbleRoot.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sprite;
        sr.color        = color;
        sr.sortingOrder = order;
        if (mat != null) sr.sharedMaterial = mat;
        return sr;
    }

    TextMesh MakeText(string name, Color color, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(bubbleRoot.transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localScale    = Vector3.one * textScale;

        var tm = go.AddComponent<TextMesh>();
        tm.fontSize      = 64;
        tm.characterSize = 0.1f;
        tm.anchor        = TextAnchor.MiddleCenter;
        tm.alignment     = TextAlignment.Center;
        tm.color         = color;
        tm.fontStyle     = FontStyle.Bold;

        // 프로젝트 전 UI가 쓰는 동적폰트(한글 OK) + 그 머티리얼.
        var f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (f != null)
        {
            tm.font = f;
            var mr0 = go.GetComponent<MeshRenderer>();
            if (mr0 != null) mr0.sharedMaterial = f.material;
        }

        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null) mr.sortingOrder = order;
        return tm;
    }

    void ResizePanel(string shown)
    {
        if (panel == null) return;
        // 글자 수 기반 추정(LateUpdate에서 실제 bounds로 보정).
        float w = Mathf.Max(0.5f, (shown != null ? shown.Length : 0) * 0.26f * textScale) + 0.18f;
        float h = 0.42f * textScale + 0.16f;
        panel.transform.localScale = new Vector3(w, h, 1f);
    }

    void LateUpdate()
    {
        // 실제 텍스트 bounds로 패널 크기 보정(정확).
        if (!speaking || panel == null || textMR == null) return;
        Vector3 sz = textMR.bounds.size;
        if (sz.x <= 0.0001f) return;
        float scale = bubbleRoot.transform.lossyScale.x;
        if (scale < 0.0001f) scale = 1f;
        panel.transform.localScale = new Vector3(sz.x / scale + 0.20f, sz.y / scale + 0.16f, 1f);
    }
}
