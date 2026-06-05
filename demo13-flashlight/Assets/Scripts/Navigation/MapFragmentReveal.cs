using UnityEngine;

/// <summary>
/// 지도 아이템 — 획득/상호작용 시 미니맵 구역을 해금(fog 해제).
/// InteractableObject(Pickup/Note 등)와 함께 부착하면 상호작용 시 자동 발동.
/// 또는 외부에서 Reveal() 직접 호출.
/// 설계: docs/navigation.md §3.2 (fog 베이스 + 지도 조각 해금)
/// </summary>
public class MapFragmentReveal : MonoBehaviour
{
    [Tooltip("해금할 구역 id 목록. 비우거나 revealEntireMap이면 전체 해금.")]
    [SerializeField] string[] revealZoneIds;
    [SerializeField] bool revealEntireMap = false;
    [SerializeField] string toastMessage = "지도 조각을 확보했다";

    InteractableObject _io;

    void Start()
    {
        _io = GetComponent<InteractableObject>();
        if (_io != null) _io.OnInteracted += OnInteracted;
    }

    void OnDestroy()
    {
        if (_io != null) _io.OnInteracted -= OnInteracted;
    }

    void OnInteracted(GameObject _) => Reveal();

    public void Reveal()
    {
        var m = RaidMapManager.Instance;
        if (m == null) return;

        if (revealEntireMap || revealZoneIds == null || revealZoneIds.Length == 0)
            m.RevealAll();
        else
            foreach (var id in revealZoneIds) m.RevealZone(id);

        if (!string.IsNullOrEmpty(toastMessage))
            ToastManager.Show(toastMessage, ToastManager.ToastType.Success);
    }
}
