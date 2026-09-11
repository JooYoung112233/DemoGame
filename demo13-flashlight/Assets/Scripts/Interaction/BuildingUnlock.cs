using UnityEngine;

/// <summary>
/// 잠긴 상가의 **해금** — 조건이 차면 셔터를 걷고 들어갈 수 있게 한다.
///
/// 마을 상가 4채(수리점·의료소·가구점·암시장)는 처음부터 실내까지 다 지어져 있고
/// 입구만 셔터로 막혀 있다(safehouse.md §잠금 건물). 해금은 **셔터를 치우는 것**이
/// 전부다 — 건물을 새로 짓거나 씬을 갈아끼우지 않는다. 그래야 해금 순간이
/// "닫혀 있던 가게가 열렸다"로 읽힌다.
///
/// 조건은 <see cref="QuestManager"/>의 플래그 하나다. 스토리·퀘스트가 그 플래그를
/// 세우면(예: <c>shop_repair_unlocked</c>) 여기서 알아채고 연다.
///
/// ⚠️ 세이브는 플래그 쪽이 책임진다. 이 컴포넌트는 상태를 따로 들고 있지 않고
///    매번 플래그를 읽어 맞춘다 — 그래야 로드 직후에도 열린 가게가 열린 채로 뜬다.
///
/// 설계: docs/safehouse.md, docs/building-interior.md
/// </summary>
public class BuildingUnlock : MonoBehaviour
{
    [Tooltip("이 플래그가 서면 열린다. QuestManager.SetFlag(...)로 세운다.")]
    [SerializeField] string unlockFlag;

    [Tooltip("걷어낼 셔터. 비우면 이름이 'Shutter'로 시작하는 자식을 자동 수집.")]
    [SerializeField] GameObject shutter;

    [Tooltip("열렸을 때만 켤 것들(주인 NPC·실내 조명 등). 비어도 무방.")]
    [SerializeField] GameObject[] revealOnUnlock;

    [Tooltip("플래그를 다시 확인하는 주기(초). 플래그에 이벤트가 없어 폴링한다.")]
    [SerializeField] float pollInterval = 0.5f;

    float _next;
    bool _open;

    public bool IsOpen => _open;
    public string UnlockFlag => unlockFlag;

    void Awake()
    {
        if (shutter == null)
        {
            foreach (Transform t in transform)
                if (t.name.StartsWith("Shutter")) { shutter = t.gameObject; break; }
        }
        Apply(Resolve());          // 시작 상태를 즉시 맞춘다(로드 직후 깜빡임 방지)
    }

    void Update()
    {
        if (Time.unscaledTime < _next) return;
        _next = Time.unscaledTime + Mathf.Max(0.1f, pollInterval);

        bool want = Resolve();
        if (want != _open) Apply(want);
    }

    bool Resolve()
    {
        if (string.IsNullOrEmpty(unlockFlag)) return false;   // 플래그가 없으면 영영 잠금
        var qm = QuestManager.Instance;
        return qm != null && qm.GetFlag(unlockFlag);
    }

    void Apply(bool open)
    {
        _open = open;
        if (shutter != null) shutter.SetActive(!open);
        foreach (var go in revealOnUnlock)
            if (go != null) go.SetActive(open);
    }

    /// <summary>빌더에서 배선할 때 쓴다.</summary>
    public void Configure(string flag, GameObject shutterGo, params GameObject[] reveal)
    {
        unlockFlag = flag;
        shutter = shutterGo;
        revealOnUnlock = reveal;
    }

    /// <summary>테스트·디버그용 강제 해금 — 플래그를 세워 정식 경로로 연다.</summary>
    public void ForceUnlock()
    {
        if (string.IsNullOrEmpty(unlockFlag)) return;
        if (QuestManager.Instance != null) QuestManager.Instance.SetFlag(unlockFlag);
        Apply(true);
    }
}
