using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 영속 시스템 씬("Systems") 부트스트랩 / 판별 유틸.
///
/// Systems 씬 = [매니저들] + UIManager(+모든 UI 패널) + PlayerRig(카메라/라이트/후처리) + 글로벌 조명 + GameBoot.
/// 게임플레이(콘텐츠) 씬(안전가옥/인게임 등)은 그 위에 <b>additive</b>로 교체 로드된다(SceneTransitionManager).
///
/// 진입 시나리오:
///  • Systems 씬에서 Play    → GameBoot이 기본 게임플레이 씬을 additive 로드.
///  • 게임플레이 씬에서 Play  → 여기 EnsureLoaded가 Systems를 additive로 끌어와 매니저/플레이어/UI 공급.
///  • 맵툴 씬(MapTool*)에서 Play → 아무것도 안 함(자체 완결).
///  • Systems가 빌드세팅에 아직 없음(빌더 미실행) → 끌어오지 않음 → 기존 코드 스폰 폴백이 동작.
///
/// 이렇게 하면 기존 자동 부트스트랩(GameBootstrap/UIManager/TopDownPlayer/SceneTransitionManager)이
/// "Systems가 공급하면 건너뛰고, 없으면 폴백"으로 자연스럽게 양립한다.
/// </summary>
public static class SystemsScene
{
    public const string SceneName = "Systems";

    /// <summary>Systems 씬이 빌드세팅에 등록되어 로드 가능한가.</summary>
    public static bool Available => Application.CanStreamedLevelBeLoaded(SceneName);

    /// <summary>Systems 씬이 현재 로드되어 있는가.</summary>
    public static bool IsLoaded
    {
        get
        {
            var s = SceneManager.GetSceneByName(SceneName);
            return s.IsValid() && s.isLoaded;
        }
    }

    /// <summary>지금 런타임이 Systems 씬 기반으로 동작하는가(코드 스폰 폴백을 막을지 판단용).
    /// = Systems 씬이 빌드되어 있고 + 맵툴 씬이 아님.</summary>
    public static bool ProvidesSystems => Available && !MapToolScene.IsActive;

    /// <summary>게임플레이(맵 콘텐츠) 씬인지 — Systems / 맵툴 / 무효 씬 제외.</summary>
    public static bool IsGameplayScene(Scene s)
    {
        if (!s.IsValid()) return false;
        if (s.name == SceneName) return false;
        if (!string.IsNullOrEmpty(s.name) &&
            s.name.IndexOf("MapTool", System.StringComparison.OrdinalIgnoreCase) >= 0) return false;
        return true;
    }

    /// <summary>게임플레이 씬에서 직접 Play했을 때 Systems를 additive로 끌어온다.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureLoaded()
    {
        if (MapToolScene.IsActive) return;        // 맵툴: 시스템 안 띄움
        if (!Available) return;                   // Systems 미빌드 → 기존 폴백에 맡김
        var active = SceneManager.GetActiveScene();
        if (active.name == SceneName) return;     // Systems에서 시작 → GameBoot이 게임플레이 로드
        if (IsLoaded) return;                     // 이미 로드됨
        SceneManager.LoadScene(SceneName, LoadSceneMode.Additive);
    }
}
