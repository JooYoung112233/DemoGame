using UnityEditor;
using UnityEngine;
using Spine.Unity;

/// <summary>
/// PlayerRig 프리팹에 Spine 캐릭터(cha)를 적용한다.
/// - cha_SkeletonData로 SkeletonAnimation 자식("PlayerSpine") 생성/갱신 (멱등 — 다시 실행하면 재생성)
/// - 크기를 기존 플레이어 키에 맞추고, 정렬(Sorting Layer/Order)을 기존 PlayerSprite와 동일하게
/// - 기존 PlayerSprite(SpriteRenderer)는 "비표시로 유지" — HitFlash/InjuryVFX/HideoutController가
///   자식 SpriteRenderer를 스캔하므로 오브젝트 자체는 남겨야 한다 (렌더러만 off)
/// - TopDownPlayer.skeletonAnimation 참조 자동 연결
///
/// 메뉴: Tools ▸ TopDown ▸ 초기설정 ▸ Spine 플레이어 적용 (cha)
/// 선행조건: Spine 런타임 4.2에서 cha 리임포트가 끝나 cha_SkeletonData가 정상 로드되어야 한다.
/// </summary>
public static class SpinePlayerSetup
{
    const string RigPath  = "Assets/Resources/PlayerRig.prefab";
    const string DataPath = "Assets/Resources/Charater/cha_SkeletonData.asset";
    const string SpineChildName = "PlayerSpine";
    const float  TargetHeight = 2.2f;          // 월드 유닛 기준 캐릭터 키 (기존 PlayerSprite ≈ 2.5)
    const string DefaultAnim  = "walk";        // cha엔 idle이 없어 정지 시엔 런타임이 셋업 포즈로 둠

    [MenuItem("Tools/TopDown/초기설정/Spine 플레이어 적용 (cha)")]
    static void RunMenu() => Apply(true);

    /// <summary>
    /// PlayerRig.prefab에 Spine 캐릭터(cha)를 적용한다(멱등). 시스템 씬 빌더가 빌드 시 자동 호출.
    /// cha SkeletonData가 없거나 로드 실패면 아무것도 바꾸지 않고 false 반환(기존 스프라이트 유지).
    /// </summary>
    /// <param name="interactive">true면 실패 시 대화상자, false면 콘솔 경고만(빌더용).</param>
    public static bool Apply(bool interactive)
    {
        var data = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(DataPath);
        if (data == null)
        {
            string msg = $"SkeletonDataAsset를 못 찾았습니다: {DataPath} — Spine 런타임이 4.2인지, cha를 Reimport 했는지 확인하세요.";
            if (interactive) EditorUtility.DisplayDialog("Spine 플레이어", msg, "확인");
            else Debug.LogWarning("[SpinePlayer] " + msg + " (Spine 적용 건너뜀)");
            return false;
        }

        var skelData = data.GetSkeletonData(true);
        if (skelData == null)
        {
            string msg = "SkeletonData 로드 실패 — Console의 임포트/버전 에러 확인. " +
                "('Data version 4.2.43 / Required 4.3'가 남아있으면 런타임 교체 미반영, atlasAssets 비어있으면 .atlas.txt 미인식)";
            if (interactive) EditorUtility.DisplayDialog("Spine 플레이어", msg, "확인");
            else Debug.LogWarning("[SpinePlayer] " + msg + " (Spine 적용 건너뜀)");
            return false;
        }

        var root = PrefabUtility.LoadPrefabContents(RigPath);
        if (root == null)
        {
            Debug.LogError($"[SpinePlayer] PlayerRig 프리팹을 못 열었습니다: {RigPath}");
            return false;
        }

        try
        {
            var player = root.GetComponentInChildren<TopDownPlayer>(true);
            if (player == null)
            {
                Debug.LogError("[SpinePlayer] 프리팹에서 TopDownPlayer를 못 찾았습니다.");
                return false;
            }
            Transform playerT = player.transform;

            // 멱등: 기존 PlayerSpine 제거 후 재생성
            var existing = playerT.Find(SpineChildName);
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var go = new GameObject(SpineChildName);
            go.transform.SetParent(playerT, false);
            go.layer = playerT.gameObject.layer;

            var skelAnim = SkeletonAnimation.AddToGameObject(go, data);
            if (HasAnim(skelData, DefaultAnim))
            {
                skelAnim.AnimationName = DefaultAnim;
                skelAnim.loop = true;
            }
            skelAnim.Initialize(true);

            // 크기: 데이터 적용 키(Height) 기준으로 목표 키에 맞춤
            float h = skelData.Height > 0.01f ? skelData.Height : 1f;
            float s = TargetHeight / h;
            go.transform.localScale = new Vector3(s, s, 1f);

            // 정렬: 기존 바디 SpriteRenderer와 동일 레이어/오더
            var mr = go.GetComponent<MeshRenderer>();
            var oldSprite = FindBodySprite(playerT);
            if (mr != null && oldSprite != null)
            {
                mr.sortingLayerID = oldSprite.sortingLayerID;
                mr.sortingOrder   = oldSprite.sortingOrder;
            }

            // 기존 스프라이트는 렌더러만 끔 (오브젝트는 유지 — 다른 시스템이 자식 SR 스캔)
            if (oldSprite != null) oldSprite.enabled = false;

            // TopDownPlayer 참조 연결
            var so = new SerializedObject(player);
            var prop = so.FindProperty("skeletonAnimation");
            if (prop != null)
            {
                prop.objectReferenceValue = skelAnim;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else Debug.LogWarning("[SpinePlayer] TopDownPlayer.skeletonAnimation 필드를 못 찾음 — 스크립트 컴파일 확인.");

            PrefabUtility.SaveAsPrefabAsset(root, RigPath);
            Debug.Log($"[SpinePlayer] PlayerRig에 Spine 캐릭터 적용 완료 — 키 {TargetHeight} (localScale {s:F4}), " +
                      $"기본 애니 '{(HasAnim(skelData, DefaultAnim) ? DefaultAnim : "(없음)")}'. " +
                      "크기/정렬이 어색하면 PlayerSpine의 Scale을 조정하세요.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return true;
    }

    static bool HasAnim(Spine.SkeletonData d, string name) => d.FindAnimation(name) != null;

    /// <summary>바 / 그림자 / 오버레이가 아닌 첫 바디 SpriteRenderer.</summary>
    static SpriteRenderer FindBodySprite(Transform playerT)
    {
        foreach (var sr in playerT.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr == null) continue;
            string nm = sr.gameObject.name;
            if (nm.Contains("Bar") || nm.Contains("Shadow") || nm.Contains("Overlay")) continue;
            return sr;
        }
        return null;
    }
}
