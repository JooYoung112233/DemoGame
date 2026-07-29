using UnityEngine;

/// <summary>
/// 플레이어 총기 — 사격 / 조준 / 장전. (docs/combat.md "총기 — 2026-07-29 결정")
///
/// 근접 상태기계(TopDownPlayer.CombatState)를 건드리지 않고 **옆에 붙는다**.
/// 장착 무기가 `WeaponData.isRanged`면 TopDownPlayer가 좌/우클릭·R을 여기로 넘긴다.
/// 근접 코드를 총기 분기로 오염시키면 애써 잡아 놓은 콤보·차징 타이밍이 같이 흔들린다.
///
/// 탄은 **총기 인스턴스**(`ItemInstance.ammoCount`)에 있다. 장전은 인벤의 탄창 인스턴스와
/// 그 값을 맞바꾸는 일이라, 반쯤 쓴 탄창을 빼도 남은 탄이 그대로 따라 나온다(타르코프식).
/// </summary>
[DisallowMultipleComponent]
public class PlayerGun : MonoBehaviour
{
    // ★ 지연 조회 — Awake에서 한 번만 캐시하면 그 시점에 아직 안 붙은 컴포넌트는 **영영 null**이다.
    //   (PlayerEquipment/PlayerInventory는 부트스트랩 순서에 따라 나중에 붙는 경우가 있다.)
    //   TopDownPlayer.Equip이 같은 이유로 지연 조회를 쓴다.
    TopDownPlayer _playerC;
    PlayerEquipment _equipC;
    PlayerInventory _invC;

    TopDownPlayer _player => _playerC != null ? _playerC : (_playerC = GetComponent<TopDownPlayer>());
    PlayerEquipment _equip => _equipC != null ? _equipC : (_equipC = GetComponent<PlayerEquipment>());
    PlayerInventory _inv   => _invC   != null ? _invC   : (_invC   = GetComponent<PlayerInventory>());

    GunVisual _visC;
    /// <summary>손에 든 총 비주얼(반동 연출용). 없을 수도 있으니 항상 ?. 로 부른다.</summary>
    GunVisual Visual => _visC != null ? _visC : (_visC = GetComponentInChildren<GunVisual>(true));

    float _nextShotAt;          // 연사 간격
    float _recoil;              // 누적 탄퍼짐(도)
    float _reloadUntil;         // 장전 끝나는 시각(0=장전 중 아님)
    float _reloadStartedAt;
    Transform _flash;           // 총구 섬광(재사용)
    float _flashUntil;

    public bool IsAiming { get; private set; }
    public bool IsReloading => _reloadUntil > 0f && Time.time < _reloadUntil;

    /// <summary>장전 진행도 0~1 (UI용).</summary>
    public float ReloadProgress
    {
        get
        {
            if (!IsReloading) return 0f;
            float total = Mathf.Max(0.01f, _reloadUntil - _reloadStartedAt);
            return Mathf.Clamp01((Time.time - _reloadStartedAt) / total);
        }
    }

    WeaponData Gun => _player != null ? _player.CurrentWeapon : null;
    ItemInstance GunInst => _equip != null ? _equip.GetSlotInstance(EquipSlot.PrimaryWeapon) : null;

    /// <summary>남은 탄(장착 탄창 기준). 탄창이 없으면 0.</summary>
    public int Ammo => GunInst != null ? GunInst.ammoCount : 0;
    /// <summary>장탄수(= 물린 탄창 용량). 탄창이 없으면 0.</summary>
    public int Capacity => GunInst != null ? GunInst.AmmoCapacity : 0;
    /// <summary>탄창이 물려 있나. 없으면 아예 못 쏜다(탄창은 주워야 하는 물건이다).</summary>
    public bool HasMagazine => GunInst != null && GunInst.LoadedMagazineData != null;

