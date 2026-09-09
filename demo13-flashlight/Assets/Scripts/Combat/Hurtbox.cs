using UnityEngine;

/// <summary>
/// 피격 판정 박스. 캐릭터(또는 자식)에 부착, trigger Collider 필요.
/// AttackPerformer가 OverlapXXX로 스캔 → ReceiveHit 호출.
/// 비활성(구르기 무적 등) 시 콜라이더를 꺼 스캔에서 제외.
/// </summary>
/// ⚠️ <c>[RequireComponent(typeof(Collider))]</c>는 쓸 수 없다 — <c>Collider</c>는 추상 클래스라
/// Unity가 자동으로 붙여 주지 못한다(2D의 <c>Collider2D</c>도 마찬가지였지만, 2D 프리팹엔
/// 이미 붙어 있어 드러나지 않았다). 3D 전환 후 콜라이더 없는 오브젝트에서 실제로
/// NullReference가 났다. 그래서 없으면 **박스를 직접 붙이고 경고**한다.
public class Hurtbox : MonoBehaviour
{
    [Tooltip("이 허트박스가 속한 캐릭터의 Health (비우면 부모에서 탐색)")]
    [SerializeField] Health health;
    [Tooltip("적이면 EnemyController (비우면 부모에서 탐색) — TakeHit으로 그로기 처리")]
    [SerializeField] EnemyController enemy;

    Collider _col;

    public bool Active => _col != null && _col.enabled;
    /// <summary>맞을 몸. **비어 있으면 그때 찾는다.**
    ///
    /// ⚠️ Awake에서 한 번만 찾으면 안 된다. `AddComponent`는 즉시 `Awake()`를 부르므로,
    ///    조립 순서상 <b>Health보다 Hurtbox가 먼저 붙으면 null이 그대로 굳는다.</b>
    ///    실제로 `EnemySpawner`가 허트박스를 Health보다 먼저 붙여서, 스폰된 적 전부가
    ///    **때려도 데미지가 안 들어가는** 상태였다. 지연 해석이면 순서에 안 휘둘린다.</summary>
    public Health Health
    {
        get
        {
            if (health == null) health = GetComponentInParent<Health>();
            return health;
        }
    }

    /// <summary>같은 이유로 적 참조도 지연 해석한다 — 그로기·피격 반응이 여기 달려 있다.</summary>
    EnemyController Enemy
    {
        get
        {
            if (enemy == null) enemy = GetComponentInParent<EnemyController>();
            return enemy;
        }
    }

    void Awake()
    {
        _col = GetComponent<Collider>();
        if (_col == null)
        {
            // 맞을 몸이 없으면 허트박스는 아무 일도 못 한다. 조용히 죽지 않도록 만들어 준다.
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(0.7f, 1.6f, 0.7f);
            box.center = new Vector3(0f, 0.85f, 0f);   // 부위 판정이 높이로 갈리므로 세워 둔다
            _col = box;
            Debug.LogWarning($"[Hurtbox] {name}: 콜라이더가 없어 기본 박스를 붙였다 — " +
                             "프리팹/빌더에서 3D 콜라이더를 지정할 것.", this);
        }
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
    /// 부위는 `BodyZones`의 `BodyPartType`을 쓴다 — 데미지 배율/적 부위 부상 판정용.
    /// hitPoint가 null이면(조준점 없는 공격) 가중 랜덤으로 부위를 뽑는다.</summary>
    public BodyPartType ReceiveHitAt(float damage, float groggyAmount, Vector2 hitDir, Vector3? hitPoint)
    {
        // 좌우 기준축은 **타격 방향의 수직**으로 잡는다 — 정면에서 맞으면 좌우가 그대로,
        // 옆에서 맞으면 앞뒤가 좌우가 된다. 3D에선 어느 쪽에서 맞았는지가 매번 다르다.
        Vector3 lateral = Vector3.Cross(Vector3.up, Plan3D.ToWorld(hitDir));
        var part = hitPoint.HasValue
            ? BodyZones.FromPoint(_col != null ? _col.bounds : new Bounds(transform.position, Vector3.one),
                                  hitPoint.Value, lateral)
            : BodyZones.Random();

        float dmg = damage * BodyZones.DamageMult(part);

        // 부위 부상 누적 — 데미지를 넣기 **전에** 기록해야 이번 타격도 부상 판정에 든다.
        var inj = GetComponentInParent<UnitInjuries>();
        if (inj != null) inj.Add(part, dmg);

        // 지연 해석 프로퍼티로 읽는다 — 필드를 직접 보면 조립 순서에 따라 null일 수 있다.
        if (Enemy != null)      Enemy.TakeHit(dmg, groggyAmount, hitDir);
        else if (Health != null) Health.TakeDamage(dmg);

        // 표시용 — 오버레이가 맞은 자리를 잠깐 밝힌다(테스트 도구, 로직 아님).
        var ov = GetComponentInParent<BodyZoneOverlay>();
        if (ov != null) ov.FlashPart(part);
        LastHitPart = part;
        return part;
    }

    /// <summary>마지막으로 맞은 부위(디버그/HUD용).</summary>
    public BodyPartType LastHitPart { get; private set; }
}
