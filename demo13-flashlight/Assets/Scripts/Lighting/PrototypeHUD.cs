using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] DayNightCycle dayNight;
    [SerializeField] FlashlightController flashlight;
    [SerializeField] Health playerHealth;

    PlayerController playerCtrl;

    GUIStyle boxStyle;
    GUIStyle labelStyle;
    GUIStyle headerStyle;
    Texture2D pixel;

    void Start()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            playerCtrl = playerGO.GetComponent<PlayerController>();
            if (playerHealth == null) playerHealth = playerGO.GetComponent<Health>();
        }
        if (dayNight == null) dayNight = FindObjectOfType<DayNightCycle>();
        if (flashlight == null) flashlight = FindObjectOfType<FlashlightController>();
    }

    void OnGUI()
    {
        InitStyles();

        DrawInfoPanel();
        DrawStaminaBar();
        DrawHealthBar();
        DrawChargeBar();
    }

    void DrawInfoPanel()
    {
        float w = 300, h = 300;
        Rect panel = new Rect(10, 10, w, h);
        GUI.Box(panel, "", boxStyle);

        GUILayout.BeginArea(new Rect(20, 15, w - 20, h - 10));
        GUILayout.Label("Combat Proto", headerStyle);
        GUILayout.Space(3);

        if (dayNight != null)
        {
            string phase = dayNight.IsNight ? "NIGHT" : "DAY";
            float remain = dayNight.TimeRemaining;
            GUILayout.Label($"Phase: {phase}  |  Time: {remain:F0}s", labelStyle);
        }

        if (flashlight != null)
        {
            string fState = flashlight.IsOn ? "ON" : "OFF";
            float pct = flashlight.BatteryPercent * 100f;
            GUILayout.Label($"Flashlight: {fState}  |  Battery: {pct:F0}%", labelStyle);
        }

        if (playerCtrl != null)
        {
            GUILayout.Space(3);
            GUILayout.Label($"Combat: {playerCtrl.GetStateText()}", labelStyle);
        }

        GUILayout.Space(10);
        GUILayout.Label("Controls:", headerStyle);
        GUILayout.Label("WASD - Move", labelStyle);
        GUILayout.Label("LClick - Light Attack (combo x3)", labelStyle);
        GUILayout.Label("RClick Hold - Heavy Attack (charge)", labelStyle);
        GUILayout.Label("Space - Dodge Roll", labelStyle);
        GUILayout.Label("F - Toggle flashlight", labelStyle);
        GUILayout.Label("T - Toggle day/night", labelStyle);
        GUILayout.EndArea();
    }

    void DrawStaminaBar()
    {
        if (playerCtrl == null) return;

        float barW = 250, barH = 14;
        float x = (Screen.width - barW) * 0.5f;
        float y = Screen.height - 60;

        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, barW + 4, barH + 4), pixel);

        float pct = playerCtrl.StaminaPercent;
        Color barColor = playerCtrl.IsExhausted
            ? new Color(0.4f, 0.4f, 0.8f)
            : Color.Lerp(new Color(1f, 0.5f, 0f), new Color(0f, 1f, 0.3f), pct);
        GUI.color = barColor;
        GUI.DrawTexture(new Rect(x, y, barW * pct, barH), pixel);

        GUI.color = Color.white;
        var staminaLabel = new GUIStyle(labelStyle);
        staminaLabel.alignment = TextAnchor.MiddleCenter;
        staminaLabel.fontSize = 11;
        string stText = playerCtrl.IsExhausted ? "EXHAUSTED" : $"STA {playerCtrl.StaminaCurrent:F0}/{playerCtrl.StaminaMax:F0}";
        GUI.Label(new Rect(x, y, barW, barH), stText, staminaLabel);
    }

    void DrawHealthBar()
    {
        if (playerHealth == null) return;

        float barW = 250, barH = 14;
        float x = (Screen.width - barW) * 0.5f;
        float y = Screen.height - 80;

        GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, barW + 4, barH + 4), pixel);

        float pct = playerHealth.Percent;
        GUI.color = Color.Lerp(Color.red, Color.green, pct);
        GUI.DrawTexture(new Rect(x, y, barW * pct, barH), pixel);

        GUI.color = Color.white;
        var hpLabel = new GUIStyle(labelStyle);
        hpLabel.alignment = TextAnchor.MiddleCenter;
        hpLabel.fontSize = 11;
        GUI.Label(new Rect(x, y, barW, barH), $"HP {playerHealth.CurrentHp:F0}/{playerHealth.MaxHp:F0}", hpLabel);
    }

    void DrawChargeBar()
    {
        if (playerCtrl == null) return;
        float charge = playerCtrl.ChargePercent;
        if (charge <= 0) return;

        float barW = 120, barH = 8;
        float x = (Screen.width - barW) * 0.5f;
        float y = Screen.height * 0.5f - 60;

        GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, barW + 2, barH + 2), pixel);

        Color chargeColor = charge >= 1f
            ? new Color(1f, 0.1f, 0.1f)
            : Color.Lerp(new Color(1f, 0.8f, 0.3f), new Color(1f, 0.4f, 0f), charge);
        GUI.color = chargeColor;
        GUI.DrawTexture(new Rect(x, y, barW * charge, barH), pixel);

        GUI.color = Color.white;
    }

    void InitStyles()
    {
        if (boxStyle != null) return;

        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        boxStyle = new GUIStyle(GUI.skin.box);
        var bgTex = new Texture2D(1, 1);
        bgTex.SetPixel(0, 0, new Color(0, 0, 0, 0.75f));
        bgTex.Apply();
        boxStyle.normal.background = bgTex;
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        labelStyle.normal.textColor = Color.white;
        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
    }
}
