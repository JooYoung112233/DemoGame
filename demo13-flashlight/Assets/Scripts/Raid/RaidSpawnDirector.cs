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
        Debug.Log($"[RaidSpawnDirector] 스폰={ChosenSpawn} · 탈출=고정({fixedExitName}) + {string.Join(",", Exits(ChosenSpawn))}");
    }

    GameObject PickSpawn()
    {
        var found = new List<GameObject>();
        foreach (var n in spawnNames)
        {
            var go = FindInScene(n);
            if (go != null) found.Add(go);
        }
        if (found.Count == 0) return null;
        return found[Random.Range(0, found.Count)];
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
    }

    void MovePlayerTo(Vector3 pos)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        pos.z = player.transform.position.z;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = pos;          // Rigidbody2D는 position으로 옮겨야 물리와 어긋나지 않음
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
