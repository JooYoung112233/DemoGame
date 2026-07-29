using UnityEngine;

/// <summary>
/// 탄창에 탄을 채우고 빼는 일. (docs/combat.md "총기 — 2026-07-29 결정")
///
/// 탄창을 아이템으로 만든 이상(타르코프식) **레이드 전 준비**가 루프의 일부가 된다 —
/// 낱알 탄약은 그 자체로 아무 쓸모가 없고, 탄창에 채워 넣어야 비로소 화력이 된다.
/// 이 과정이 없으면 장전은 영원히 "맞는 탄창 없음"이다.
/// </summary>
public static class GunAmmo
{
    /// <summary>인벤의 탄약으로 탄창을 채운다. 채운 발수를 돌려준다(0=못 채움).</summary>
    public static int FillMagazine(PlayerInventory inv, ItemInstance mag)
    {
        if (inv == null || mag == null || mag.data == null || !mag.data.IsMagazine) return 0;
        int need = mag.data.magCapacity - mag.ammoCount;
        if (need <= 0) return 0;

        // 이미 든 탄이 있으면 **같은 탄종만** 더 넣는다 — 한 탄창에 탄종이 섞이면
        // 데미지 배율이 무엇인지 말할 수 없게 된다(발마다 다른 탄을 추적하진 않는다).
        string lockedAmmo = mag.ammoCount > 0 ? mag.ammoItemId : null;

        int added = 0;
        foreach (var grid in Grids(inv))
        {
            if (grid == null) continue;
            foreach (var p in grid.GetAll())            // GetAll은 복사본 — 도중에 Remove해도 안전
            {
                if (added >= need) break;
                var it = p.item;
                if (it == null || it.data == null || !it.data.IsAmmo) continue;
                if (!Fits(mag.data, it.data)) continue;
                if (lockedAmmo != null && it.data.itemId != lockedAmmo) continue;

                int take = Mathf.Min(need - added, it.stackCount);
                if (take <= 0) continue;

                if (mag.ammoCount == 0) mag.ammoItemId = it.data.itemId;
                mag.ammoCount += take;
                it.stackCount -= take;
                added += take;
                lockedAmmo = mag.ammoItemId;

                if (it.stackCount <= 0) grid.Remove(p);
                else grid.NotifyChanged();
            }
            if (added >= need) break;
        }
        return added;
    }

    /// <summary>탄창을 비워 탄약을 인벤으로 되돌린다. 뺀 발수를 돌려준다.</summary>
    public static int UnloadMagazine(PlayerInventory inv, ItemInstance mag)
    {
        if (inv == null || mag == null || mag.data == null || !mag.data.IsMagazine) return 0;
        if (mag.ammoCount <= 0) return 0;
        var ammo = ItemDatabase.Get(mag.ammoItemId);
        if (ammo == null) { mag.ammoCount = 0; mag.ammoItemId = null; return 0; }

        int left = mag.ammoCount;
        int returned = 0;
        // 스택 한도만큼 잘라 넣는다. 자리가 없으면 넣은 만큼만 빠지고 나머지는 탄창에 남는다.
        while (left > 0)
        {
            int chunk = Mathf.Min(left, Mathf.Max(1, ammo.maxStack));
            var inst = new ItemInstance(ammo, chunk);
            if (!inv.TryAutoPlaceAnywhere(inst)) break;
            left -= chunk;
            returned += chunk;
        }
        mag.ammoCount = left;
        if (mag.ammoCount <= 0) mag.ammoItemId = null;
        return returned;
    }

    /// <summary>이 탄약이 이 탄창에 들어가나(구경 일치). 탄창 구경이 비어 있으면 아무거나 받는다.</summary>
    public static bool Fits(ItemData mag, ItemData ammo)
    {
        if (mag == null || ammo == null || !ammo.IsAmmo) return false;
        if (string.IsNullOrEmpty(mag.magCaliber)) return true;
        return mag.magCaliber == ammo.ammoCaliber;
    }

    /// <summary>인벤 격자 3종(주머니 → 가방 → 보안). 주머니부터 훑어 손 가까운 것을 먼저 쓴다.</summary>
    public static InventoryGrid[] Grids(PlayerInventory inv)
        => inv == null ? new InventoryGrid[0]
                       : new[] { inv.PocketsGrid, inv.Grid, inv.SecureGrid };
}
