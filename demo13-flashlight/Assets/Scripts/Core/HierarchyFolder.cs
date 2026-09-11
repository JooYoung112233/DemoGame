using UnityEngine;

/// <summary>
/// 하이어라키 정리용 **폴더 표시**. 로직은 없다 — "이 오브젝트는 묶음일 뿐"이라는 표식이다.
/// `SceneHierarchyOrganizer`(에디터)가 씬을 기능별로 묶을 때 만들고, 다시 돌릴 때 이걸로 폴더를 알아본다.
/// 규약: docs/architecture.md §씬 하이어라키 규약.
///
/// ⚠️ 폴더가 끼면 **`DontDestroyOnLoad`가 조용히 안 먹는다.** Unity는 루트가 아닌 오브젝트엔
///    DDOL을 무시하고 경고만 낸다. Systems 씬의 매니저들은 원래 루트에 있다가 Awake에서 DDOL 씬으로
///    옮겨졌는데, 폴더 안에 넣으면 그게 끊긴다. 그래서 DDOL은 <see cref="Persist"/>로 부른다 —
///    부모가 "영속 폴더"면 먼저 루트로 빼고 DDOL을 건다. 결과적으로 **런타임 동작은 폴더가 없던
///    때와 똑같고**, 에디터에서만 정리돼 보인다.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("")]
public sealed class HierarchyFolder : MonoBehaviour
{
    [Tooltip("켜면 이 폴더 바로 아래 오브젝트가 Persist(DDOL)를 부를 때 루트로 빠져나간다. Systems 씬 폴더만 켠다.\n"
           + "맵 씬 폴더는 끈다 — 맵 안의 오브젝트는 원래 루트가 아니었으니 DDOL이 무시되던 그대로 둔다.")]
    public bool detachOnPersist;

    /// <summary>폴더를 건너뛴 **소유 루트** — 폴더가 없던 시절의 <c>transform.root</c>에 해당한다.
    /// 부모를 타고 올라가다 폴더(또는 씬 루트)를 만나면 멈춘다.</summary>
    public static Transform OwnerRoot(Transform t)
    {
        while (t.parent != null && t.parent.GetComponent<HierarchyFolder>() == null) t = t.parent;
        return t;
    }

    /// <summary><c>DontDestroyOnLoad(go)</c> 대신 부른다. 영속 폴더 안이면 루트로 뺀 뒤 건다.
    /// 그 외엔 <c>DontDestroyOnLoad</c>와 완전히 같다(루트가 아니면 Unity가 무시하는 것까지).</summary>
    public static void Persist(GameObject go)
    {
        var p = go.transform.parent;
        if (p != null && p.TryGetComponent(out HierarchyFolder f) && f.detachOnPersist)
            go.transform.SetParent(null, true);
        DontDestroyOnLoad(go);
    }
}
