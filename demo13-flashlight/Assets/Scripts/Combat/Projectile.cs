using UnityEngine;

/// <summary>
/// 총알. **실제로 날아가는 오브젝트**다(히트스캔 아님 — 2026-07-29 사용자 결정, docs/combat.md).
///
/// 투사체로 고른 이유: 맵에 깔아 둔 엄폐(차량·잔해·컨테이너 열)가 *진짜로* 막아 줘야 하기 때문.
/// 히트스캔은 "선이 막혔나"로만 작동해서, 둘레형 블록·야적장처럼 공들여 만든 엄폐가 의미를 잃는다.
/// 탑다운 줌에서 총알이 날아가는 게 눈에 보이는 것도 크다 — 어디서 쏘는지 읽힌다.
///
/// 이동은 **이전 위치 → 현재 위치 레이캐스트**로 판정한다. 위치만 옮기고 겹침을 보면
/// 빠른 탄이 얇은 벽을 **뚫고 지나간다**(터널링). 42m/s × 1프레임이면 0.7m라 벽 두께와 맞먹는다.
/// </summary>
[DisallowMultipleComponent]
public class Projectile : MonoBehaviour
{
    Vector2 _dir;
    float _speed, _damage, _groggy, _rangeLeft, _rangeTotal;
    Transform _owner;
    int _ownerLayer;
    bool _dead;

    static readonly RaycastHit[] _buf = new RaycastHit[12];

    /// <summary>탄이 나는 높이(총구 높이). 지면을 긁지 않게 가슴 높이로 띄운다.</summary>
    const float MuzzleY = 0.9f;
    float _height;

    /// <summary>총알 하나를 쏜다. from=총구, dir=단위벡터.</summary>
    public static Projectile Spawn(Transform owner, Vector2 from, Vector2 dir,
                                   float speed, float damage, float groggy, float range,
                                   Color color, float length = 0.55f)
    {
        var go = new GameObject("Bullet");
        go.transform.position = Plan3D.ToWorld(from, (owner != null ? owner.position.y : 0f) + MuzzleY);
        go.transform.rotation = Plan3D.LookRotation(dir, Quaternion.identity);

        // ⚠️ 스프라이트는 XY 평면에 납작해서 **local Z 스케일이 아무 일도 하지 않는다** —
        //    회전·길이를 이미 3D로 계산해 두고도 화면엔 길이 없는 점으로 나왔다.
        //    상자로 세우면 진행 방향(local Z)으로 실제 길이가 생겨 탄이 읽힌다.
        GreyboxMesh.Box(go.transform, "Tracer", Vector3.zero,
                        new Vector3(0.09f, 0.09f, length), color, castShadow: false);

        var p = go.AddComponent<Projectile>();
        p._owner = owner;
        p._ownerLayer = owner != null ? owner.gameObject.layer : -1;
        p._dir = dir.normalized;
        p._height = go.transform.position.y;
        p._speed = speed;
        p._damage = damage;
        p._groggy = groggy;
        p._rangeLeft = range;
        p._rangeTotal = Mathf.Max(0.01f, range);
        return p;
    }

    /// <summary>날아간 거리에 따른 데미지 배율. 유효사거리의 절반까지는 그대로,
    /// 그 뒤로 끝에서 55%까지 선형으로 준다. 총도 "멀면 약하다"가 있어야
    /// 거리를 좁힐 이유가 생긴다 — 안 그러면 사거리 끝에서만 쏘는 게 항상 정답이다.</summary>
    float Falloff()
    {
        float traveled = _rangeTotal - _rangeLeft;
        float t = Mathf.Clamp01(traveled / _rangeTotal);
        if (t <= 0.5f) return 1f;
        return Mathf.Lerp(1f, 0.55f, Mathf.InverseLerp(0.5f, 1f, t));
    }

    void Update()
    {
        if (_dead) return;
        float step = _speed * Time.deltaTime;
        if (step <= 0f) return;
        if (step > _rangeLeft) step = _rangeLeft;

        Vector2 prev = Plan3D.ToPlan(transform.position);
        Vector2 next = prev + _dir * step;

        if (Sweep(prev, step)) return;               // 뭔가 맞았으면 여기서 끝

        transform.position = Plan3D.ToWorld(next, _height);
        _rangeLeft -= step;
        if (_rangeLeft <= 0.001f) Destroy(gameObject);   // 유효사거리 끝 — 조용히 사라진다
    }

    /// <summary>이전→현재 구간을 훑어 처음 걸리는 것에 맞는다. 맞았으면 true.
    /// 평면 방향으로만 난다 — 탄도(중력)는 없다. 쿼터뷰라 높이차 사격은 아직 다루지 않는다.</summary>
    bool Sweep(Vector2 from, float dist)
    {
        Vector3 origin = Plan3D.ToWorld(from, _height);
        int n = Physics.RaycastNonAlloc(origin, Plan3D.ToWorld(_dir), _buf, dist, ~0,
                                        QueryTriggerInteraction.Collide);
        if (n <= 0) return false;

        // 가까운 것부터 봐야 한다 — Raycast 결과 순서는 보장되지 않는다.
        System.Array.Sort(_buf, 0, n, HitOrder.I);

        for (int i = 0; i < n; i++)
        {
            var c = _buf[i].collider;
            if (c == null) continue;
            if (_owner != null && (c.transform == _owner || c.transform.IsChildOf(_owner))) continue;

            var hb = c.GetComponent<Hurtbox>();
            if (hb == null) hb = c.GetComponentInParent<Hurtbox>();
            if (hb != null && hb.Active)
            {
                // 쏜 쪽과 같은 진영(레이어)은 통과 — 아군 오사는 아직 없다.
                if (_ownerLayer >= 0 && hb.gameObject.layer == _ownerLayer) continue;
                // 부위 = **맞은 자리 그대로**. 총은 조준한 곳이 맞는다(2026-07-29 결정).
                // 사거리 감쇠 — 멀수록 약해진다. 유효사거리 절반까지는 그대로.
                hb.ReceiveHitAt(_damage * Falloff(), _groggy, _dir, _buf[i].point);
                Impact(_buf[i].point, true);
                return true;
            }

            // 허트박스가 아닌 **솔리드**면 막힌다. 트리거(루트 상자·존·문)는 통과.
            if (!c.isTrigger) { Impact(_buf[i].point, false); return true; }
        }
        return false;
    }

    void Impact(Vector3 at, bool onFlesh)
    {
        _dead = true;
        if (onFlesh)
        {
            Hitstop.Do(0.02f);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.06f, 0.08f);
        }
        Destroy(gameObject);
    }

    /// <summary>RaycastHit을 거리순으로 — Array.Sort에 넘길 비교자.</summary>
    class HitOrder : System.Collections.Generic.IComparer<RaycastHit>
    {
        public static readonly HitOrder I = new HitOrder();
        public int Compare(RaycastHit a, RaycastHit b) => a.distance.CompareTo(b.distance);
    }

}
