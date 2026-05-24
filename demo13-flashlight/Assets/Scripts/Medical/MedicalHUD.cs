using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 의료 상태 패널 (Canvas/uGUI).
/// Tab키로 토글. 부위별 상태 표시.
/// Player에 부착.
/// </summary>
public class MedicalHUD : MonoBehaviour
{
    [SerializeField] KeyCode toggleKey = KeyCode.Tab;

    bool isOpen;
    PlayerMedicalSystem medical;

    // uGUI
    Canvas canvas;
    GameObject panelRoot;
    Text[] partTexts;
    Text debuffText;
    Text healingText;
    Text titleText;
    Image panelBg;

    // 미니 경고 (캔버스 밖, OnGUI 대신 별도 Text)
    Text warningText;

    string[] partNames = { "머 리", "몸 통", "양 팔", "왼다리", "오른다리" };

    void Start()
    {
        medical = GetComponent<PlayerMedicalSystem>();
        if (medical == null)
            medical = FindObjectOfType<PlayerMedicalSystem>();

        BuildUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            if (panelRoot != null)
                panelRoot.SetActive(isOpen);
        }

        UpdateWarning();

        if (!isOpen || medical == null) return;
        UpdatePanel();
    }

    void BuildUI()
    {
        // Canvas
        var canvasGO = new GameObject("MedicalHUD_Canvas");
        canvasGO.transform.SetParent(transform, false);

        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 15;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        var canvasRT = canvasGO.GetComponent<RectTransform>();

        // ── 메인 패널 (좌측 중앙) ──
        panelRoot = new GameObject("Panel");
        panelRoot.transform.SetParent(canvasRT, false);

        var panelRT = panelRoot.AddComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0.5f);
        panelRT.anchorMax = new Vector2(0, 0.5f);
        panelRT.pivot = new Vector2(0, 0.5f);
        panelRT.anchoredPosition = new Vector2(20, 0);
        panelRT.sizeDelta = new Vector2(260, 260);

        panelBg = panelRoot.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.85f);

        // 제목
        titleText = CreateText(panelRoot.transform, "Title", "◈ 신체 상태",
            new Vector2(10, -10), new Vector2(240, 25), 16, new Color(0.9f, 0.8f, 0.4f));
        titleText.fontStyle = FontStyle.Bold;

        // 부위 텍스트 (5개)
        partTexts = new Text[5];
        for (int i = 0; i < 5; i++)
        {
            partTexts[i] = CreateText(panelRoot.transform, $"Part_{i}", $"{partNames[i]}: 정상",
                new Vector2(15, -45 - i * 26), new Vector2(230, 22), 14, new Color(0.4f, 1f, 0.5f));
            partTexts[i].fontStyle = FontStyle.Bold;
        }

        // 치료 진행
        healingText = CreateText(panelRoot.transform, "Healing", "",
            new Vector2(15, -180), new Vector2(230, 20), 14, new Color(0.4f, 1f, 0.8f));

        // 디버프 요약
        debuffText = CreateText(panelRoot.transform, "Debuffs", "",
            new Vector2(15, -205), new Vector2(230, 50), 12, new Color(1f, 0.6f, 0.4f));

        panelRoot.SetActive(false);

        // ── 미니 경고 (좌상단) ──
        warningText = CreateText(canvasRT, "InjuryWarning", "",
            new Vector2(20, -80), new Vector2(150, 25), 15, new Color(1f, 0.3f, 0.3f));
        warningText.fontStyle = FontStyle.Bold;
        // 앵커: 좌상단
        var warnRT = warningText.GetComponent<RectTransform>();
        warnRT.anchorMin = new Vector2(0, 1);
        warnRT.anchorMax = new Vector2(0, 1);
        warnRT.pivot = new Vector2(0, 1);
    }

    void UpdatePanel()
    {
        var parts = medical.GetAllParts();
        for (int i = 0; i < parts.Length && i < partTexts.Length; i++)
        {
            if (parts[i].IsInjured)
            {
                string summary = GetInjurySummary(parts[i]);
                partTexts[i].text = $"{partNames[i]}: {summary}";
                partTexts[i].color = GetWorstColor(parts[i]);
            }
            else
            {
                partTexts[i].text = $"{partNames[i]}: 정상";
                partTexts[i].color = new Color(0.4f, 1f, 0.5f);
            }
        }

        // 치료 진행
        if (medical.IsHealing)
            healingText.text = $"치료 중... {medical.HealProgress * 100:F0}%";
        else
            healingText.text = "";

        // 디버프
        string debuffs = "";
        if (medical.MoveSpeedMultiplier < 1f)
            debuffs += $"이동속도: {medical.MoveSpeedMultiplier * 100:F0}%\n";
        if (medical.AttackSpeedMultiplier < 1f)
            debuffs += $"공격속도: {medical.AttackSpeedMultiplier * 100:F0}%\n";
        if (medical.StaminaRegenMultiplier < 1f)
            debuffs += $"스태미너회복: {medical.StaminaRegenMultiplier * 100:F0}%";
        debuffText.text = debuffs;
    }

    void UpdateWarning()
    {
        if (warningText == null || medical == null) return;

        if (medical.HasAnyInjury && !isOpen)
        {
            float alpha = Mathf.PingPong(Time.unscaledTime * 2f, 1f) * 0.5f + 0.5f;
            warningText.color = new Color(1f, 0.3f, 0.3f, alpha);
            warningText.text = "⚠ 부상 [Tab]";
        }
        else
        {
            warningText.text = "";
        }
    }

    string GetInjurySummary(BodyPart part)
    {
        string s = "";
        if (part.HasInjury(InjuryType.Bleeding)) s += "출혈 ";
        if (part.HasInjury(InjuryType.Fracture)) s += "골절 ";
        if (part.HasInjury(InjuryType.Pain)) s += "통증 ";
        return s.TrimEnd();
    }

    Color GetWorstColor(BodyPart part)
    {
        if (part.HasInjury(InjuryType.Fracture)) return new Color(1f, 0.2f, 0.2f);
        if (part.HasInjury(InjuryType.Bleeding)) return new Color(1f, 0.5f, 0.2f);
        return new Color(1f, 1f, 0.3f);
    }

    Text CreateText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var txt = go.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = fontSize;
        txt.color = color;
        txt.text = content;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        return txt;
    }
}