    /// <summary>지금 탄퍼짐(도, 반각) — 조준/누적 반동/부착물이 모두 반영된 값. 조준선 UI가 이걸 쓴다.</summary>
    public float CurrentSpreadDeg
    {
        get
        {
            var g = Gun;
            if (g == null) return 0f;
            float baseSpread = IsAiming ? g.adsSpreadDeg : g.hipSpreadDeg;
            float partMult = _equip != null ? _equip.WeaponPartRecoilMult : 1f;
            return (baseSpread + _recoil) * Mathf.Max(0.05f, partMult);
        }
    }

    void Update()
    {
        // 누적 반동은 손을 떼면 다시 모인다.
        var g = Gun;
        if (g != null && _recoil > 0f)
            _recoil = Mathf.Max(0f, _recoil - g.recoilRecover * Time.deltaTime);

        if (_reloadUntil > 0f && Time.time >= _reloadUntil) FinishReload();
        if (_flash != null && Time.time >= _flashUntil && _flash.gameObject.activeSelf)
            _flash.gameObject.SetActive(false);
    }

    /// <summary>TopDownPlayer가 매 프레임 넘겨 주는 입력. uiOpen이면 아무것도 안 한다.</summary>
    public void HandleInput(bool uiOpen)
    {
        var g = Gun;
        if (g == null || !g.isRanged) { IsAiming = false; return; }
        if (uiOpen) { IsAiming = false; return; }

        IsAiming = GameInput.GetMouseButton(1) && !IsReloading;

        if (GameInput.GetKeyDown(KeyCode.R)) TryReload();

        bool wantFire = g.fireMode == WeaponData.FireMode.Auto
            ? GameInput.GetMouseButton(0)
            : GameInput.GetMouseButtonDown(0);
        if (wantFire) TryFire();
    }

