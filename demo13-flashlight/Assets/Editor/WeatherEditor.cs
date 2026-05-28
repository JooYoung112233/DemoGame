using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

/// <summary>
/// 날씨/라이팅 에디터 윈도우.
/// 낮/밤 라이팅, 안개, 비 설정을 시각적으로 편집.
/// </summary>
public class WeatherEditor : EditorWindow
{
    const string DATA_PATH = "Assets/Settings/WeatherData.asset";

    WeatherData data;
    SerializedObject so;
    Vector2 scrollPos;

    bool showDay = true;
    bool showNight = true;
    bool showWeather = true;
    bool showRain = true;
    bool showRainFog = false;

    [MenuItem("Tools/Dev Tools/Visual/Weather & Lighting Editor")]
    static void Open()
    {
        var win = GetWindow<WeatherEditor>("Weather & Lighting");
        win.minSize = new Vector2(400, 600);
        win.LoadOrCreate();
    }

    void OnEnable() => LoadOrCreate();
    void OnFocus() => LoadOrCreate();

    void LoadOrCreate()
    {
        data = AssetDatabase.LoadAssetAtPath<WeatherData>(DATA_PATH);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            data = ScriptableObject.CreateInstance<WeatherData>();
            AssetDatabase.CreateAsset(data, DATA_PATH);
            AssetDatabase.SaveAssets();
        }
        so = new SerializedObject(data);
    }

    void OnGUI()
    {
        if (data == null) { LoadOrCreate(); return; }
        so.Update();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        // ===== 헤더 =====
        EditorGUILayout.Space(8);
        DrawHeader("WEATHER & LIGHTING");
        EditorGUILayout.Space(6);

        // ===== 낮 =====
        showDay = EditorGUILayout.BeginFoldoutHeaderGroup(showDay, "DAY SETTINGS");
        if (showDay)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Directional Light", EditorStyles.miniLabel);
            DrawSlider("dayIntensity", "Intensity", 0, 3);
            EditorGUILayout.PropertyField(so.FindProperty("dayLightColor"), new GUIContent("Light Color"));
            EditorGUILayout.PropertyField(so.FindProperty("daySunAngle"), new GUIContent("Sun Angle"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ambient", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("dayAmbientColor"), new GUIContent("Ambient Color"));
            DrawSlider("dayAmbientIntensity", "Ambient Intensity", 0, 2);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Fog", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("dayFog"), new GUIContent("Enable Fog"));
            if (data.dayFog)
            {
                EditorGUILayout.PropertyField(so.FindProperty("dayFogColor"), new GUIContent("  Fog Color"));
                DrawSlider("dayFogDensity", "  Fog Density", 0, 0.2f);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Preview DAY in Scene", GUILayout.Height(22)))
                ApplyToScene(false);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        // ===== 밤 =====
        showNight = EditorGUILayout.BeginFoldoutHeaderGroup(showNight, "NIGHT SETTINGS");
        if (showNight)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Directional Light", EditorStyles.miniLabel);
            DrawSlider("nightIntensity", "Intensity", 0, 3);
            EditorGUILayout.PropertyField(so.FindProperty("nightLightColor"), new GUIContent("Light Color"));
            EditorGUILayout.PropertyField(so.FindProperty("nightSunAngle"), new GUIContent("Sun Angle"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ambient", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("nightAmbientColor"), new GUIContent("Ambient Color"));
            DrawSlider("nightAmbientIntensity", "Ambient Intensity", 0, 2);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Fog", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("nightFog"), new GUIContent("Enable Fog"));
            if (data.nightFog)
            {
                EditorGUILayout.PropertyField(so.FindProperty("nightFogColor"), new GUIContent("  Fog Color"));
                DrawSlider("nightFogDensity", "  Fog Density", 0, 0.2f);
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Preview NIGHT in Scene", GUILayout.Height(22)))
                ApplyToScene(true);

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        // ===== 날씨 타입 =====
        showWeather = EditorGUILayout.BeginFoldoutHeaderGroup(showWeather, "WEATHER TYPE");
        if (showWeather)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(so.FindProperty("weather"), new GUIContent("Weather"));

            // 현재 날씨 상태 표시
            string weatherIcon = "";
            switch (data.weather)
            {
                case WeatherData.WeatherType.Clear: weatherIcon = "Clear"; break;
                case WeatherData.WeatherType.Rain: weatherIcon = "Rain"; break;
                case WeatherData.WeatherType.HeavyRain: weatherIcon = "Heavy Rain"; break;
                case WeatherData.WeatherType.Fog: weatherIcon = "Fog"; break;
            }
            DrawInfoBox($"Current: {weatherIcon}  |  Raining: {data.IsRaining}");

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(6);

        // ===== 비 상세 =====
        showRain = EditorGUILayout.BeginFoldoutHeaderGroup(showRain, "RAIN SETTINGS");
        if (showRain)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Visual", EditorStyles.miniLabel);
            DrawSlider("rainIntensity", "Rain Intensity", 0, 1);
            EditorGUILayout.PropertyField(so.FindProperty("rainTint"), new GUIContent("Rain Tint"));
            DrawSlider("rainParticleRate", "Particle Rate", 50, 1000);
            DrawSlider("rainSpeed", "Drop Speed", 3, 25);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Wind", EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(so.FindProperty("rainWindDirection"), new GUIContent("Wind Direction"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Ground", EditorStyles.miniLabel);
            DrawSlider("groundWetness", "Wetness", 0, 1);
            DrawSlider("rippleSpeed", "Ripple Speed", 0, 1);
            DrawSlider("rippleDensity", "Ripple Density", 0, 1);

            // 미리보기 바
            EditorGUILayout.Space(4);
            DrawWetnessPreview();

            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(4);

        // ===== 비 안개 =====
        showRainFog = EditorGUILayout.BeginFoldoutHeaderGroup(showRainFog, "RAIN FOG OVERRIDE");
        if (showRainFog)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(so.FindProperty("rainOverrideFog"), new GUIContent("Override Fog"));
            if (data.rainOverrideFog)
            {
                EditorGUILayout.PropertyField(so.FindProperty("rainFogColor"), new GUIContent("Rain Fog Color"));
                DrawSlider("rainFogDensity", "Rain Fog Density", 0, 0.2f);
            }
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();

        EditorGUILayout.Space(12);

        // ===== 하단 버튼 =====
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Apply to Scene", GUILayout.Height(28)))
        {
            so.ApplyModifiedProperties();
            ApplyWeatherToScene();
        }
        if (GUILayout.Button("Reset Defaults", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Reset", "기본값으로 초기화?", "Yes", "Cancel"))
                ResetDefaults();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        // 연결 상태
        DrawConnectionStatus();

        EditorGUILayout.EndScrollView();

        so.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(data);
    }

    // ================================================================
    //  씬 적용
    // ================================================================
    void ApplyToScene(bool asNight)
    {
        so.ApplyModifiedProperties();

        var dirLight = Object.FindFirstObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.intensity = asNight ? data.nightIntensity : data.dayIntensity;
            dirLight.color = asNight ? data.nightLightColor : data.dayLightColor;
            dirLight.transform.rotation = Quaternion.Euler(
                asNight ? data.nightSunAngle : data.daySunAngle);
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = asNight ? data.nightAmbientColor : data.dayAmbientColor;

        bool useFog = asNight ? data.nightFog : data.dayFog;
        RenderSettings.fog = useFog;
        if (useFog)
        {
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = asNight ? data.nightFogColor : data.dayFogColor;
            RenderSettings.fogDensity = asNight ? data.nightFogDensity : data.dayFogDensity;
        }

        Debug.Log($"[Weather] {(asNight ? "Night" : "Day")} 라이팅 씬에 적용!");
        SceneView.RepaintAll();
    }

    void ApplyWeatherToScene()
    {
        // DayNightCycle에 WeatherData 연결
        var dnc = Object.FindFirstObjectByType<DayNightCycle>();
        if (dnc != null)
        {
            var dncSO = new SerializedObject(dnc);
            dncSO.FindProperty("weatherData").objectReferenceValue = data;
            dncSO.ApplyModifiedProperties();
        }

        // RainController에 WeatherData 연결
        var rain = Object.FindFirstObjectByType<RainController>();
        if (rain != null)
        {
            var rainSO = new SerializedObject(rain);
            rainSO.FindProperty("weatherData").objectReferenceValue = data;
            rainSO.ApplyModifiedProperties();
        }

        // 현재 낮/밤에 맞게 라이팅 적용
        bool isNight = dnc != null && dnc.IsNight;
        ApplyToScene(isNight);

        Debug.Log("[Weather] 씬에 날씨 데이터 적용 완료!");
    }

    void ResetDefaults()
    {
        data.dayIntensity = 1f;
        data.dayLightColor = new Color(1f, 0.95f, 0.9f);
        data.daySunAngle = new Vector3(50, -30, 0);
        data.dayAmbientColor = new Color(0.35f, 0.35f, 0.4f);
        data.dayFog = false;
        data.nightIntensity = 0f;
        data.nightLightColor = Color.black;
        data.nightAmbientColor = new Color(0.04f, 0.04f, 0.06f);
        data.nightFog = true;
        data.nightFogColor = new Color(0.02f, 0.02f, 0.03f);
        data.nightFogDensity = 0.04f;
        data.weather = WeatherData.WeatherType.Clear;
        data.rainIntensity = 0.6f;
        data.groundWetness = 0.7f;
        EditorUtility.SetDirty(data);
    }

    // ================================================================
    //  UI 헬퍼
    // ================================================================
    void DrawHeader(string text)
    {
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField(text, style);
    }

    void DrawSlider(string propName, string label, float min, float max)
    {
        var prop = so.FindProperty(propName);
        if (prop == null) return;
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(140));
        prop.floatValue = EditorGUILayout.Slider(prop.floatValue, min, max);
        EditorGUILayout.EndHorizontal();
    }

    void DrawInfoBox(string text)
    {
        var style = new GUIStyle(EditorStyles.helpBox) { richText = true, fontSize = 11 };
        EditorGUILayout.LabelField(text, style, GUILayout.MinHeight(22));
    }

    void DrawWetnessPreview()
    {
        EditorGUILayout.LabelField("Wetness Preview", EditorStyles.miniLabel);
        Rect r = EditorGUILayout.GetControlRect(false, 20);

        // 배경 (마른 바닥)
        Color dryColor = new Color(0.22f, 0.21f, 0.2f);
        EditorGUI.DrawRect(r, dryColor);

        // 젖은 영역
        Rect wetRect = r;
        wetRect.width *= data.groundWetness;
        Color wetColor = new Color(
            dryColor.r * (1 - data.groundWetness * 0.35f),
            dryColor.g * (1 - data.groundWetness * 0.35f),
            dryColor.b * (1 - data.groundWetness * 0.2f));
        EditorGUI.DrawRect(wetRect, wetColor);

        var labelStyle = new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        EditorGUI.LabelField(r, $"Wetness: {data.groundWetness:P0}", labelStyle);
    }

    void DrawConnectionStatus()
    {
        EditorGUILayout.Space(4);
        var dnc = Object.FindFirstObjectByType<DayNightCycle>();
        var rain = Object.FindFirstObjectByType<RainController>();

        string status = "";
        if (dnc != null)
        {
            var dncSO = new SerializedObject(dnc);
            bool linked = dncSO.FindProperty("weatherData").objectReferenceValue == data;
            status += linked ? "DayNightCycle: Connected" : "DayNightCycle: Not linked";
        }
        else
            status += "DayNightCycle: Not in scene";

        status += "  |  ";

        if (rain != null)
            status += "RainController: In scene";
        else
            status += "RainController: Not in scene";

        var miniStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        EditorGUILayout.LabelField(status, miniStyle);
    }
}
