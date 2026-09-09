using UnityEngine;

/// <summary>
/// 그레이박스 총기 비주얼 — **손에 총이 들려 있고, 쏘면 반동으로 튄다.**
/// (2026-07-29 사용자: "총도 칼처럼 비주얼 만들었나?")
///
/// `MeleeWeaponVisual`과 같은 규약이다 — 스파인이 들어오면 통째로 교체할 임시 연출이라
/// **판정에는 일절 관여하지 않는다.** 총알은 `Projectile`, 탄퍼짐은 `PlayerGun`이 담당하고
/// 여기는 보여주기만 한다. (연출이 판정을 건드리면 "보이는 것과 맞는 것"이 어긋난다.)
///
/// 구조: owner ─ Pivot(회전) ─ Body(총몸) + Barrel(총열) + Grip(손잡이)
///   Pivot의 Z회전 = 바라보는 각 + 반동/장전 오프셋. 총열은 Pivot의 +X로 뻗는다.
/// </summary>
public class GunVisual : MonoBehaviour
{
    const float RecoilKickDeg = 13f;    // 발사 순간 총구가 들리는 각
    const float RecoilBackM   = 0.12f;  // 발사 순간 뒤로 밀리는 거리
    const float AdsForwardM   = 0.10f;  // 조준하면 앞으로 살짝 내민다(자세가 달라 보이게)
    const float ReloadDownDeg = -58f;   // 장전 중 총구를 내린다 — 지금 못 쏜다는 표시

    /// <summary>총을 든 손 높이(m).</summary>
    const float HandHeight = 1.10f;

    Transform _pivot;
    MeshRenderer _body, _barrel, _grip;

    float _facingDeg;
    float _recoil01;        // 1 = 방금 쏨 → 0으로 회복
    float _recoilFade = 9f;
    bool  _aiming, _reloading;
    float _reload01;

    /// <summary>owner 밑에 총을 만들어 붙인다. 이미 있으면 그걸 돌려준다.</summary>
    public static GunVisual Attach(Transform owner, Color metal, int sortingOrder, float length = 0.62f)
    {
        if (owner == null) return null;
        var existing = owner.GetComponentInChildren<GunVisual>(true);
        if (existing != null) return existing;

        var root = new GameObject("GunVisual");
        root.transform.SetParent(owner, false);
        root.transform.localPosition = Vector3.zero;
        var g = root.AddComponent<GunVisual>();
        g.Build(metal, sortingOrder, length);
        return g;
    }

    void Build(Color metal, int order, float len)
    {
        _pivot = new GameObject("Pivot").transform;
        _pivot.SetParent(transform, false);

        var dark = new Color(metal.r * 0.55f, metal.g * 0.55f, metal.b * 0.55f);
        // 손잡이(아래로 살짝) → 총몸 → 총열 순으로 겹쳐 놓으면 탑다운에서도 '총'으로 읽힌다.
        _grip   = MakePart("Grip",   dark,  order,     0.06f, -0.09f, 0.16f, 0.16f);
        _body   = MakePart("Body",   metal, order + 1, len * 0.34f, 0f, len * 0.62f, 0.15f);
        _barrel = MakePart("Barrel", dark,  order + 1, len * 0.78f, 0f, len * 0.52f, 0.085f);

        Apply();
    }

    /// <summary>총 조각 하나(3D 상자). offY(총열 아래 손잡이)는 3D에서도 그대로 y다 —
    /// 위아래 관계라서 평면으로 눕히면 안 된다.</summary>
    MeshRenderer MakePart(string name, Color color, int _unusedOrder,
                          float offX, float offY, float len, float thick)
        => GreyboxMesh.Box(_pivot, name, new Vector3(offX, offY, 0f),
                           new Vector3(len, thick, thick), color, castShadow: false);

    /// <summary>표시 on/off — 시야콘 밖에서 **총만 어둠에 떠 있는** 것을 막는다.
    /// (총은 몸통 스프라이트의 자식이 아니라 루트의 자식이라 몸통을 꺼도 자동으로 안 꺼진다.)</summary>
    public void SetVisible(bool v)
    {
        if (_body != null)   _body.enabled = v;
        if (_barrel != null) _barrel.enabled = v;
        if (_grip != null)   _grip.enabled = v;
    }

    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        // 평면 방향 → Unity yaw(부호가 뒤집힌다). 자세한 근거는 MeleeWeaponVisual.SetFacing 참조.
        _facingDeg = Mathf.Atan2(-dir.y, dir.x) * Mathf.Rad2Deg;
    }

    /// <summary>매 프레임 상태 갱신 — 조준/장전.</summary>
    public void SetState(bool aiming, bool reloading, float reloadProgress)
    {
        _aiming = aiming;
        _reloading = reloading;
        _reload01 = Mathf.Clamp01(reloadProgress);
    }

    /// <summary>한 발 쏨 — 반동 1회.</summary>
    public void Kick() => _recoil01 = 1f;

    void LateUpdate()
    {
        if (_recoil01 > 0f) _recoil01 = Mathf.Max(0f, _recoil01 - _recoilFade * Time.deltaTime);
        Apply();
    }

    void Apply()
    {
        if (_pivot == null) return;

        // 장전은 반동보다 우선 — 총구를 내렸다가 끝날 때쯤 다시 든다.
        //   진행도로 각을 되돌려 "다 됐다"가 눈에 보이게 한다(HUD를 안 봐도 읽힌다).
        float reloadDeg = 0f;
        if (_reloading)
        {
            float back = _reload01 < 0.75f ? 1f : Mathf.InverseLerp(1f, 0.75f, _reload01);
            reloadDeg = ReloadDownDeg * back;
        }

        float kickDeg = RecoilKickDeg * _recoil01;
        // ⚠️ 2D에선 조준 방향과 총구 들림이 **둘 다 Z축**이라 그냥 더하면 됐다. 3D에선 갈린다 —
        //    조준은 yaw(Y), 총구 들림·장전 내림은 **총열을 기준으로 한 상하**(로컬 Z)다.
        //    Euler(0, y, z)는 Rz를 먼저 적용하므로 총열을 들었다가 그대로 조준 방향으로 돌린다.
        _pivot.localRotation = Quaternion.Euler(0f, _facingDeg, reloadDeg + kickDeg);

        // 앞뒤 위치 — 조준하면 내밀고, 쏘면 뒤로 밀린다.
        float fwd = (_aiming ? AdsForwardM : 0f) - RecoilBackM * _recoil01;
        // yaw θ의 전방은 (cos θ, 0, −sin θ)다. 2D 시절 (cos, sin, 0)을 그대로 두면
        // 반동이 **위아래로** 튄다.
        float rad = _facingDeg * Mathf.Deg2Rad;
        _pivot.localPosition = new Vector3(Mathf.Cos(rad) * fwd, HandHeight, -Mathf.Sin(rad) * fwd);
    }
}
