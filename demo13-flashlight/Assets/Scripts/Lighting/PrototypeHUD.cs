using UnityEngine;

public class PrototypeHUD : MonoBehaviour
{
    [SerializeField] DayNightCycle dayNight;
    [SerializeField] FlashlightController flashlight;

    GUIStyle boxStyle;
    GUIStyle labelStyle;
    GUIStyle headerStyle;

    void OnGUI()
    {
        InitStyles();

        float w = 280, h = 200;
        Rect panel = new Rect(10, 10, w, h);
        GUI.Box(panel, "", boxStyle);

        GUILayout.BeginArea(new Rect(20, 15, w - 20, h - 10));
        GUILayout.Label("Flashlight Prototype (3D)", headerStyle);
        GUILayout.Space(5);

        if (dayNight != null)
        {
            string phase = dayNight.IsNight ? "NIGHT" : "DAY";
            float remain = dayNight.TimeRemaining;
            GUILayout.Label($"Phase: {phase}  |  Time: {remain:F0}s", labelStyle);
        }
        GUILayout.Space(3);

        if (flashlight != null)
        {
            string state = flashlight.IsOn ? "ON" : "OFF";
            float pct = flashlight.BatteryPercent * 100f;
            GUILayout.Label($"Flashlight: {state}  |  Battery: {pct:F0}%", labelStyle);

            Rect barBg = GUILayoutUtility.GetRect(w - 40, 12);
            GUI.color = new Color(0.2f, 0.2f, 0.2f);
            GUI.DrawTexture(barBg, Texture2D.whiteTexture);
            GUI.color = pct > 20 ? Color.yellow : Color.red;
            barBg.width *= flashlight.BatteryPercent;
            GUI.DrawTexture(barBg, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        GUILayout.Space(10);
        GUILayout.Label("Controls:", headerStyle);
        GUILayout.Label("WASD - Move", labelStyle);
        GUILayout.Label("Mouse - Aim flashlight", labelStyle);
        GUILayout.Label("F - Toggle flashlight", labelStyle);
        GUILayout.Label("T - Toggle day/night", labelStyle);
        GUILayout.Label("Ctrl - Crouch (slow)", labelStyle);
        GUILayout.EndArea();
    }

    void InitStyles()
    {
        if (boxStyle != null) return;
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
