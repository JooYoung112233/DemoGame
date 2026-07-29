using UnityEngine;

/// <summary>
/// 피격 판정 박스. 캐릭터(또는 자식)에 부착, trigger Collider2D 필요.
/// AttackPerformer가 OverlapXXX로 스캔 → ReceiveHit 호출.
/// 비활성(구르기 무적 등) 시 콜라이더를 꺼 스캔에서 제외.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Hurtbox : MonoBehaviour
{
    [Tooltip("이 허트박스가 속한 캐릭터의 Health (비우면 부모에서 탐색)")]
    [SerializeField] Health health;
    [Tooltip("적이면 EnemyController (비우면 부모에서 탐색) — TakeHit으로 그로기 처리")]
    [SerializeField] EnemyController enemy;

    Collider2D _col;

    public bool Active => _col != null && _col.enabled;
    public Health Health => health;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _col.isTrigger = true;
        if (health == null) health = GetComponentInParent<Health>();
        if (enemy == null)  enemy  = GetComponentInParent<EnemyController>();
    }

    /// <summary>무적 토글 (구르기 등). 콜라이더를 꺼 스캔 제외.</summary>
    public void SetActive(bool active)
    {
        if (_col != null) _col.enabled = active;
    }

    /// <summary>공격이 명중했을 때 호출. 적이면 그로기 포함 TakeHit, 아니면 데미지만.</summary>
    public void ReceiveHit(float damage, float groggyAmount, Vector2 hitDir)
        => ReceiveHitAt(damage, groggyAmount, hitDir, null);

    /// <summary>부위까지 아는 피격(2026-07-29). hitPoint가 있으면 그 자리의 **부위 배율**이 곱해진다.
    ///
    /// 부위는 `PlayerMedicalSystem`과 같은 `BodyPartType`을 쓴다 — 판정과 치료가 같은 언어를 쓰게.
    /// hitPoint가 null이면(조준점 없는 공격) 가중 랜덤으로 부위를 뽑는다.</summary>
    public BodyPartType ReceiveHitAt(float damage, float groggyAmount, Vector2 hitDir, Vector2? hitPoint)
    {
        var part = hitPoint.HasValue
            ? BodyZones.FromPoint(_col != null ? _col.bounds : new Bounds(transform.position, Vector3.one),
                                  hitPoint.Value)
            : BodyZones.Random();

        float dmg = damage * BodyZones.DamageMult(part);

        if (enemy != null)      enemy.TakeHit(dmg, groggyAmount, hitDir);
        else if (health != null) health.TakeDamage(dmg);

        // 표시용 — 오버레이가 맞은 자리를 잠깐 밝힌다(테스트 도구, 로직 아님).
        var ov = GetComponentInParent<BodyZoneOverlay>();
        if (ov != null) ov.FlashPart(part);
        LastHitPart = part;
        return part;
    }

    /// <summary>마지막으로 맞은 부위(디버그/HUD용).</summary>
    public BodyPartType LastHitPart { get; private set; }
}
