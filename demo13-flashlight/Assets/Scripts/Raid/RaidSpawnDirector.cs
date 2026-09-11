using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 레이드 진입 연출 — **스폰 랜덤 1곳 + 탈출 고정1 + 스폰 반대편 매치 2곳 활성**.
/// (docs/level-scrapmarket.md 2026-07-07 "스폰 랜덤·탈출 고정1+랜덤2 원거리 / 탈출 스폰 반대편",
///  Zone1GreyboxLayout 주석이 지목한 미구현 과제)
///
/// 동작(씬에 1개 배치, 지역 맵 전용):
///   1) 후보 스폰(SP*) 중 1곳 랜덤 → 플레이어 이동. (SceneTransitionManager.OnSceneLoaded의
///      배치보다 나중에 도는 Start에서 덮어씀 — sceneLoaded는 Start 이전)
///   2) 그 스폰의 매치표대로 탈출 풀(PX_*) 중 2곳만 활성 + 고정 탈출(Exit_Fixed)은 항상 활성,
///      나머지는 비활성 → 매 판 탈출 경로가 달라지고 맵 횡단을 유도.
///
/// 매치표는 "스폰에서 먼 코너"로 짜여 있다(빌더와 동일 — 한쪽에 치우치지 않게).
/// </summary>
public class RaidSpawnDirector : MonoBehaviour
{
    [Tooltip("후보 스폰 오브젝트 이름(씬). 이 중 1곳이 매 판 랜덤 선택된다.")]
    [SerializeField] string[] spawnNames = { "SP1_S", "SP2_N", "SP3_W", "SP4_E", "SP5_NE" };

    [Tooltip("항상 활성인 고정 탈출구 이름.")]
    [SerializeField] string fixedExitName = "Exit_Fixed";

    [Tooltip("탈출 풀(랜덤 매치 대상) 이름 전체. 매치되지 않은 것은 비활성.")]
    [SerializeField] string[] exitPoolNames = { "PX_SW", "PX_SE", "PX_NW", "PX_NE" };

    /// <summary>fixedExitName 말고도 **항상 열어 두는** 탈출구.
    ///
    /// 풀에 안 들어간 탈출구는 자동으로 상시 활성이 되는데, 그래서 여태 '일부러 고정으로 둔 것'과
    /// '풀에 넣는 걸 빠뜨린 것'을 구분할 수 없었다. 여기 적힌 것만 **의도된** 고정 탈출이다.
    ///   · Manhole_Exit — 남서 튜토 구역 맨홀. 2026-07-29 사용자 결정: **남서 고정으로 둔다**.
    ///     (튜토로 들어가는 길이 뚫리면서 실제로 쓸 수 있는 탈출구가 됐다. 풀에는 넣지 않는다.)
    ///
    /// [SerializeField]로 두지 않는 건 일부러다 — 배열 필드를 새로 직렬화하면 **이미 베이크된 씬**의
    /// 컴포넌트는 필드 초기화식을 안 타고 빈 배열이 되어, 고정 탈출이 조용히 사라진다.
    static readonly string[] AlwaysOnExtra = { "Manhole_Exit" };

    /// <summary>이번 판에 선택된 스폰 이름(디버그/HUD용).</summary>
    public static string ChosenSpawn { get; private set; }

    /// <summary>스폰 → 활성화할 탈출 2곳(스폰 반대편 먼 코너). 빌더 주석과 동일.</summary>
    static readonly Dictionary<string, string[]> Match = new Dictionary<string, string[]>
    {
        { "SP1_S",  new[] { "PX_NW", "PX_NE" } },
        { "SP2_N",  new[] { "PX_SW", "PX_SE" } },
        { "SP3_W",  new[] { "PX_SE", "PX_NE" } },
        { "SP4_E",  new[] { "PX_SW", "PX_NW" } },
        { "SP5_NE", new[] { "PX_SW", "PX_NW" } },
    };

    void Start()
    {
        var spawn = PickSpawn();
        if (spawn == null)
        {
            Debug.LogWarning("[RaidSpawnDirector] 후보 스폰을 못 찾음 — 랜덤 스폰/탈출 매치 생략(씬 마커 이름 확인).");
            return;
        }

        ChosenSpawn = spawn.name;
        MovePlayerTo(spawn.transform.position);
        ApplyExits(ChosenSpawn);
        Debug.Log($"[RaidSpawnDirector] 스폰={ChosenSpawn} · 탈출=고정({fixedExitName}," +
                  $"{string.Join(",", AlwaysOnExtra)}) + 매치({string.Join(",", Exits(ChosenSpawn))})");
    }

