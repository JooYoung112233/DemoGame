// 컴파일 + **기하 시뮬레이션**용 스텁. Unity를 못 돌리므로 빌더를 그대로 실행시켜
// 벽/문/상자 좌표를 기록하고 연결성(안뜰에 갈 수 있나)을 검사한다.
using System.Collections.Generic;

namespace UnityEngine.SceneManagement { public struct Scene { public string name; } }

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float a, float b) { x = a; y = b; }
        public static Vector2 one => new Vector2(1, 1);
        public static Vector2 down => new Vector2(0, -1);
        public float sqrMagnitude => x * x + y * y;
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float a, float b, float c) { x = a; y = b; z = c; }
        public static implicit operator Vector2(Vector3 v) => new Vector2(v.x, v.y);
        public static Vector3 zero => new Vector3(0, 0, 0);
        public static Vector3 one => new Vector3(1, 1, 1);
    }
    public struct Quaternion { public static Quaternion Euler(float a, float b, float c) => default; public static Quaternion identity => default; }
    public struct Rect
    {
        public float x, y, width, height;
        public float xMin => x; public float xMax => x + width;
        public float yMin => y; public float yMax => y + height;
        public Rect(float a, float b, float c, float d) { x = a; y = b; width = c; height = d; }
        public static Rect MinMaxRect(float a, float b, float c, float d) => new Rect(a, b, c - a, d - b);
        public bool Contains(Vector2 p) => p.x >= xMin && p.x <= xMax && p.y >= yMin && p.y <= yMax;
        public bool Overlaps(Rect r) => xMin < r.xMax && r.xMin < xMax && yMin < r.yMax && r.yMin < yMax;
    }
    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public static float Min(float a, float b) => a < b ? a : b;
        public static float Max(float a, float b) => a > b ? a : b;
        public static int Max(int a, int b) => a > b ? a : b;
        public static float Abs(float a) => a < 0 ? -a : a;
        public static float Sign(float a) => a < 0 ? -1f : 1f;
        public static float Lerp(float a, float b, float t) => a + (b - a) * t;
        public static float Clamp01(float a) => a < 0 ? 0 : a > 1 ? 1 : a;
        public static int Clamp(int a, int lo, int hi) => a < lo ? lo : a > hi ? hi : a;
        public static float Clamp(float a, float lo, float hi) => a < lo ? lo : a > hi ? hi : a;
        public static int RoundToInt(float a) => (int)System.Math.Round((double)a, System.MidpointRounding.ToEven);
        public static int CeilToInt(float a) => (int)System.Math.Ceiling((double)a);
        public static int FloorToInt(float a) => (int)System.Math.Floor((double)a);
        public static float InverseLerp(float a, float b, float v) => 0f;
        public static float Sqrt(float a) => (float)System.Math.Sqrt(a);
        public static float Cos(float a) => (float)System.Math.Cos(a);
        public static float Sin(float a) => (float)System.Math.Sin(a);
        public static bool Approximately(float a, float b) => System.Math.Abs(a - b) < 1e-5f;
    }
    public static class Debug
    {
        public static void Log(string s) => Rec.Logs.Add(s);
        public static void LogWarning(string s) => Rec.Warns.Add(s);
        public static void LogError(string s) => Rec.Warns.Add("ERROR " + s);
    }
    public static class Application { public static bool isBatchMode => true; }
    public static class Resources
    {
        // 팔레트 프리팹은 좌표만 필요하므로 '있다'고만 답한다(id를 이름으로 실어 보낸다).
        public static T Load<T>(string path) where T : Object
            => typeof(T) == typeof(GameObject)
             ? (T)(object)new GameObject(path.Substring(path.LastIndexOf('/') + 1))
             : null;
    }
    public class Object
    {
        public string name;
        public static void DestroyImmediate(Object o) { if (o is Component c && c.owner != null) c.owner.parts.Remove(c); }
    }
    public class Component : Object
    {
        internal GameObject owner;
        public Transform transform => owner != null ? owner.transform : null;
        public GameObject gameObject => owner;
        public T GetComponent<T>() where T : Component => owner != null ? owner.GetComponent<T>() : null;
        public T GetComponentInChildren<T>() where T : Component => owner != null ? owner.GetComponentInChildren<T>() : null;
        public T AddComponent<T>() where T : Component => owner != null ? owner.AddComponent<T>() : null;
    }
    public class Transform : Component
    {
        public Vector3 localPosition, localScale = new Vector3(1, 1, 1);
        public Quaternion localRotation;
        internal Transform parent;
        internal readonly List<Transform> kids = new List<Transform>();
        public int childCount => kids.Count;
        public Transform GetChild(int i) => kids[i];
        // 이름은 오브젝트 쪽이 진실 — 프리팹으로 찍은 뒤 go.name을 갈아 끼우는 경로가 있다.
        public Transform Find(string n) { foreach (var k in kids) if (k.owner.name == n) return k; return null; }
        public void SetParent(Transform t, bool worldPositionStays)
        {
            if (parent != null) parent.kids.Remove(this);
            parent = t;
            if (t != null) t.kids.Add(this);
        }
    }
    public class GameObject : Object
    {
        public readonly List<Component> parts = new List<Component>();
        readonly Transform _t;
        public GameObject() : this("GameObject") { }
        public GameObject(string n)
        {
            _t = new Transform();
            _t.owner = this;
            name = n;
            Rec.All.Add(this);
        }
        /// <summary>Transform.name과 항상 같이 간다(유니티와 동일). 안 맞추면 프리팹을 찍고
        /// 이름을 갈아 끼운 오브젝트를 Find/FindChild가 **조용히 못 찾는다** — 실제로 이걸로 물렸다.</summary>
        public new string name
        {
            get => base.name;
            set { base.name = value; if (_t != null) _t.name = value; }
        }
        public Transform transform => _t;
        public SceneManagement.Scene scene => default;
        public T GetComponent<T>() where T : Component
        {
            foreach (var c in parts) if (c is T t) return t;
            return null;
        }
        public T GetComponentInChildren<T>() where T : Component
        {
            var r = GetComponent<T>();
            if (r != null) return r;
            foreach (var k in _t.kids) { var v = k.owner.GetComponentInChildren<T>(); if (v != null) return v; }
            return null;
        }
        public T AddComponent<T>() where T : Component
        {
            var c = (T)System.Activator.CreateInstance(typeof(T));
            c.owner = this; c.name = name;
            parts.Add(c);
            return c;
        }
    }
    public class MonoBehaviour : Component { }
    public class Collider2D : Component { }
    public class BoxCollider2D : Collider2D { public bool isTrigger; public Vector2 size, offset; }
    public class ScriptableObject : Object { }
}

