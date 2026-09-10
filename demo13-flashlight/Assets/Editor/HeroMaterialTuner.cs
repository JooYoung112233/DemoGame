#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 캐릭터 머티리얼의 **재질 분리**를 이름 기준으로 다시 칠한다 — 천/가죽/금속/피부/눈.
///
/// 왜 필요한가 (2026-09-10 사용자 지적: "옷이 코팅된 덩어리처럼 보인다, 가죽·금속은 따로 조절해야") —
/// 저작된 머티리얼은 전부 `_Smoothness = 1`, `_Metallic = 1`로 들어와 있고, 실제 값은 **하나의
/// 공유 마스크맵**이 정한다(실측: 마스크 A(스무스니스) 0.14~0.40, R(메탈릭) 평균 0.007·최대 0.65).
/// 그래서 천이든 가죽이든 금속이든 **같은 곡선**을 타고, 재질이 서로 갈리지 않는다.
/// 스칼라 `_Smoothness`/`_Metallic`은 마스크에 **곱해지는** 값이라, 여기서 역할별로 눌러 주면
/// 마스크가 만든 결은 살리면서 재질 구분만 붙는다.
///
/// 왜 .mat을 손으로 안 고치나 — 이 머티리얼들은 `tools/*.py`가 FBX를 다시 뽑을 때 같이
/// 재생성된다. 손으로 고치면 다음 익스포트에 날아간다. 이 도구는 **몇 번 돌려도 같은 결과**라
/// 재익스포트 뒤에 다시 실행하면 된다.
///
/// 값을 바꾸려면 아래 표를 고치고 메뉴를 다시 실행한다.
/// </summary>
public static class HeroMaterialTuner
{
    /// <summary>재질 역할. 값은 마스크맵에 곱해지는 스칼라다.</summary>
    struct Role
    {
        public string name;
        public float smoothness;   // 마스크 A에 곱해진다
        public float metallic;     // 마스크 R에 곱해진다
        public Role(string n, float s, float m) { name = n; smoothness = s; metallic = m; }
    }

    // 이름 조각 → 역할. 위에서부터 **먼저 맞는 것**을 쓴다(금속을 천보다 앞에 둘 것).
    static readonly (string key, Role role)[] Table =
    {
        // 눈은 유일하게 반짝여야 하는 곳이다. 여기가 매트하면 얼굴이 죽는다.
        ("iris",        new Role("눈",    0.80f, 0f)),
        ("pupil",       new Role("눈",    0.80f, 0f)),
        ("white",       new Role("눈",    0.70f, 0f)),

        ("weaponsteel", new Role("금속",  0.85f, 1f)),
        ("metal",       new Role("금속",  0.80f, 1f)),
        ("buckle",      new Role("금속",  0.80f, 1f)),
        ("lamp",        new Role("금속",  0.65f, 1f)),

        ("leather",     new Role("가죽",  0.50f, 0f)),
        ("boots",       new Role("가죽",  0.45f, 0f)),
        ("sole",        new Role("고무",  0.28f, 0f)),

        // 피부는 살짝만. 완전 매트면 점토처럼 보인다.
        ("skin",        new Role("피부",  0.30f, 0f)),
        ("face",        new Role("피부",  0.30f, 0f)),
        ("ear",         new Role("피부",  0.30f, 0f)),
        ("nose",        new Role("피부",  0.30f, 0f)),
        ("mouth",       new Role("피부",  0.35f, 0f)),

        ("hair",        new Role("머리",  0.38f, 0f)),

        // 천 — 이 게임 옷의 대부분. 가장 낮게.
        ("jacket",      new Role("천",    0.22f, 0f)),
        ("trouser",     new Role("천",    0.22f, 0f)),
        ("cap",         new Role("천",    0.22f, 0f)),
        ("brim",        new Role("천",    0.25f, 0f)),
        ("cuff",        new Role("천",    0.22f, 0f)),
        ("scarf",       new Role("천",    0.20f, 0f)),
        ("lapel",       new Role("천",    0.22f, 0f)),
        ("flap",        new Role("천",    0.22f, 0f)),
        ("patch",       new Role("천",    0.22f, 0f)),
        ("under",       new Role("천",    0.22f, 0f)),
        ("pack",        new Role("천",    0.26f, 0f)),
        ("wrap",        new Role("천",    0.22f, 0f)),
        ("pants",       new Role("천",    0.22f, 0f)),
        ("hood",        new Role("천",    0.22f, 0f)),
        ("mask",        new Role("천",    0.22f, 0f)),
        ("belt",        new Role("가죽",  0.45f, 0f)),

        ("batwood",     new Role("나무",  0.30f, 0f)),
        ("wood",        new Role("나무",  0.30f, 0f)),

        // 잉크(눈썹·라인)·발광 액센트는 반사가 붙으면 그림이 깨진다. 완전 매트로.
        ("ink",         new Role("라인",  0.05f, 0f)),
        ("accent",      new Role("발광",  0.10f, 0f)),
    };

    static readonly string[] Folders =
    {
        "Assets/ChibiSurvivor/Player",
        "Assets/ChibiSurvivor/Bandit",
    };

    [MenuItem("Tools/TopDown/개발/캐릭터 재질 분리 (천·가죽·금속)")]
    public static void Apply()
    {
        var sb = new StringBuilder();
        var counts = new Dictionary<string, int>();
        int touched = 0, skipped = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Material", Folders))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) continue;
            // URP/Lit 계열만 — 커스텀 셰이더는 프로퍼티 이름이 달라 건드리면 안 된다.
            if (!m.HasProperty("_Smoothness") || !m.HasProperty("_Metallic")) { skipped++; continue; }

            string lower = m.name.ToLowerInvariant();
            bool hit = false;
            foreach (var (key, role) in Table)
            {
                if (!lower.Contains(key)) continue;
                m.SetFloat("_Smoothness", role.smoothness);
                m.SetFloat("_Metallic", role.metallic);
                EditorUtility.SetDirty(m);
                counts[role.name] = counts.TryGetValue(role.name, out var c) ? c + 1 : 1;
                touched++; hit = true;
                break;
            }
            if (!hit) { skipped++; sb.AppendLine("  분류 안 됨: " + m.name + "  (" + path + ")"); }
        }

        AssetDatabase.SaveAssets();

        var head = new StringBuilder();
        head.Append("[재질 분리] 머티리얼 " + touched + "개 조정, " + skipped + "개 건너뜀 — ");
        foreach (var kv in counts) head.Append(kv.Key + " " + kv.Value + "개, ");
        Debug.Log(head.ToString().TrimEnd(' ', ',') + "\n" + sb);

        if (!ContentBuildAll.Quiet)
            EditorUtility.DisplayDialog("캐릭터 재질 분리",
                $"{touched}개 조정 / {skipped}개 건너뜀.\n\n모델을 다시 익스포트하면 이 메뉴를 다시 실행할 것.", "확인");
    }
}
#endif
