using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 스토리 씬 재생기.
/// JSON 스크립트를 로드하고 노드를 순서대로 실행.
/// NarrationUI, DialogueUI, TutorialPrompt, ScreenEffectManager 등을 오케스트레이션.
///
/// 사용:
///   StoryPlayer.Instance.PlayScene("S-000");           // 특정 씬 재생
///   StoryPlayer.Instance.PlayScene("S-000", onDone);   // 콜백 포함
///   StoryPlayer.Instance.CheckAutoTriggers();           // 플래그 기반 자동 트리거 체크
/// </summary>
public class StoryPlayer : MonoBehaviour
{
    public static StoryPlayer Instance { get; private set; }

    public bool IsPlaying => isPlaying;

    // 로드된 스크립트
    Dictionary<string, StoryScene> scenes = new Dictionary<string, StoryScene>();
    HashSet<string> playedScenes = new HashSet<string>();

    // 재생 상태
    bool isPlaying;
    StoryScene currentScene;
    int nodeIndex;
    System.Action onSceneComplete;
    Coroutine playCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadAllScripts();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ═══════════════════════════
    //  스크립트 로드
    // ═══════════════════════════

    void LoadAllScripts()
    {
        var assets = Resources.LoadAll<TextAsset>("Story/Scripts");
        foreach (var asset in assets)
        {
            var script = JsonUtility.FromJson<StoryScript>(asset.text);
            if (script?.scenes == null) continue;

            foreach (var scene in script.scenes)
            {
                if (!string.IsNullOrEmpty(scene.id))
                    scenes[scene.id] = scene;
            }
        }
        Debug.Log($"[StoryPlayer] {scenes.Count}개 씬 로드됨.");
    }

    // ═══════════════════════════
    //  씬 재생
    // ═══════════════════════════

    /// <summary>
    /// 특정 스토리 씬을 재생.
    /// </summary>
    /// <summary>해당 씬이 로드돼 있는지(스토리 스크립트에 정의됐는지). 없으면 NPC 대화가 스토리로 먹히지 않게 가드용.</summary>
    public bool HasScene(string sceneId) => scenes != null && scenes.ContainsKey(sceneId);

    public void PlayScene(string sceneId, System.Action onComplete = null)
    {
        if (!scenes.TryGetValue(sceneId, out var scene))
        {
            Debug.LogWarning($"[StoryPlayer] 씬 없음: {sceneId}");
            onComplete?.Invoke();
            return;
        }

        // oneShot 체크
        if (scene.oneShot && playedScenes.Contains(sceneId))
        {
            onComplete?.Invoke();
            return;
        }

        if (isPlaying)
        {
            Debug.LogWarning($"[StoryPlayer] 이미 재생 중. {sceneId} 무시.");
            onComplete?.Invoke();
            return;
        }

        currentScene = scene;
        nodeIndex = 0;
        onSceneComplete = onComplete;
        isPlaying = true;

        if (playCoroutine != null) StopCoroutine(playCoroutine);
        playCoroutine = StartCoroutine(PlayNodes());
    }

    /// <summary>
    /// 플래그 기반 자동 트리거 체크.
    /// 안전가옥 진입, 휴식 후 등에 호출.
    /// </summary>
    public void CheckAutoTriggers()
    {
        if (isPlaying) return;
        if (QuestManager.Instance == null) return;

        foreach (var kvp in scenes)
        {
            var scene = kvp.Value;
            if (string.IsNullOrEmpty(scene.triggerFlag)) continue;
            if (scene.oneShot && playedScenes.Contains(scene.id)) continue;
            if (!QuestManager.Instance.GetFlag(scene.triggerFlag)) continue;

            // 완료 후 재귀 체크 — 연속된 자동 씬(S-007→S-008 등)을 끊김 없이 연쇄.
            // 모든 자동 씬은 oneShot이라 playedScenes 가드로 무한 루프 방지.
            PlayScene(scene.id, CheckAutoTriggers);
            return; // 한 번에 하나만
        }
    }

    // ═══════════════════════════
    //  노드 실행 루프
    // ═══════════════════════════

