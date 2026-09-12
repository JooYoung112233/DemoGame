using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Safehouse 야외 전용 지도. 씬의 도로·건물·문 좌표를 캐시하고 플레이어만 갱신한다.</summary>
public sealed class TownMinimapHUD : MonoBehaviour
{
    const float Width = 308f, Height = 216f;
    public const float PanelHeight = Height + 72f;
    readonly List<ShopMarker> shops = new List<ShopMarker>();
    RectTransform panel, map, playerDot, heading;
    Rect world;
    Scene town;
    bool dirty = true;
    float scale;
    QuestHUD questHUD;
    readonly Color ink = new Color(.12f, .13f, .12f, 1f);
    readonly Color paper = new Color(.80f, .78f, .66f, 1f);
    readonly Color playerColor = new Color(.34f, .92f, .88f, 1f);

    sealed class ShopMarker
    {
        public BuildingUnlock unlock;
        public UnityEngine.UI.Text label;
        public string title;
        public UnityEngine.UI.Image door;
        public bool? wasOpen;
    }

    void Awake()
    {
        questHUD = GetComponentInParent<UIManager>()?.GetComponentInChildren<QuestHUD>(true);
        var canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null) { enabled = false; return; }
        panel = Box(canvas.transform, "TownMinimap", new Vector2(Width + 28, PanelHeight), new Color(.065f, .075f, .07f, .96f));
        panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one;
        panel.anchoredPosition = new Vector2(-24, -24);
        Label(panel, "마을 안내", new Vector2(-92, Height / 2 + 13), new Vector2(120, 24), 18, paper);
        Label(panel, "N ↑", new Vector2(131, Height / 2 + 13), new Vector2(50, 24), 16, paper);
        map = Box(panel, "TownPlan", new Vector2(Width, Height), new Color(.23f, .25f, .22f, 1));
        map.anchoredPosition = new Vector2(0, -1);
        Label(panel, "● 내 위치   ■ 입구   × 잠김", new Vector2(0, -Height / 2 - 18), new Vector2(Width, 22), 14, paper);
        panel.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        dirty = true;
        SceneManager.sceneLoaded += Loaded;
        SceneManager.sceneUnloaded += Unloaded;
    }
    void OnDisable()
    {
        SceneManager.sceneLoaded -= Loaded;
        SceneManager.sceneUnloaded -= Unloaded;
        if (panel != null) panel.gameObject.SetActive(false);
        questHUD?.SetTownMapVisible(false);
    }
    void OnDestroy() { if (panel != null) Destroy(panel.gameObject); }
    void Loaded(Scene scene, LoadSceneMode mode) { dirty = true; }
    void Unloaded(Scene scene) { dirty = true; }

    void Update()
    {
        if (panel == null) return;
        if (dirty) Rebuild();
        var player = TopDownPlayer.Instance;
        bool show = town.IsValid() && town.isLoaded && player != null && player.gameObject.activeInHierarchy
            && !HideoutController.IsActive && !TitleScreen.IsShowing
            && !(RaidManager.Instance != null && RaidManager.Instance.IsRaidActive)
            && !(SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning)
            && !(UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen());
        panel.gameObject.SetActive(show);
        questHUD?.SetTownMapVisible(show);
        if (!show || playerDot == null) return;
        playerDot.anchoredPosition = Project(Plan3D.ToPlan(player.transform.position));
        Vector2 facing = player.FacingDirection;
        heading.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg - 90f);
        foreach (var shop in shops)
        {
            bool open = shop.unlock == null || shop.unlock.IsOpen;
            if (shop.wasOpen == open) continue;
            shop.wasOpen = open;
            shop.label.text = shop.title + (open ? "" : " ×");
            shop.door.color = open ? new Color(.97f, .77f, .39f) : new Color(.66f, .49f, .42f);
        }
    }

    void Rebuild()
    {
        dirty = false;
        foreach (Transform child in map) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        shops.Clear();
        playerDot = null;
        town = SceneManager.GetSceneByName("Safehouse");
        if (!town.IsValid() || !town.isLoaded) return;
        var transforms = new List<Transform>();
        foreach (var root in town.GetRootGameObjects()) transforms.AddRange(root.GetComponentsInChildren<Transform>(true));
        // Ground collider is the authored playable map extent, not the visual prop bounds.
        world = new Rect(0, 0, 80, 56);
        foreach (var t in transforms)
            if (t.name == "Ground" && t.TryGetComponent<BoxCollider>(out var ground))
            {
                var b = ground.bounds;
                if (b.size.x > 10 && b.size.z > 10) world = new Rect(b.min.x, b.min.z, b.size.x, b.size.z);
            }
        scale = Mathf.Min((Width - 16) / world.width, (Height - 16) / world.height);
        foreach (var t in transforms)
            if (t.name.StartsWith("Road_") && t.TryGetComponent<Renderer>(out var road))
                Footprint(t.name, road.bounds, new Color(.44f, .44f, .37f));
        foreach (var t in transforms)
        {
            var building = t.GetComponent<BuildingInterior>();
            bool home = t.name == "Container_Home";
            if (building == null && !home) continue;
            if (!t.TryGetComponent<BoxCollider>(out var collider)) continue;
            var b = collider.bounds;
            var footprint = Footprint("Building_" + t.name, b, paper);
            var label = Label(footprint, BuildingName(t.name), Vector2.zero,
                new Vector2(84, home ? 36 : 22), home ? 13 : 14, ink);
            Vector3 entrance = t.position;
            Transform doorFrame = t.Find("DoorFrame");
            if (doorFrame != null) entrance = doorFrame.position;
            if (home)
                foreach (var candidate in transforms)
                    if (candidate.TryGetComponent<SceneDoor3D>(out var door) && door.TargetScene == "Hideout") entrance = candidate.position;
            var marker = Box(map, "Entrance_" + t.name, new Vector2(7, 7), paper);
            marker.anchoredPosition = Project(Plan3D.ToPlan(entrance));
            shops.Add(new ShopMarker { unlock = t.GetComponent<BuildingUnlock>(), label = label,
                title = BuildingName(t.name), door = marker.GetComponent<UnityEngine.UI.Image>() });
        }
        foreach (var t in transforms)
            if (t.TryGetComponent<InteractableObject>(out var io) &&
                (io.Type == InteractableObject.InteractType.MapBoard || io.Type == InteractableObject.InteractType.ExitPoint))
            {
                var gate = Box(map, "Departure", new Vector2(8, 8), new Color(.87f, .65f, .31f));
                gate.anchoredPosition = Project(Plan3D.ToPlan(t.position));
                Label(gate, "출전", new Vector2(-18, 15), new Vector2(50, 20), 13, paper);
            }
        playerDot = Box(map, "PlayerPosition", new Vector2(14, 14), ink);
        playerDot.GetComponent<UnityEngine.UI.Image>().sprite = PlaceholderSprite.Circle;
        heading = Box(playerDot, "Facing", Vector2.zero, Color.clear);
        var needle = Box(heading, "Direction", new Vector2(3, 10), playerColor);
        needle.anchoredPosition = new Vector2(0, 10);
        var dot = Box(playerDot, "Dot", new Vector2(8, 8), playerColor);
        dot.GetComponent<UnityEngine.UI.Image>().sprite = PlaceholderSprite.Circle;
    }

    Vector2 Project(Vector2 p)
    {
        var result = (p - world.center) * scale;
        return new Vector2(Mathf.Clamp(result.x, -Width / 2 + 8, Width / 2 - 8),
            Mathf.Clamp(result.y, -Height / 2 + 8, Height / 2 - 8));
    }
    RectTransform Footprint(string name, Bounds b, Color color)
    {
        var rt = Box(map, name, new Vector2(b.size.x, b.size.z) * scale, color);
        rt.anchoredPosition = Project(new Vector2(b.center.x, b.center.z));
        var outline = rt.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = ink;
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return rt;
    }
    static string BuildingName(string name)
    {
        switch (name)
        {
            case "Pawnshop": return "전당포";
            case "Repair": return "수리점";
            case "Medical": return "의료소";
            case "Furniture": return "가구점";
            case "BlackMarket": return "암시장";
            case "Container_Home": return "하이드\n아웃";
            default: return name;
        }
    }
    static RectTransform Box(Transform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = size;
        var image = go.GetComponent<UnityEngine.UI.Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }
    static UnityEngine.UI.Text Label(Transform parent, string title, Vector2 at, Vector2 size, int fontSize, Color color)
    {
        // Match NavigationHUD's existing Korean legacy font pipeline.
        var go = new GameObject("Label", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.sizeDelta = size;
        rt.anchoredPosition = at;
        var text = go.GetComponent<UnityEngine.UI.Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = title;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }
}
