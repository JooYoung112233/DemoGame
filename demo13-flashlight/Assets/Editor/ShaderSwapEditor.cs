using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ShaderSwapEditor : EditorWindow
{
    enum FilterMode { All, SpriteOnly, MeshOnly }
    enum SwapMode { Material, Shader }
    enum GroupBy { Material, Parent }

    Vector2 scrollPos;
    FilterMode filter = FilterMode.All;
    SwapMode swapMode = SwapMode.Material;
    GroupBy groupBy = GroupBy.Material;
    Material targetMaterial;
    Shader targetShader;
    string searchFilter = "";
    bool includeInactive = true;

    List<DisplayGroup> groups = new List<DisplayGroup>();
    int totalRendererCount;

    Dictionary<Renderer, Material[]> backupMap = new Dictionary<Renderer, Material[]>();
    bool hasBackup;

    class DisplayGroup
    {
        public string label;
        public Material material;
        public List<RendererSlot> slots = new List<RendererSlot>();
        public bool foldout;
        public bool selected = true;
    }

    struct RendererSlot
    {
        public Renderer renderer;
        public int materialIndex;
    }

    [MenuItem("Tools/Shader Swap Editor")]
    static void Open()
    {
        var win = GetWindow<ShaderSwapEditor>("Shader Swap");
        win.minSize = new Vector2(440, 550);
        win.ScanScene();
    }

    void OnEnable() => ScanScene();
    void OnFocus() => ScanScene();

    void ScanScene()
    {
        groups.Clear();
        totalRendererCount = 0;

        var allRenderers = includeInactive
            ? Resources.FindObjectsOfTypeAll<Renderer>()
            : Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        var slotList = new List<(Renderer r, int idx, Material mat)>();
        var countedRenderers = new HashSet<Renderer>();

        foreach (var r in allRenderers)
        {
            if (r == null) continue;
            if (EditorUtility.IsPersistent(r.gameObject)) continue;
            if (r.gameObject.scene.name == null) continue;
            if (r is ParticleSystemRenderer) continue;

            if (filter == FilterMode.SpriteOnly && !(r is SpriteRenderer)) continue;
            if (filter == FilterMode.MeshOnly && r is SpriteRenderer) continue;

            var mats = r.sharedMaterials;
            if (mats == null || mats.Length == 0) continue;

            for (int i = 0; i < mats.Length; i++)
            {
                var mat = mats[i];
                if (mat == null) continue;

                if (!string.IsNullOrEmpty(searchFilter))
                {
                    string lf = searchFilter.ToLower();
                    if (!mat.name.ToLower().Contains(lf) &&
                        !(mat.shader != null && mat.shader.name.ToLower().Contains(lf)) &&
                        !r.gameObject.name.ToLower().Contains(lf) &&
                        !GetRootName(r.gameObject).ToLower().Contains(lf))
                        continue;
                }

                slotList.Add((r, i, mat));

                if (countedRenderers.Add(r))
                    totalRendererCount++;
            }
        }

        if (groupBy == GroupBy.Material)
            BuildMaterialGroups(slotList);
        else
            BuildParentGroups(slotList);
    }

    void BuildMaterialGroups(List<(Renderer r, int idx, Material mat)> slotList)
    {
        var map = new Dictionary<Material, DisplayGroup>();

        foreach (var (r, idx, mat) in slotList)
        {
            if (!map.TryGetValue(mat, out var group))
            {
                string shaderName = mat.shader != null ? mat.shader.name : "(none)";
                group = new DisplayGroup
                {
                    label = $"{mat.name}  [{shaderName}]",
                    material = mat
                };
                map[mat] = group;
            }
            group.slots.Add(new RendererSlot { renderer = r, materialIndex = idx });
        }

        groups = map.Values.OrderBy(g => g.label).ToList();
    }

    void BuildParentGroups(List<(Renderer r, int idx, Material mat)> slotList)
    {
        var map = new Dictionary<string, DisplayGroup>();

        foreach (var (r, idx, mat) in slotList)
        {
            string root = GetRootName(r.gameObject);
            if (!map.TryGetValue(root, out var group))
            {
                group = new DisplayGroup { label = root, material = null };
                map[root] = group;
            }
            group.slots.Add(new RendererSlot { renderer = r, materialIndex = idx });
        }

        groups = map.Values.OrderBy(g => g.label).ToList();
    }

    void OnGUI()
    {
        EditorGUILayout.Space(6);
        var headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.LabelField("SHADER SWAP EDITOR", headerStyle);
        EditorGUILayout.Space(4);

        DrawToolbar();
        EditorGUILayout.Space(4);
        DrawTargetSection();
        EditorGUILayout.Space(4);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        DrawGroups();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(4);
        DrawActions();
        EditorGUILayout.Space(4);
    }

    void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mode", GUILayout.Width(50));
        swapMode = (SwapMode)GUILayout.Toolbar((int)swapMode,
            new[] { "Material", "Shader" });
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Group", GUILayout.Width(50));
        var newGroupBy = (GroupBy)GUILayout.Toolbar((int)groupBy,
            new[] { "By Material", "By Parent" });
        if (newGroupBy != groupBy) { groupBy = newGroupBy; ScanScene(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Filter", GUILayout.Width(50));
        var newFilter = (FilterMode)GUILayout.Toolbar((int)filter,
            new[] { "All", "Sprite", "Mesh" });
        if (newFilter != filter) { filter = newFilter; ScanScene(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search", GUILayout.Width(50));
        var newSearch = EditorGUILayout.TextField(searchFilter);
        if (newSearch != searchFilter) { searchFilter = newSearch; ScanScene(); }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        var newInclude = EditorGUILayout.Toggle("Include Inactive", includeInactive);
        if (newInclude != includeInactive) { includeInactive = newInclude; ScanScene(); }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Refresh", GUILayout.Width(70))) ScanScene();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField(
            $"Groups: {groups.Count}  |  Renderers: {totalRendererCount}",
            EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndVertical();
    }

    void DrawTargetSection()
    {
        EditorGUILayout.BeginVertical("box");

        if (swapMode == SwapMode.Material)
        {
            EditorGUILayout.LabelField("Target Material", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            targetMaterial = (Material)EditorGUILayout.ObjectField(
                targetMaterial, typeof(Material), false);
            if (GUILayout.Button("Pick", GUILayout.Width(50)))
                ShowMaterialPicker();
            EditorGUILayout.EndHorizontal();

            if (targetMaterial != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(
                    $"Shader: {targetMaterial.shader.name}",
                    EditorStyles.miniLabel);
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.LabelField("Target Shader", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            targetShader = (Shader)EditorGUILayout.ObjectField(
                targetShader, typeof(Shader), false);
            if (GUILayout.Button("Pick", GUILayout.Width(50)))
                ShowShaderPicker();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    void DrawGroups()
    {
        if (groups.Count == 0)
        {
            EditorGUILayout.HelpBox("렌더러 없음. 필터/검색을 확인하세요.", MessageType.Info);
            return;
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft))
            groups.ForEach(g => g.selected = true);
        if (GUILayout.Button("Deselect All", EditorStyles.miniButtonMid))
            groups.ForEach(g => g.selected = false);
        if (GUILayout.Button("Invert", EditorStyles.miniButtonRight))
            groups.ForEach(g => g.selected = !g.selected);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(4);

        foreach (var group in groups)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            group.selected = EditorGUILayout.Toggle(group.selected, GUILayout.Width(18));

            // 아이콘
            if (group.material != null)
            {
                var preview = AssetPreview.GetAssetPreview(group.material);
                if (preview != null)
                    GUILayout.Label(preview, GUILayout.Width(20), GUILayout.Height(20));
                else
                    GUILayout.Label(EditorGUIUtility.IconContent("Material Icon"),
                        GUILayout.Width(20), GUILayout.Height(20));
            }
            else
            {
                GUILayout.Label(EditorGUIUtility.IconContent("Folder Icon"),
                    GUILayout.Width(20), GUILayout.Height(20));
            }

            int uniqueRenderers = group.slots.Select(s => s.renderer).Distinct().Count();
            group.foldout = EditorGUILayout.Foldout(group.foldout,
                $"{group.label}  ({uniqueRenderers})", true, EditorStyles.foldoutHeader);

            bool canSwap = swapMode == SwapMode.Material
                ? targetMaterial != null
                : targetShader != null;

            if (canSwap)
            {
                if (GUILayout.Button("Swap", GUILayout.Width(50)))
                {
                    SaveBackup();
                    ApplySwap(group.slots);
                    ScanScene();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (group.foldout)
            {
                EditorGUI.indentLevel++;

                if (group.material != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(24);
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(group.material, typeof(Material), false);
                    EditorGUI.EndDisabledGroup();
                    EditorGUILayout.EndHorizontal();
                }

                var uniqueSlots = group.slots
                    .GroupBy(s => s.renderer)
                    .Select(g2 => g2.First());

                foreach (var slot in uniqueSlots)
                {
                    var r = slot.renderer;
                    if (r == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(24);

                    string typeBadge = r is SpriteRenderer ? "[S]" : "[M]";
                    var badgeStyle = new GUIStyle(EditorStyles.miniLabel);
                    badgeStyle.normal.textColor = r is SpriteRenderer
                        ? new Color(0.4f, 0.8f, 0.4f)
                        : new Color(0.5f, 0.7f, 1f);
                    EditorGUILayout.LabelField(typeBadge, badgeStyle, GUILayout.Width(24));

                    string path = GetShortPath(r.gameObject);
                    if (GUILayout.Button(path, EditorStyles.linkLabel))
                    {
                        Selection.activeGameObject = r.gameObject;
                        EditorGUIUtility.PingObject(r.gameObject);
                    }

                    // 개별 머티리얼 표시
                    var mat = r.sharedMaterials.Length > slot.materialIndex
                        ? r.sharedMaterials[slot.materialIndex] : null;
                    if (mat != null && groupBy == GroupBy.Parent)
                    {
                        EditorGUILayout.LabelField(mat.name,
                            EditorStyles.miniLabel, GUILayout.Width(100));
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }
    }

    void DrawActions()
    {
        EditorGUILayout.BeginVertical("box");

        int selectedSlots = groups.Where(g => g.selected).Sum(g => g.slots.Count);
        int selectedRenderers = groups.Where(g => g.selected)
            .SelectMany(g => g.slots).Select(s => s.renderer).Distinct().Count();

        string modeLabel = swapMode == SwapMode.Material ? "Material" : "Shader";
        string targetName = swapMode == SwapMode.Material
            ? (targetMaterial != null ? targetMaterial.name : "(none)")
            : (targetShader != null ? targetShader.name : "(none)");

        EditorGUILayout.LabelField(
            $"Selected: {selectedRenderers} renderers ({selectedSlots} slots)",
            EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.BeginHorizontal();

        bool hasTarget = swapMode == SwapMode.Material
            ? targetMaterial != null : targetShader != null;

        GUI.enabled = hasTarget && selectedSlots > 0;
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button($"Apply to Selected", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Swap",
                $"선택된 {selectedRenderers}개 렌더러의 {modeLabel}을\n" +
                $"'{targetName}'(으)로 변경합니다.",
                "Apply", "Cancel"))
            {
                SaveBackup();
                var slots = groups.Where(g => g.selected).SelectMany(g => g.slots).ToList();
                ApplySwap(slots);
                ScanScene();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        GUI.enabled = hasTarget && totalRendererCount > 0;
        GUI.backgroundColor = new Color(1f, 0.7f, 0.3f);
        if (GUILayout.Button($"Apply to ALL", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Swap",
                $"씬의 모든 {totalRendererCount}개 렌더러의 {modeLabel}을\n" +
                $"'{targetName}'(으)로 변경합니다.",
                "Apply All", "Cancel"))
            {
                SaveBackup();
                var allSlots = groups.SelectMany(g => g.slots).ToList();
                ApplySwap(allSlots);
                ScanScene();
            }
        }
        GUI.backgroundColor = Color.white;
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        GUI.enabled = hasBackup;
        if (GUILayout.Button("Restore Backup", GUILayout.Height(24)))
        {
            RestoreBackup();
            ScanScene();
        }
        GUI.enabled = true;

        if (GUILayout.Button("Undo (Ctrl+Z)", GUILayout.Height(24)))
            Undo.PerformUndo();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ================================================================
    //  Swap
    // ================================================================

    void ApplySwap(List<RendererSlot> slots)
    {
        if (swapMode == SwapMode.Material)
            ApplyMaterialSwap(slots);
        else
            ApplyShaderSwap(slots);

        // 씬 변경 마킹
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    void ApplyMaterialSwap(List<RendererSlot> slots)
    {
        if (targetMaterial == null) return;

        int count = 0;
        var processed = new HashSet<Renderer>();

        foreach (var slot in slots)
        {
            var r = slot.renderer;
            if (r == null) continue;

            Undo.RecordObject(r, "Material Swap");

            if (r is SpriteRenderer sr)
            {
                sr.sharedMaterial = targetMaterial;
                count++;
            }
            else
            {
                var mats = r.sharedMaterials;
                if (slot.materialIndex < mats.Length)
                {
                    mats[slot.materialIndex] = targetMaterial;
                    r.sharedMaterials = mats;
                    count++;
                }
            }

            EditorUtility.SetDirty(r);
            processed.Add(r);
        }

        Debug.Log($"[ShaderSwap] Material '{targetMaterial.name}' → {count} slots on {processed.Count} renderers");
    }

    void ApplyShaderSwap(List<RendererSlot> slots)
    {
        if (targetShader == null) return;

        var processedMats = new HashSet<Material>();
        int count = 0;

        foreach (var slot in slots)
        {
            var r = slot.renderer;
            if (r == null) continue;

            var mats = r.sharedMaterials;
            if (slot.materialIndex >= mats.Length) continue;

            var mat = mats[slot.materialIndex];
            if (mat == null || processedMats.Contains(mat)) continue;

            Undo.RecordObject(mat, "Shader Swap");
            mat.shader = targetShader;
            EditorUtility.SetDirty(mat);

            processedMats.Add(mat);
            count++;
        }

        Debug.Log($"[ShaderSwap] Shader '{targetShader.name}' → {count} materials");
    }

    // ================================================================
    //  Backup / Restore
    // ================================================================

    void SaveBackup()
    {
        backupMap.Clear();
        foreach (var g in groups)
            foreach (var slot in g.slots)
            {
                var r = slot.renderer;
                if (r == null || backupMap.ContainsKey(r)) continue;
                backupMap[r] = r.sharedMaterials.ToArray();
            }
        hasBackup = true;
    }

    void RestoreBackup()
    {
        foreach (var kvp in backupMap)
        {
            if (kvp.Key == null) continue;
            Undo.RecordObject(kvp.Key, "Material Restore");

            if (kvp.Key is SpriteRenderer sr && kvp.Value.Length > 0)
                sr.sharedMaterial = kvp.Value[0];
            else
                kvp.Key.sharedMaterials = kvp.Value;

            EditorUtility.SetDirty(kvp.Key);
        }
        hasBackup = false;

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
    }

    // ================================================================
    //  Picker menus
    // ================================================================

    void ShowMaterialPicker()
    {
        var menu = new GenericMenu();

        // 씬에서 사용 중인 머티리얼 (최상단)
        if (groups.Count > 0)
        {
            menu.AddDisabledItem(new GUIContent("-- Scene (In Use) --"));
            var sceneMats = groups
                .Where(g => g.material != null)
                .Select(g => g.material)
                .Distinct();
            foreach (var mat in sceneMats)
            {
                var m = mat;
                string shaderName = m.shader != null ? m.shader.name : "?";
                menu.AddItem(new GUIContent($"Scene/{m.name}  [{shaderName}]"), false, () =>
                {
                    targetMaterial = m;
                    Repaint();
                });
            }
            menu.AddSeparator("");
        }

        // 프로젝트 에셋 머티리얼
        menu.AddDisabledItem(new GUIContent("-- Project Assets --"));
        var guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("IsoTools/")) continue;
            if (path.Contains("PackageCache/")) continue;
            if (path.Contains("PixelArtStudio/")) continue;
            if (path.Contains("Toon Muzzleflash")) continue;

            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null) continue;

            var m = mat;
            string folder = System.IO.Path.GetDirectoryName(path)
                .Replace("Assets\\", "").Replace("Assets/", "");
            if (string.IsNullOrEmpty(folder)) folder = "Root";
            string shaderName = m.shader != null ? m.shader.name : "?";

            menu.AddItem(new GUIContent($"{folder}/{m.name}  [{shaderName}]"), false, () =>
            {
                targetMaterial = m;
                Repaint();
            });
        }

        menu.ShowAsContext();
    }

    void ShowShaderPicker()
    {
        var menu = new GenericMenu();

        menu.AddDisabledItem(new GUIContent("-- Project Shaders --"));
        var guids = AssetDatabase.FindAssets("t:Shader", new[] { "Assets/Shaders" });
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var s = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (s == null) continue;
            var shader = s;
            menu.AddItem(new GUIContent(s.name), false, () =>
            {
                targetShader = shader;
                Repaint();
            });
        }

        menu.AddSeparator("");
        menu.AddDisabledItem(new GUIContent("-- Built-in --"));
        string[] builtins = {
            "Universal Render Pipeline/Lit",
            "Universal Render Pipeline/Unlit",
            "Universal Render Pipeline/Simple Lit",
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default",
        };
        foreach (var name in builtins)
        {
            var s = Shader.Find(name);
            if (s == null) continue;
            var shader = s;
            menu.AddItem(new GUIContent(name), false, () =>
            {
                targetShader = shader;
                Repaint();
            });
        }

        menu.ShowAsContext();
    }

    // ================================================================
    //  Util
    // ================================================================

    static string GetRootName(GameObject go)
    {
        var t = go.transform;
        while (t.parent != null) t = t.parent;
        return t.gameObject.name;
    }

    static string GetShortPath(GameObject go)
    {
        if (go.transform.parent == null)
            return go.name;

        string root = GetRootName(go);
        if (go.transform.parent.gameObject.name == root)
            return root + "/" + go.name;

        return root + "/.../" + go.name;
    }
}