    IEnumerator PlayNodes()
    {
        while (nodeIndex < currentScene.nodes.Length)
        {
            var node = currentScene.nodes[nodeIndex];
            yield return ExecuteNode(node);
            nodeIndex++;
        }

        // 씬 완료
        if (currentScene.oneShot)
            playedScenes.Add(currentScene.id);

        isPlaying = false;
        currentScene = null;

        var callback = onSceneComplete;
        onSceneComplete = null;
        callback?.Invoke();
    }

    IEnumerator ExecuteNode(StoryNode node)
    {
        switch (node.type)
        {
            case "narration":
                yield return ExecuteNarration(node);
                break;
            case "dialogue":
                yield return ExecuteDialogue(node);
                break;
            case "choice":
                yield return ExecuteChoice(node);
                break;
            case "tutorial":
                ExecuteTutorial(node);
                break;
            case "effect":
                yield return ExecuteEffect(node);
                break;
            case "system":
                ExecuteSystem(node);
                break;
            case "condition":
                ExecuteCondition(node);
                break;
            default:
                Debug.LogWarning($"[StoryPlayer] 알 수 없는 노드 타입: {node.type}");
                break;
        }
    }

    // ═══════════════════════════
    //  노드 타입별 실행
    // ═══════════════════════════

    IEnumerator ExecuteNarration(StoryNode node)
    {
        if (NarrationUI.Instance == null || node.textKeys == null) yield break;

        var lines = StoryLocale.Instance.GetAll(node.textKeys);
        bool done = false;
        NarrationUI.Instance.Show(lines, () => done = true);

        while (!done) yield return null;
    }

    IEnumerator ExecuteDialogue(StoryNode node)
    {
        if (node.textKeys == null) yield break;

        string speakerName = "";
        if (!string.IsNullOrEmpty(node.speakerKey))
            speakerName = StoryLocale.Instance.Get(node.speakerKey);

        var lines = StoryLocale.Instance.GetAll(node.textKeys);

        // DialogueUI의 스토리 모드 사용
        if (DialogueUI.Instance != null)
        {
            bool done = false;
            DialogueUI.Instance.ShowStoryDialogue(speakerName, lines, () => done = true);
            while (!done) yield return null;
        }
        else
        {
            // DialogueUI가 없으면 NarrationUI로 대체
            bool done = false;
            NarrationUI.Instance?.Show(lines, () => done = true);
            while (!done) yield return null;
        }
    }

    IEnumerator ExecuteChoice(StoryNode node)
    {
        if (node.options == null || node.options.Length == 0) yield break;
        if (DialogueUI.Instance == null) yield break;

        // 선택지 텍스트 변환
        var choiceTexts = new string[node.options.Length];
        for (int i = 0; i < node.options.Length; i++)
            choiceTexts[i] = StoryLocale.Instance.Get(node.options[i].textKey);

        int selectedIndex = -1;
        DialogueUI.Instance.ShowStoryChoices(choiceTexts, (idx) => selectedIndex = idx);

        while (selectedIndex < 0) yield return null;

        // 선택 효과 적용
        var chosen = node.options[selectedIndex];
        if (chosen.effects != null)
        {
            foreach (var eff in chosen.effects)
                ApplyChoiceEffect(eff);
        }

        // 선택 후 응답 대사
        if (chosen.responseKeys != null && chosen.responseKeys.Length > 0)
        {
            var responseLines = StoryLocale.Instance.GetAll(chosen.responseKeys);
            // 응답은 NPC 대사이므로 speakerKey는 직전 dialogue 노드의 것을 유지
            bool done = false;
            if (DialogueUI.Instance != null)
                DialogueUI.Instance.ShowStoryDialogue("", responseLines, () => done = true);
            else
                NarrationUI.Instance?.Show(responseLines, () => done = true);
            while (!done) yield return null;
        }
    }

    void ApplyChoiceEffect(StoryChoiceEffect eff)
    {
        if (eff == null) return;
        switch (eff.type)
        {
            case "affinity":
                NPCRelationshipManager.Instance?.ModifyAffinity(eff.target, eff.value);
                break;
            case "trust":
                NPCRelationshipManager.Instance?.ModifyTrust(eff.target, eff.value);
                break;
            case "fear":
                NPCRelationshipManager.Instance?.ModifyFear(eff.target, eff.value);
                break;
            case "flag":
                QuestManager.Instance?.SetFlag(eff.target, eff.value != 0);
                break;
        }
    }

