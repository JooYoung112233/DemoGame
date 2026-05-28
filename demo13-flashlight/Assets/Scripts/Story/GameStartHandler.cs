using UnityEngine;
using System.Collections;

/// <summary>
/// 게임 최초 시작 처리.
/// Safehouse 씬에 배치. 세이브 데이터가 없으면 프롤로그 재생.
/// 세이브가 있으면 로드 후 정상 진행.
/// </summary>
public class GameStartHandler : MonoBehaviour
{
    [Tooltip("true면 Start에서 자동 실행. false면 수동 호출 (메뉴 UI 등)")]
    [SerializeField] bool autoStart = true;

    [Tooltip("프롤로그 전 대기 시간(초)")]
    [SerializeField] float delayBeforePrologue = 0.5f;

    bool started;

    void Start()
    {
        if (autoStart)
            StartCoroutine(InitGame());
    }

    /// <summary>
    /// 외부에서 호출 가능 (타이틀 화면 → 새 게임 버튼 등).
    /// </summary>
    public void BeginNewGame()
    {
        if (started) return;
        StartCoroutine(InitGame());
    }

    IEnumerator InitGame()
    {
        if (started) yield break;
        started = true;

        // 싱글톤 초기화 대기 (GameBootstrap이 AfterSceneLoad에서 실행)
        yield return null;

        // 세이브 로드 시도
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            SaveManager.Instance.Load();
            Debug.Log("[GameStart] 세이브 로드 완료. 정상 진행.");

            // 자동 트리거 체크 (로드된 플래그 기반)
            if (StoryPlayer.Instance != null)
                StoryPlayer.Instance.CheckAutoTriggers();

            yield break;
        }

        // ── 새 게임: 프롤로그 재생 ──
        Debug.Log("[GameStart] 새 게임. 프롤로그 시작.");

        yield return new WaitForSecondsRealtime(delayBeforePrologue);

        if (StoryTriggerManager.Instance != null)
        {
            StoryTriggerManager.Instance.PlayPrologueAuto();
        }
        else if (StoryPlayer.Instance != null)
        {
            // 폴백: StoryTriggerManager 없이 직접 재생
            StoryPlayer.Instance.PlayScene("S-000");
        }
    }
}
