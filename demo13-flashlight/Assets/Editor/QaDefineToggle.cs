using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

/// <summary>
/// QA 자동화(`Assets/Scripts/QA/`, `Game.QA` 어셈블리)를 켜고 끈다.
///
/// 2026-09-09 볼륨 축소(docs/scope-cut.md 6번): QA 봇 4,215줄이 게임 런타임 어셈블리에
/// 그대로 들어가 있었다. `Game.QA.asmdef`의 defineConstraints=["QA_ENABLED"]로 떼어내서
/// **평소 빌드에는 한 줄도 안 들어가고**, QA를 돌릴 때만 이 메뉴로 켠다.
///
/// 삭제가 아니라 분리인 이유: QaBot은 빌드된 게임 안에서 돌아야 하므로(-qa-serve)
/// 에디터 전용 어셈블리로는 옮길 수 없고, 지우면 /qa · /qa-loop 워크플로가 통째로 죽는다.
/// </summary>
public static class QaDefineToggle
{
    const string Symbol = "QA_ENABLED";
    const string MenuPath = "Tools/TopDown/QA/QA 자동화 켜기 (QA_ENABLED)";

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

        PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
        var list = new System.Collections.Generic.List<string>(defines);

        bool on = list.Contains(Symbol);
        if (on) list.Remove(Symbol); else list.Add(Symbol);

        PlayerSettings.SetScriptingDefineSymbols(target, list.ToArray());
        Debug.Log($"[QA] {Symbol} {(on ? "해제" : "설정")} — 컴파일이 끝나면 QA 스크립트가 " +
                  $"{(on ? "빠집니다" : "들어옵니다")}.");
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        var target = NamedBuildTarget.FromBuildTargetGroup(
            BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
        PlayerSettings.GetScriptingDefineSymbols(target, out var defines);
        Menu.SetChecked(MenuPath, System.Array.IndexOf(defines, Symbol) >= 0);
        return true;
    }
}