    void ExecuteTutorial(StoryNode node)
    {
        if (TutorialPrompt.Instance == null) return;
        string text = StoryLocale.Instance.Get(node.textKey);
        float dur = node.duration > 0 ? node.duration : 4f;
        TutorialPrompt.Instance.Show(text, dur, node.tutId);
    }

    IEnumerator ExecuteEffect(StoryNode node)
    {
        if (ScreenEffectManager.Instance == null) yield break;

        var sem = ScreenEffectManager.Instance;
        float p1 = node.floatParam;
        float p2 = node.floatParam2;

        switch (node.effect)
        {
            case "fade_out":
                yield return sem.FadeOut(p1 > 0 ? p1 : 1f);
                break;
            case "fade_in":
                yield return sem.FadeIn(p1 > 0 ? p1 : 1f);
                break;
            case "shake":
                sem.ScreenShake(p1 > 0 ? p1 : 0.15f, p2 > 0 ? p2 : 0.3f);
                break;
            case "chromatic":
                sem.ChromaticPulse(p1 > 0 ? p1 : 1f, p2 > 0 ? p2 : 1.5f);
                break;
            case "freeze":
                sem.FreezeFrame(p1 > 0 ? p1 : 0.5f);
                yield return new WaitForSecondsRealtime(p1 > 0 ? p1 : 0.5f);
                break;
            case "flash":
                sem.WhiteFlash(p1 > 0 ? p1 : 0.3f);
                break;
            case "grayscale_on":
                sem.SetGrayscale(true);
                break;
            case "grayscale_off":
                sem.SetGrayscale(false);
                break;
            case "wait":
                yield return new WaitForSecondsRealtime(p1 > 0 ? p1 : 1f);
                break;
        }
    }

    void ExecuteSystem(StoryNode node)
    {
        switch (node.action)
        {
            case "set_flag":
                QuestManager.Instance?.SetFlag(node.param, true);
                break;
            case "clear_flag":
                QuestManager.Instance?.SetFlag(node.param, false);
                break;
            case "quest_accept":
                var questData = Resources.Load<QuestData>($"Data/Quests/{node.param}");
                if (questData != null)
                    QuestManager.Instance?.AcceptQuest(questData);
                else
                    Debug.LogWarning($"[StoryPlayer] 퀘스트 없음: {node.param}");
                break;
            case "quest_complete":
                // 스토리 주도 완료: 목표 진행도와 무관하게 강제 완료 (내러티브 확정)
                QuestManager.Instance?.CompleteQuest(node.param, true);
                break;
            case "give_item":
                var itemData = ItemDatabase.Get(node.param);
                if (itemData != null)
                {
                    int amount = node.intParam > 0 ? node.intParam : 1;
                    var player = GameObject.FindGameObjectWithTag("Player");
                    var inv = player?.GetComponent<PlayerInventory>();
                    if (inv != null)
                        inv.TryPickup(new ItemInstance(itemData, amount));
                }
                break;
            case "add_stat":
                AchievementManager.Instance?.AddStat(node.param, node.intParam > 0 ? node.intParam : 1);
                break;
            case "auto_save":
                SaveManager.Instance?.AutoSave();
                break;
        }
    }

    void ExecuteCondition(StoryNode node)
    {
        bool flagValue = QuestManager.Instance != null && QuestManager.Instance.GetFlag(node.flag);

        if (flagValue && node.ifTrueGoto >= 0)
        {
            nodeIndex = node.ifTrueGoto - 1; // -1 because loop will increment
        }
        else if (!flagValue)
        {
            if (node.ifFalseGoto >= 0)
                nodeIndex = node.ifFalseGoto - 1;
            else if (node.skipIfFalse)
                return; // 다음 노드로 진행 (스킵)
        }
    }

    // ═══════════════════════════
    //  세이브/로드
    // ═══════════════════════════

    public HashSet<string> GetPlayedScenes() => new HashSet<string>(playedScenes);
    public void SetPlayedScenes(HashSet<string> ids) { playedScenes = ids ?? new HashSet<string>(); }
}
