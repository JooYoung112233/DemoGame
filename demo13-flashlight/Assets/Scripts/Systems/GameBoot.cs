using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Systems 씬에 배치되는 부트 컨트롤러.
/// Systems 씬 단독으로 Play하면 기본 게임플레이 씬을 additive로 끌어온다.
/// (게임플레이 씬에서 시작해 Systems가 끌려온 경우엔 이미 콘텐츠 씬이 있으므로 아무것도 안 함.)
/// </summary>
public class GameBoot : MonoBehaviour
{
    [Tooltip("Systems 단독 진입 시 처음 열 게임플레이 씬")]
    [SerializeField] string defaultScene = "Safehouse";
    [Tooltip("기본 스폰 포인트 ID")]
    [SerializeField] string defaultSpawn = "default";
    [Tooltip("true면 부팅 시 타이틀 화면을 먼저 띄운다. false면 곧장 게임플레이 씬으로(개발용).")]
    [SerializeField] bool showTitleOnBoot = true;

    IEnumerator Start()
    {
        // 매니저 Awake/Bootstrap 안정화 한 프레임 대기
        yield return null;

        // 이미 게임플레이(콘텐츠) 씬이 로드돼 있으면(= 게임플레이 씬에서 직접 시작),
        // 씬 전환을 안 거쳤으니 플레이어를 스폰 포인트로 스냅만 하고 종료.
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SystemsScene.IsGameplayScene(SceneManager.GetSceneAt(i)))
            {
                SnapPlayerToSpawn();
                yield break;
            }

        // ── 타이틀 화면: 부팅 시 메인 메뉴를 먼저(새 게임/이어하기/종료) ──
        //    검게 덮은 채 타이틀을 띄우고 페이드로 드러낸다 → 부팅 직후 HUD(HP바) 깜빡임 차단.
        //    버튼이 직접 Safehouse로 전환하므로 여기서 자동 전환하지 않는다.
        if (showTitleOnBoot)
        {
            // SceneTransitionManager 준비 대기(페이드 커버용, 최대 3초)
            float tw = 0f;
            while (SceneTransitionManager.Instance == null && tw < 3f)
            {
                tw += Time.unscaledDeltaTime;
                yield return null;
            }

            var stm = SceneTransitionManager.Instance;
            if (stm != null) stm.CoverInstant();   // 즉시 검게 덮기
            TitleScreen.Show();
            yield return null;                      // 타이틀 1프레임 렌더 대기
            if (stm != null) yield return stm.RevealRoutine();  // 타이틀을 페이드로 드러냄
            yield break;
        }

        // SceneTransitionManager 준비 대기 (최대 3초)
        float t = 0f;
        while (SceneTransitionManager.Instance == null && t < 3f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (SceneTransitionManager.Instance != null && !string.IsNullOrEmpty(defaultScene))
            SceneTransitionManager.Instance.TransitionTo(defaultScene, defaultSpawn);
        else
            Debug.LogWarning("[GameBoot] SceneTransitionManager 없음 또는 defaultScene 미설정 — 게임플레이 씬을 못 엶.");
    }

    /// <summary>게임플레이 씬 직접 진입 시 플레이어를 스폰 포인트(있으면 defaultSpawn 우선)로 스냅.</summary>
    void SnapPlayerToSpawn()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return;

        var points = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
        if (points == null || points.Length == 0) return;

        SpawnPoint target = points[0];
        foreach (var sp in points)
            if (sp != null && sp.PointId == defaultSpawn) { target = sp; break; }

        if (target != null)
        {
            SpawnPoint.PlacePlayer(playerGO, target.transform.position);
        }
    }
}
