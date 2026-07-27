using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 건물 내부에서 나올 때 **들어온 문 앞**으로 되돌려 놓는다.
/// (docs/level-scrapmarket.md 2026-07-11 "건물 = 전당포식 씬 전환")
///
/// 왜 필요한가: 지역1의 절차 생성 점포는 수십 채인데 내부 씬은 아직 12개뿐이라
/// **여러 건물이 공용 내부(`Int_Generic`) 하나를 돌려 쓴다**(사용자 결정: 임의 1개 → 후속 확장).
/// 이때 출구가 고정 `SpawnPoint`를 가리키면 **엉뚱한 건물 앞으로 나오게** 되므로,
/// 진입 시 외부 좌표를 기억했다가 복귀 시 그 자리로 되돌린다.
///
/// 사용: 출구 `BuildingEntrance`의 spawnPointId를 <see cref="BackSpawnId"/>(`__back__`)로 두면 된다.
/// 전용 내부 씬(약국·창고 등)은 기존처럼 `from_&lt;건물&gt;` 고정 스폰을 써도 무방하다.
/// </summary>
public static class BuildingReturn
{
    /// <summary>이 값을 spawnPointId로 쓰면 "들어온 자리로 복귀"가 된다.</summary>
    public const string BackSpawnId = "__back__";

    static string _scene;
    static Vector3 _pos;
    static bool _has;

    /// <summary>대기 중인 복귀 지점이 있는지(디버그·QA용).</summary>
    public static bool HasPending => _has;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { _has = false; _scene = null; _pos = default; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>건물에 들어가기 직전 호출 — 현재 외부 씬·좌표를 기억.</summary>
    public static void Remember(string exteriorScene, Vector3 pos)
    {
        _scene = exteriorScene;
        _pos = pos;
        _has = true;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_has || scene.name != _scene) return;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;   // 아직 스폰 전이면 포기(고정 스폰 폴백)

        Vector3 p = _pos;
        p.z = player.transform.position.z;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.position = p;      // Rigidbody2D는 position으로 옮겨야 물리와 어긋나지 않음
        player.transform.position = p;
        CameraFollow.Instance?.SnapToTarget();   // 없으면 직전 위치에서 Lerp로 날아간다

        _has = false;
    }
}
