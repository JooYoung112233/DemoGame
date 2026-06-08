using UnityEngine;

/// <summary>
/// 범용 스토리 영역 트리거.
/// Collider(isTrigger=true) 위에 배치.
/// 플레이어 진입 시 StoryTriggerManager 이벤트 호출 또는 직접 씬 재생.
///
/// 사용 예:
///   - 지하창고 진입 → triggerType = Basement
///   - 특정 구역 진입 → triggerType = CustomFlag, customFlagName = "some_flag"
///   - 직접 씬 재생 → triggerType = PlayScene, storySceneId = "S-015_BASEMENT"
/// </summary>
[RequireComponent(typeof(Collider))]
public class StoryAreaTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        Basement,       // 지하창고 진입 (StoryTriggerManager.OnBasementEnter)
        CustomFlag,     // 커스텀 플래그 설정 + 자동 트리거 체크
        PlayScene,      // 직접 스토리 씬 재생
    }

    [Header("── 트리거 설정 ──")]
    [SerializeField] TriggerType triggerType = TriggerType.Basement;

    [Tooltip("CustomFlag 타입: 설정할 플래그 이름")]
    [SerializeField] string customFlagName;

    [Tooltip("PlayScene 타입: 재생할 스토리 씬 ID")]
    [SerializeField] string storySceneId;

    [Tooltip("true면 1회만 작동")]
    [SerializeField] bool oneShot = true;

    bool triggered;

    void OnTriggerEnter(Collider other)
    {
        if (triggered && oneShot) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;
        Execute();
    }

    void Execute()
    {
        switch (triggerType)
        {
            case TriggerType.Basement:
                if (StoryTriggerManager.Instance != null)
                    StoryTriggerManager.Instance.OnBasementEnter();
                break;

            case TriggerType.CustomFlag:
                if (!string.IsNullOrEmpty(customFlagName) && QuestManager.Instance != null)
                {
                    QuestManager.Instance.SetFlag(customFlagName);
                    if (StoryPlayer.Instance != null)
                        StoryPlayer.Instance.CheckAutoTriggers();
                }
                break;

            case TriggerType.PlayScene:
                if (!string.IsNullOrEmpty(storySceneId) && StoryPlayer.Instance != null)
                    StoryPlayer.Instance.PlayScene(storySceneId);
                break;
        }
    }
}