namespace UnityEditor
{
    using UnityEngine;
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class MenuItemAttribute : System.Attribute
    {
        public MenuItemAttribute(string path) { }
        public int priority;
    }
    public class SerializedProperty
    {
        public string stringValue; public float floatValue; public bool boolValue;
        public int enumValueIndex; public Object objectReferenceValue;
    }
    public class SerializedObject
    {
        public SerializedObject(Object o) { }
        public SerializedProperty FindProperty(string n) => new SerializedProperty();
        public void ApplyModifiedPropertiesWithoutUndo() { }
    }
    public class EditorBuildSettingsScene
    {
        public string path;
        public EditorBuildSettingsScene(string p, bool e) { path = p; }
    }
    public static class EditorBuildSettings
    {
        public static EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[0];
    }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : Object => null;
        public static void SaveAssets() { }
    }
}

// ── 기록기 ──
public static class Rec
{
    public struct Box { public float x0, y0, x1, y1; public string name, kind; }
    public struct Pt { public float x, y; public string name, kind; }
    public static readonly List<Box> Solids = new List<Box>();
    public static readonly List<Pt> Points = new List<Pt>();
    public static readonly List<string> Logs = new List<string>();
    public static readonly List<string> Warns = new List<string>();
    public static readonly List<UnityEngine.GameObject> All = new List<UnityEngine.GameObject>();
    /// <summary>치우거나 조건을 맞추면 지나갈 수 있는 것들 — 래스터에서 뺀다.</summary>
    public static readonly HashSet<string> Passable = new HashSet<string>();

    public static void Solid(string kind, string name, float cx, float cy, float w, float h)
        => Solids.Add(new Box { x0 = cx - w * 0.5f, y0 = cy - h * 0.5f, x1 = cx + w * 0.5f, y1 = cy + h * 0.5f, name = name, kind = kind });
    public static void Point(string kind, string name, float x, float y)
        => Points.Add(new Pt { x = x, y = y, name = name, kind = kind });