    /// <summary>후보 스폰 중 하나. **적이 붙어 있는 곳은 고르지 않는다** —
    /// 진입 직후 아무것도 못 하고 얻어맞는 시작은 랜덤 스폰의 취지(매 판 다른 동선)와 무관한 사고다.
    ///
    /// 안전 반경(GameTuning.raidSpawnSafeRadius)을 만족하는 후보들 중에서 랜덤 —
    /// "가장 안전한 곳"을 고르면 매 판 같은 데서 시작하게 되므로 무작위성은 지킨다.
    /// 전부 실패하면 그중 가장 여유 있는 곳(적이 아직 안 깔렸으면 예전처럼 완전 랜덤).</summary>
    GameObject PickSpawn()
    {
        var found = new List<GameObject>();
        foreach (var n in spawnNames)
        {
            var go = FindInScene(n);
            if (go != null) found.Add(go);
        }
        if (found.Count == 0) return null;

        var gt = GameTuning.Instance;
        float safe = gt != null ? gt.raidSpawnSafeRadius : 20f;
        var enemies = EnemyController.All;
        if (safe <= 0f || enemies == null || enemies.Count == 0)
            return found[Random.Range(0, found.Count)];

        var ok = new List<GameObject>();
        GameObject best = null; float bestClear = -1f;
        foreach (var go in found)
        {
            float clear = float.MaxValue;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] == null) continue;
                float d = Vector3.Distance(go.transform.position, enemies[i].transform.position);
                if (d < clear) clear = d;
            }
            if (clear >= safe) ok.Add(go);
            if (clear > bestClear) { bestClear = clear; best = go; }
        }

        if (ok.Count > 0) return ok[Random.Range(0, ok.Count)];
        Debug.LogWarning($"[RaidSpawnDirector] 안전 반경 {safe}m를 만족하는 스폰이 없다 — 가장 여유 있는 {best.name}({bestClear:F0}m) 사용.");
        return best;
    }

    string[] Exits(string spawnName)
        => Match.TryGetValue(spawnName, out var e) ? e : new string[0];

    void ApplyExits(string spawnName)
    {
        var active = Exits(spawnName);
        foreach (var n in exitPoolNames)
        {
            var go = FindInScene(n);
            if (go == null) continue;
            bool on = System.Array.IndexOf(active, n) >= 0;
            go.SetActive(on);
        }
        // 고정 탈출은 항상 활성(꺼져 있었을 수 있으니 보정)
        var fx = FindInScene(fixedExitName);
        if (fx != null) fx.SetActive(true);
        foreach (var n in AlwaysOnExtra)
        {
            var go = FindInScene(n);
            if (go != null) go.SetActive(true);
        }
    }

    void MovePlayerTo(Vector3 pos)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        // 3D: 지면 평면은 XZ, 높이는 Y. (2D 시절엔 z가 정렬 깊이라 z를 보존했는데,
        //  그대로 두면 X만 옮겨지고 Z는 직전 씬 값이 남아 스폰이 맵 밖으로 튄다.)
        pos.y = player.transform.position.y;
        var rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.position = pos;                      // Rigidbody는 position으로 옮겨야 물리와 어긋나지 않음
        }
        player.transform.position = pos;

        // 카메라 즉시 스냅 — 없으면 직전 위치(안전가옥 등)에서 최대 300u를 Lerp로 날아가는 게 보인다.
        //   (spawnId=""라 SceneTransitionManager의 스냅 경로를 안 타므로 여기서 직접)
        CameraFollow.Instance?.SnapToTarget();
    }

    /// <summary>이 디렉터가 속한 씬에서만 이름으로 탐색(비활성 포함) — 다른 씬의 동명 오브젝트 오인 방지.</summary>
    GameObject FindInScene(string n)
    {
        if (string.IsNullOrEmpty(n)) return null;
        var scene = gameObject.scene;
        foreach (var root in scene.GetRootGameObjects())
        {
            var t = FindRecursive(root.transform, n);
            if (t != null) return t.gameObject;
        }
        return null;
    }

    static Transform FindRecursive(Transform t, string n)
    {
        if (t.name == n) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = FindRecursive(t.GetChild(i), n);
            if (r != null) return r;
        }
        return null;
    }
}