    /// <summary>한 발. 못 쏘면 이유를 알려 주고 false.</summary>
    public bool TryFire()
    {
        var g = Gun;
        if (g == null || !g.isRanged) return false;
        if (IsReloading) return false;
        if (Time.time < _nextShotAt) return false;

        var inst = GunInst;
        if (inst == null) return false;
        if (!HasMagazine) { Notify("탄창 없음"); return false; }
        if (inst.ammoCount <= 0) { Notify("탄 없음 — R 장전"); DryFireClick(); return false; }

        _nextShotAt = Time.time + 60f / Mathf.Max(30f, g.rpm);
        inst.ammoCount--;

        // 탄퍼짐 = (기본 or 조준) + 누적 반동, 부착물(손잡이·소염기)로 곱연산 감소.
        float spread = CurrentSpreadDeg;
        Vector2 dir = _player.FacingDirection;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + Random.Range(-spread, spread);
        Vector2 shotDir = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));

        float range = g.effectiveRange + (_equip != null ? _equip.WeaponPartRangeBonus : 0f);
        float dmg = g.damage * AmmoDamageMult(inst);
        Vector2 muzzle = (Vector2)transform.position + shotDir * 0.55f;

        Projectile.Spawn(transform, muzzle, shotDir, g.projectileSpeed, dmg, g.groggy, range,
                         new Color(1f, 0.93f, 0.6f));

        _recoil = Mathf.Min(g.recoilMax, _recoil + g.recoilPerShot);
        ShowFlash(muzzle, shotDir);
        Visual?.Kick();                 // 손에 든 총이 반동으로 튄다

        // ★ 총성 — 이 게임에서 가장 시끄러운 행동. 소염기가 이 반경을 줄인다.
        float noise = g.noiseRadius * (_equip != null ? _equip.WeaponPartNoiseMult : 1f);
        PlayerNoise.Pulse(muzzle, noise);

        if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.09f, 0.09f);
        return true;
    }

    /// <summary>장전 = 인벤에서 **가장 탄이 많은** 호환 탄창을 골라 맞바꾼다.</summary>
    public bool TryReload()
    {
        var g = Gun;
        if (g == null || !g.isRanged || IsReloading) return false;
        var inst = GunInst;
        if (inst == null) return false;

        var best = FindBestMagazine(g.caliber, inst);
        if (best == null) { Notify("맞는 탄창 없음"); return false; }

        _reloadStartedAt = Time.time;
        _reloadUntil = Time.time + Mathf.Max(0.2f, g.reloadSeconds);
        _pendingMag = best;
        return true;
    }

    InventoryGrid.PlacedItem _pendingMag;
    InventoryGrid _pendingGrid;

    void FinishReload()
    {
        _reloadUntil = 0f;
        var inst = GunInst;
        var placed = _pendingMag;
        _pendingMag = null;
        if (inst == null || placed == null || placed.item == null) return;

        // 장전 도중 그 탄창이 사라졌을 수 있다(버리기·정리). 다시 확인한다.
        if (_pendingGrid == null || _pendingGrid.Remove(placed) == null) { Notify("장전 취소"); return; }

        var newMag = placed.item;

        // ① 빼낸 탄창을 **남은 탄째로** 인벤에 되돌린다 — 이게 타르코프식의 핵심이다.
        var oldMagData = inst.LoadedMagazineData;
        if (oldMagData != null)
        {
            var old = new ItemInstance(oldMagData);
            old.ammoCount = inst.ammoCount;
            old.ammoItemId = inst.ammoItemId;
            if (_inv == null || !_inv.TryAutoPlaceAnywhere(old))
                Notify("빈 탄창 둘 자리가 없다 — 버림");   // 자리가 없으면 사라진다(무게 규칙과 같은 취급)
        }

        // ② 새 탄창을 물린다.
        inst.SetAttachment(WeaponPartType.Magazine, newMag.data.itemId);
        inst.ammoCount = newMag.ammoCount;
        inst.ammoItemId = newMag.ammoItemId;
        Notify($"장전 — {inst.ammoCount}/{inst.AmmoCapacity}");
    }

    /// <summary>구경이 맞고 탄이 든 탄창 중 가장 많이 든 것. 없으면 null.</summary>
    InventoryGrid.PlacedItem FindBestMagazine(string caliber, ItemInstance gun)
    {
        InventoryGrid.PlacedItem best = null;
        _pendingGrid = null;
        if (_inv == null) return null;

        foreach (var grid in new[] { _inv.PocketsGrid, _inv.Grid, _inv.SecureGrid })
        {
            if (grid == null) continue;
            foreach (var p in grid.GetAll())
            {
                var it = p.item;
                if (it == null || it.data == null || !it.data.IsMagazine) continue;
                if (it.ammoCount <= 0) continue;                                  // 빈 탄창은 넣어 봐야 못 쏜다
                if (!string.IsNullOrEmpty(it.data.magCaliber) &&
                    !string.IsNullOrEmpty(caliber) &&
                    it.data.magCaliber != caliber) continue;                      // 구경 불일치
                if (best == null || it.ammoCount > best.item.ammoCount) { best = p; _pendingGrid = grid; }
            }
        }
        return best;
    }

    float AmmoDamageMult(ItemInstance gun)
    {
        if (gun == null || string.IsNullOrEmpty(gun.ammoItemId)) return 1f;
        var d = ItemDatabase.Get(gun.ammoItemId);
        return d != null ? Mathf.Max(0.1f, d.ammoDamageMult) : 1f;
    }

    // ── 연출 ────────────────────────────────────────────────────────────
    void ShowFlash(Vector2 at, Vector2 dir)
    {
        if (_flash == null)
        {
            var go = new GameObject("MuzzleFlash");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = FlashSprite();
            sr.color = new Color(1f, 0.86f, 0.45f, 0.95f);
            sr.sortingOrder = 45;
            _flash = go.transform;
        }
        _flash.gameObject.SetActive(true);
        _flash.position = at;
        _flash.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        _flash.localScale = new Vector3(0.7f, 0.34f, 1f);
        _flashUntil = Time.time + 0.045f;
    }

    static Sprite _flashSprite;
    static Sprite FlashSprite()
    {
        if (_flashSprite != null) return _flashSprite;
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _flashSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0f, 0.5f), 1f);
        return _flashSprite;
    }

    void DryFireClick() { _nextShotAt = Time.time + 0.25f; }

    static void Notify(string msg)
    {
        // 토스트가 있으면 그쪽, 없으면 콘솔. (UI 의존을 강제하지 않는다)
        Debug.Log($"[총기] {msg}");
    }
}