    /// <summary>프리팹 경로로 배치된 것들(튜토 빌더)을 Build가 끝난 뒤 훑어 기록한다.
    /// GreyboxBuild를 쓰는 Zone1과 달리 튜토는 PrefabUtility로 찍고 **나중에** 크기를 준다.</summary>
    public static void Harvest()
    {
        foreach (var go in All)
        {
            var tag = go.GetComponent<StubPrefabTag>();
            if (tag == null) continue;
            var t = go.transform;
            float x = t.localPosition.x, y = t.localPosition.y;
            switch (tag.id)
            {
                case "gb_floor": break;
                case "gb_wall":      Solid("wall", go.name, x, y, t.localScale.x, t.localScale.y); break;
                case "gb_barricade": Solid("barricade", go.name, x, y, t.localScale.x, t.localScale.y); break;
                case "gb_prop":      Solid("prop", go.name, x, y, t.localScale.x, t.localScale.y); break;
                case "gb_car":       Solid("car", go.name, x, y, t.localScale.x, t.localScale.y); break;
                default:             Point(tag.id, go.name, x, y); break;
            }
        }
    }
}

public class SpawnPoint : UnityEngine.MonoBehaviour { }
public class ItemSpawnPoint : UnityEngine.MonoBehaviour { }
public class LootContainer : UnityEngine.MonoBehaviour { public void Setup(string n, int w, int h) { } }
public class MapSpawnProfile : UnityEngine.ScriptableObject { }
public class MapSpawnController : UnityEngine.MonoBehaviour { }
public class RaidSpawnDirector : UnityEngine.MonoBehaviour { }
public class DoorController : UnityEngine.MonoBehaviour { }
public class SpawnZone : UnityEngine.MonoBehaviour
{
    public void Setup(UnityEngine.Vector3 s, int c, string k)
        => Rec.Point("enemyzone", name + "|" + k + "x" + c, transform.localPosition.x, transform.localPosition.y);
}
public class BuildingEntrance : UnityEngine.MonoBehaviour
{
    public void Configure(string s, string i, bool e) { }
    public void Configure(string s, string i, bool e, UnityEngine.Vector2 sz) { }
    public void SetRequireInteract(bool v) { }
}
public class InteractableObject : UnityEngine.MonoBehaviour
{
    public enum InteractType { Door, Container, ExitPoint, NPC, Note, Passage }
    public void Configure(InteractType t, string prompt, float r) { }
    public void SetNote(string c, string t, string p) { }
}
public class BlockedPassage : UnityEngine.MonoBehaviour
{
    public enum Mode { Permanent, Clearable, Locked, NightOnly, Code }
    public void Configure(Mode m, string label, string item)
    {
        // Permanent가 아니면 **대가를 치르면 지나갈 수 있는 길**이다 — 연결성 검사에선 뚫린 것으로 본다.
        if (m != Mode.Permanent) Rec.Passable.Add(name);
        Rec.Point("passage", name + "|" + m, transform.localPosition.x, transform.localPosition.y);
    }
}

// ── 튜토(고철시장) 빌더가 추가로 쓰는 것들 ──
//   2026-07-29: 튜토 동측을 고철 야적장으로 바꾸면서 **튜토도 검사 대상**에 넣었다.
//   여기 지오메트리를 손대면서 검사 밖에 두면 "문이 막혔나"를 또 눈으로만 보게 된다.
public class QuestPoiZone : UnityEngine.MonoBehaviour { public string poiId, displayName; }
public class RaidManager : UnityEngine.MonoBehaviour { }
public static class ContentBuildAll { public static bool Quiet => true; }
public static class GreyboxPaletteBuilder { public static void Generate() { } }

/// <summary>프리팹으로 찍힌 오브젝트에 어떤 gb_*였는지 남긴다(호출자가 이름을 덮어쓰므로).</summary>
public class StubPrefabTag : UnityEngine.MonoBehaviour { public string id; }

