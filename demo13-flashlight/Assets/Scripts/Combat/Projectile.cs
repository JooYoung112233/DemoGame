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
    float _speed, _damage, _groggy, _rangeLeft;
    Transform _owner;
    int _ownerLayer;
    bool _dead;

    static Sprite _dot;
    static readonly RaycastHit2D[] _buf = new RaycastHit2D[12];

    /// <summary>총알 하나를 쏜다. from=총구, dir=단위벡터.</summary>
    public static Projectile Spawn(Transform owner, Vector2 from, Vector2 dir,
                                   float speed, float damage, float groggy, float range,
                                   Color color, float length = 0.55f)
    {
        var go = new GameObject("Bullet");
        go.transform.position = from;
        go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Dot();
        sr.color = color;
        sr.sortingOrder = 40;                       // 바닥·건물 위, UI 아래
        go.transform.localScale = new Vector3(length, 0.09f, 1f);

        var p = go.AddComponent<Projectile>();
        p._owner = owner;
        p._ownerLayer = owner != null ? owner.gameObject.layer : -1;
        p._dir = dir.normalized;
        p._speed = speed;
        p._damage = damage;
        p._groggy = groggy;
        p._rangeLeft = range;
        return p;
    }

    void Update()
    {
        if (_dead) return;
        float step = _speed * Time.deltaTime;
        if (step <= 0f) return;
        if (step > _rangeLeft) step = _rangeLeft;

        Vector2 prev = transform.position;
        Vector2 next = prev + _dir * step;

        if (Sweep(prev, step)) return;               // 뭔가 맞았으면 여기서 끝

        transform.position = next;
        _rangeLeft -= step;
        if (_rangeLeft <= 0.001f) Destroy(gameObject);   // 유효사거리 끝 — 조용히 사라진다
    }

    /// <summary>이전→현재 구간을 훑어 처음 걸리는 것에 맞는다. 맞았으면 true.</summary>
    bool Sweep(Vector2 from, float dist)
    {
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
        int n = Physics2D.Raycast(from, _dir, filter, _buf, dist);
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
                hb.ReceiveHit(_damage, _groggy, _dir);
                Impact(_buf[i].point, true);
                return true;
            }

            // 허트박스가 아닌 **솔리드**면 막힌다. 트리거(루트 상자·존·문)는 통과.
            if (!c.isTrigger) { Impact(_buf[i].point, false); return true; }
        }
        return false;
    }

    void Impact(Vector2 at, bool onFlesh)
    {
        _dead = true;
        if (onFlesh)
        {
            Hitstop.Do(0.02f);
            if (CameraFollow.Instance != null) CameraFollow.Instance.Shake(0.06f, 0.08f);
        }
        Destroy(gameObject);
    }

    /// <summary>RaycastHit2D를 거리순으로 — Array.Sort에 넘길 비교자.</summary>
    class HitOrder : System.Collections.Generic.IComparer<RaycastHit2D>
    {
        public static readonly HitOrder I = new HitOrder();
        public int Compare(RaycastHit2D a, RaycastHit2D b) => a.distance.CompareTo(b.distance);
    }

    /// <summary>1×1 흰 스프라이트(총알 몸통). 스케일로 길이를 준다.</summary>
    static Sprite Dot()
    {
        if (_dot != null) return _dot;
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _dot = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _dot;
    }
}
