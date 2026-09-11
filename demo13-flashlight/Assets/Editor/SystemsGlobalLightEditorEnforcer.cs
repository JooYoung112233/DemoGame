#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 런타임 <see cref="SystemsSceneEnforcer"/>의 "글로벌 라이트 중복 제거"를 <b>에디트 모드</b>에 그대로 적용.
///
/// URP 2D는 같은 sorting layer에 활성 Global Light2D가 2개 이상이면
/// "More than one global light on layer ..." 경고를 repaint마다 뱉는다.
/// 런타임 enforcer는 <c>sceneLoaded</c>에만 반응해 에디터(씬뷰)에선 안 돈다 →
/// Systems 씬 + 게임플레이/샌드박스 씬을 함께 열면(각자 글로벌 보유) 경고가 쏟아진다.
///
/// 이 에디터 보조는 Systems 씬이 로드돼 있으면 그 글로벌만 남기고 다른 씬의 글로벌을 끈다(에디트 모드 한정).
/// Systems가 언로드되면 우리가 껐던 것을 되살린다(단독 씬 편집 존중). 씬을 강제 저장하진 않는다 —
/// 끈 라이트 때문에 해당 씬에 dirty(*) 표시가 잠깐 뜰 수 있으나 저장 안 하면 무해.
/// Play 중에는 손대지 않는다(런타임 enforcer가 담당).
/// </summary>
[InitializeOnLoad]
static class SystemsGlobalLightEditorEnforcer
{
    static readonly HashSet<Light2D> _disabled = new HashSet<Light2D>();

    static SystemsGlobalLightEditorEnforcer()
    {
        EditorApplication.hierarchyChanged += Enforce;
        EditorSceneManager.sceneOpened += (s, m) => Enforce();
        EditorSceneManager.sceneClosed += (s) => Enforce();
        Enforce();
    }

    static void Enforce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; // 런타임은 SystemsSceneEnforcer 담당

        _disabled.RemoveWhere(l => l == null);

        var all = Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        var systems = SceneManager.GetSceneByName(SystemsScene.SceneName);
        bool systemsLoaded = systems.IsValid() && systems.isLoaded;

        // 유지할 단 하나의 글로벌(owner) 선정 — 우선순위:
        //  1) Systems 씬의 글로벌(있으면)  2) 현재 활성 글로벌  3) 비활성 포함 첫 글로벌
        // Systems가 없어도 단독 씬에 글로벌이 2개 이상이면 1개만 남겨 "More than one global light" 경고를 막는다.
        Light2D owner = null;
        if (systemsLoaded)
            foreach (var l in all)
                if (IsGlobal(l) && l.gameObject.scene == systems) { owner = l; break; }
        if (owner == null)
            foreach (var l in all)
                if (IsGlobal(l) && l.enabled) { owner = l; break; }
        if (owner == null)
            foreach (var l in all)
                if (IsGlobal(l)) { owner = l; break; }
        if (owner == null) { Restore(); return; }    // 글로벌이 아예 없음 → 우리가 껐던 것 복구

        if (!owner.enabled) owner.enabled = true;     // owner는 항상 켜둠
        _disabled.Remove(owner);

        foreach (var l in all)
        {
            if (!IsGlobal(l) || l == owner) continue;
            if (l.enabled) { l.enabled = false; _disabled.Add(l); }   // 나머지 글로벌은 끔(경고 방지)
        }
    }

    static bool IsGlobal(Light2D l) => l != null && l.lightType == Light2D.LightType.Global;

    static void Restore()
    {
        foreach (var l in _disabled) if (l != null) l.enabled = true;
        _disabled.Clear();
    }
}
#endif
