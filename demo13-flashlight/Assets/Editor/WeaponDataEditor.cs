using UnityEditor;

/// <summary>
/// WeaponData 인스펙터 — 근접이면 근접 칸만, 총이면 총 칸만 보여 준다
/// (2026-09-11 "근접/총 데이터 칸 분리", docs/combat.md §무기 구성 결정).
/// 안 보이는 칸의 값은 지우지 않는다 — 무기 종류를 바꿔도 되돌릴 수 있게.
/// </summary>
[CustomEditor(typeof(WeaponData))]
public class WeaponDataEditor : Editor
{
    static readonly string[] Common = { "weaponId", "isRanged", "moveSpeedMult" };
    static readonly string[] Melee  = { "useTwoHandSwordAnimations", "useTwoHandBatAnimations",
                                        "lightCombo", "heavyAttack", "heavyFullAttack", "staminaCostMult" };
    static readonly string[] Gun    = { "firearmStance", "caliber", "fireMode", "rpm", "damage", "groggy",
                                        "projectileSpeed", "effectiveRange", "hipSpreadDeg", "adsSpreadDeg",
                                        "recoilPerShot", "recoilRecover", "recoilMax", "reloadSeconds", "adsMoveMult" };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));

        Draw(Common);
        bool ranged = serializedObject.FindProperty("isRanged").boolValue;
        Draw(ranged ? Gun : Melee);
        if (ranged)
            EditorGUILayout.HelpBox("탄창 장탄수는 탄창 아이템, 탄 스펙은 탄약 아이템에 있다.\n" +
                                    "총기 밴딧은 StatDB 유닛(rangedWeaponData)이 이 에셋을 참조한다 — 적 전용 배율만 StatDB.",
                                    MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }

    void Draw(string[] names)
    {
        foreach (var n in names)
        {
            var p = serializedObject.FindProperty(n);
            if (p != null) EditorGUILayout.PropertyField(p, true);
        }
    }
}