namespace UnityEditor
{
    using UnityEngine;
    public static class PrefabUtility
    {
        // 크기는 이 시점엔 아직 안 정해졌으므로, Build가 끝난 뒤 Rec.Harvest()가 훑어 기록한다.
        public static Object InstantiatePrefab(Object prefab, Transform parent)
        {
            var go = new GameObject(prefab != null ? prefab.name : "gb");
            go.transform.SetParent(parent, false);
            go.AddComponent<StubPrefabTag>().id = prefab != null ? prefab.name : "gb";
            return go;
        }
    }
    public static class EditorSceneBuildUtil
    {
        public static UnityEngine.SceneManagement.Scene NewDetachedScene(
            out UnityEngine.SceneManagement.Scene prev) { prev = default; return default; }
        public static bool SaveAndClose(UnityEngine.SceneManagement.Scene s, string p,
                                        UnityEngine.SceneManagement.Scene prev) => true;
    }
    public static class Selection { public static Object activeObject; }
    public static class EditorUtility
    {
        public static bool DisplayDialog(string t, string m, string ok) => true;
    }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s, string p) => true;
    }
}
public class GameTuning : UnityEngine.ScriptableObject
{
    public static GameTuning Instance => null;   // 에셋에 필드가 없어 코드 기본값을 쓰는 현 상태와 동일
    public float buildingEnterRatio, roadObstacleDensity, roadMinPassWidth;
}

public static class GreyboxBuild
{
    static UnityEngine.GameObject Spawn(string name, UnityEngine.GameObject parent)
    {
        var go = new UnityEngine.GameObject(name);
        go.transform.SetParent(parent != null ? parent.transform : null, false);
        return go;
    }
    static int Bar(string kind, UnityEngine.GameObject p, string n, float cx, float cy, float w, float h)
    {
        var go = Spawn(n, p);
        go.transform.localPosition = new UnityEngine.Vector3(cx, cy, 0f);
        go.transform.localScale = new UnityEngine.Vector3(w, h, 1f);
        Rec.Solid(kind, n, cx, cy, w, h);
        return 1;
    }
    public static UnityEngine.GameObject BeginScene(out UnityEngine.SceneManagement.Scene s)
    {
        s = default;
        return new UnityEngine.GameObject("Map");
    }
    public static void EndScene(UnityEngine.SceneManagement.Scene s, string p, int n, string l)
        => Rec.Logs.Add("EndScene placed=" + n + " " + l);
    public static int Floor(UnityEngine.GameObject p, string n, float a, float b, float c, float d) { Spawn(n, p); return 1; }
    public static int Wall(UnityEngine.GameObject p, string n, float a, float b, float c, float d) => Bar("wall", p, n, a, b, c, d);
    public static int Wall(UnityEngine.GameObject p, string n, float a, float b, float c, float d, float e) => Bar("wall", p, n, a, b, c, d);
    public static int Barricade(UnityEngine.GameObject p, string n, float a, float b, float c, float d) => Bar("barricade", p, n, a, b, c, d);
    public static int Car(UnityEngine.GameObject p, string n, float a, float b, float c, float d, float e = 0f) => Bar("car", p, n, a, b, c, d);
    public static int Prop(UnityEngine.GameObject p, string n, float a, float b, float c, float d) => Bar("prop", p, n, a, b, c, d);
    public static int WallSeg(UnityEngine.GameObject p, string n, float ax, float ay, float bx, float by)
    {
        if (bx - ax <= 0.001f || by - ay <= 0.001f) return 0;
        return Wall(p, n, (ax + bx) * 0.5f, (ay + by) * 0.5f, bx - ax, by - ay);
    }
    public static int Marker(UnityEngine.GameObject p, string id, string n, float x, float y)
    {
        var go = Spawn(n, p);
        go.transform.localPosition = new UnityEngine.Vector3(x, y, 0f);
        // 팔레트 프리팹이 갖고 있는 컴포넌트를 흉내낸다(빌더가 GetComponentInChildren으로 찾는다).
        switch (id)
        {
            case "gb_spawn": go.AddComponent<SpawnPoint>(); break;
            case "gb_exit": go.AddComponent<InteractableObject>(); break;
            case "gb_door": go.AddComponent<InteractableObject>(); go.AddComponent<DoorController>(); break;
            case "gb_crate": go.AddComponent<LootContainer>(); go.AddComponent<InteractableObject>(); break;
            case "gb_shelf": go.AddComponent<LootContainer>(); go.AddComponent<InteractableObject>(); break;
            case "gb_barricade": Rec.Solid("barricade", n, x, y, 1f, 1f); break;
        }
        Rec.Point(id, n, x, y);
        return 1;
    }
    public static int Note(UnityEngine.GameObject p, string n, float x, float y, string t, string c)
    {
        var go = Spawn(n, p);
        go.transform.localPosition = new UnityEngine.Vector3(x, y, 0f);
        go.AddComponent<InteractableObject>();
        Rec.Point("gb_note", n + "|" + t, x, y);
        return 1;
    }
    public static void Relabel(UnityEngine.Transform t, string s) { }
}

