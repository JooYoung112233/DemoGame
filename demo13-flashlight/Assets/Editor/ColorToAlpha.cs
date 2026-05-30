using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 지정 색상(기본 흰색)을 알파 투명으로 변환하는 텍스쳐 전처리 도구.
/// Project 창에서 텍스쳐 선택 → 우클릭 "Color → Alpha" 또는
/// 메뉴: Tools > Dev Tools > Color to Alpha
/// </summary>
public class ColorToAlpha : EditorWindow
{
    Color keyColor = Color.white;       // 투명으로 만들 색
    float threshold = 0.15f;            // 색 일치 허용 범위
    float edgeSoftness = 0.1f;          // 경계 부드러움
    bool useColorPicker = true;         // 색 직접 지정 vs 모서리 자동 감지
    bool overwrite = false;
    string suffix = "_alpha";
    Vector2 scrollPos;

    [MenuItem("Tools/Dev Tools/Color to Alpha")]
    static void Open()
    {
        var window = GetWindow<ColorToAlpha>("Color to Alpha");
        window.minSize = new Vector2(360, 360);
    }

    [MenuItem("Assets/Color → Alpha", false, 100)]
    static void OpenFromContext()
    {
        GetWindow<ColorToAlpha>("Color to Alpha");
    }

    [MenuItem("Assets/Color → Alpha", true)]
    static bool ValidateContext()
    {
        foreach (var obj in Selection.objects)
            if (obj is Texture2D) return true;
        return false;
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("색상 → 알파 변환", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "지정한 색(배경)을 투명(alpha=0)으로 변환합니다.\n" +
            "Project 창에서 텍스쳐 선택 후 변환하세요.",
            MessageType.Info);
        EditorGUILayout.Space(8);

        useColorPicker = EditorGUILayout.Toggle("색 직접 지정", useColorPicker);
        if (useColorPicker)
        {
            keyColor = EditorGUILayout.ColorField("투명화할 색", keyColor);
            // 자주 쓰는 색 단축 버튼
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("흰색")) keyColor = Color.white;
            if (GUILayout.Button("검정")) keyColor = Color.black;
            if (GUILayout.Button("마젠타")) keyColor = Color.magenta;
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("좌상단 모서리 픽셀 색을 자동으로 배경색으로 사용합니다.", MessageType.None);
        }

        EditorGUILayout.Space(8);
        threshold = EditorGUILayout.Slider("Threshold (색 허용범위)", threshold, 0.01f, 1.0f);
        EditorGUILayout.HelpBox("높을수록 비슷한 색도 제거. 흰색 배경은 0.1~0.2 추천.", MessageType.None);

        edgeSoftness = EditorGUILayout.Slider("Edge Softness (경계)", edgeSoftness, 0f, 0.5f);

        EditorGUILayout.Space(5);
        overwrite = EditorGUILayout.Toggle("원본 덮어쓰기", overwrite);
        if (!overwrite)
            suffix = EditorGUILayout.TextField("파일 접미사", suffix);

        EditorGUILayout.Space(10);

        int texCount = 0;
        foreach (var obj in Selection.objects)
            if (obj is Texture2D) texCount++;
        EditorGUILayout.LabelField($"선택된 텍스쳐: {texCount}개");

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        GUI.enabled = texCount > 0;
        if (GUILayout.Button("변환 실행", GUILayout.Height(40)))
            ProcessSelection();
        GUI.enabled = true;
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    void ProcessSelection()
    {
        int processed = 0;
        foreach (var obj in Selection.objects)
        {
            if (obj is Texture2D tex)
            {
                ProcessTexture(tex);
                processed++;
            }
        }

        if (processed > 0)
        {
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[ColorToAlpha]</color> {processed}개 텍스쳐 변환 완료");
        }
        else
        {
            EditorUtility.DisplayDialog("알림", "Project 창에서 텍스쳐를 선택하세요.", "확인");
        }
    }

    void ProcessTexture(Texture2D source)
    {
        string sourcePath = AssetDatabase.GetAssetPath(source);
        var importer = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
        if (importer == null) return;

        // Read/Write + 비압축 강제
        bool needReimport = false;
        if (!importer.isReadable) { importer.isReadable = true; needReimport = true; }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        { importer.textureCompression = TextureImporterCompression.Uncompressed; needReimport = true; }
        if (needReimport)
        {
            importer.SaveAndReimport();
            source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
        }

        Color[] pixels = source.GetPixels();
        int width = source.width;
        int height = source.height;

        // 배경색 결정
        Color bg = keyColor;
        if (!useColorPicker)
            bg = pixels[(height - 1) * width]; // 좌상단 픽셀

        int removed = 0;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color px = pixels[i];
            float dr = px.r - bg.r;
            float dg = px.g - bg.g;
            float db = px.b - bg.b;
            float dist = Mathf.Sqrt(dr * dr + dg * dg + db * db);

            if (dist < threshold)
            {
                float alpha;
                if (edgeSoftness > 0.001f)
                {
                    float fadeStart = threshold - edgeSoftness;
                    alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStart, threshold, dist));
                }
                else alpha = 0f;

                pixels[i] = new Color(px.r, px.g, px.b, px.a * alpha);
                removed++;
            }
        }

        // 저장
        var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
        result.SetPixels(pixels);
        result.Apply();

        string outputPath;
        if (overwrite) outputPath = sourcePath;
        else
        {
            string dir = Path.GetDirectoryName(sourcePath);
            string name = Path.GetFileNameWithoutExtension(sourcePath);
            string ext = Path.GetExtension(sourcePath);
            outputPath = Path.Combine(dir, name + suffix + ext);
        }

        File.WriteAllBytes(outputPath, result.EncodeToPNG());
        Object.DestroyImmediate(result);

        AssetDatabase.ImportAsset(outputPath);
        var outImporter = AssetImporter.GetAtPath(outputPath) as TextureImporter;
        if (outImporter != null)
        {
            outImporter.alphaSource = TextureImporterAlphaSource.FromInput;
            outImporter.alphaIsTransparency = true;
            outImporter.SaveAndReimport();
        }

        Debug.Log($"<color=green>[ColorToAlpha]</color> '{Path.GetFileName(outputPath)}' 저장 | " +
            $"{removed}/{pixels.Length} 픽셀 투명화 (배경색 {bg})");
    }
}
