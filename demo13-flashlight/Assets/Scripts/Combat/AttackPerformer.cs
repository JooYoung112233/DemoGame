using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AttackData를 받아 시간 진행하며 활성 히트 윈도우의 영역을 OverlapXXX로 스캔,
/// 반대 팀 Hurtbox에 데미지/그로기 전달. 한 공격당 같은 Hurtbox는 1회만 타격.
/// 플레이어/적 공통.
/// </summary>
public class AttackPerformer : MonoBehaviour
{
    [Tooltip("타격 대상 레이어 (플레이어=Enemy, 적=Player)")]
    [SerializeField] LayerMask targetMask;

    Func<Vector2> _facing;
    AttackData _current;
    float _timer;
    bool _performing;
    readonly HashSet<Hurtbox> _hitThisAttack = new HashSet<Hurtbox>();
    readonly Collider2D[] _buf = new Collider2D[16];

    public bool IsPerforming => _performing;
    public AttackData Current => _current;
    public float NormalizedTime =>
        (_current != null && _current.duration > 0f) ? Mathf.Clamp01(_timer / _current.duration) : 0f;

    /// <summary>레이어 마스크와 공격 방향 공급자 설정. (TopDownPlayer/EnemyController가 호출)</summary>
    public void Configure(LayerMask mask, Func<Vector2> facingProvider)
    {
        targetMask = mask;
        _facing = facingProvider;
    }

    public void Perform(AttackData data)
    {
        if (data == null) return;
        _current = data;
        _timer = 0f;
        _performing = true;
        _hitThisAttack.Clear();
    }

    public void Cancel()
    {
        _performing = false;
        _current = null;
    }

    void Update()
    {
        if (!_performing || _current == null) return;

        _timer += Time.deltaTime;
        float norm = NormalizedTime;

        var windows = _current.windows;
        for (int i = 0; i < windows.Count; i++)
        {
            var w = windows[i];
            if (w != null && w.IsActiveAt(norm)) ScanWindow(w);
        }

        if (_timer >= _current.duration) Cancel();
    }

    void ScanWindow(HitWindow w)
    {
        Vector2 facing = _facing != null ? _facing() : (Vector2)transform.right;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector2.right;
        float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        Vector2 center = (Vector2)transform.position + RotateByAngle(w.offset, facingAngle);

        int count = w.shape == HitboxShape.Box
            ? Physics2D.OverlapBoxNonAlloc(center, w.boxSize, facingAngle + w.angle, _buf, targetMask)
            : Physics2D.OverlapCircleNonAlloc(center, w.radius, _buf, targetMask);

        for (int i = 0; i < count; i++)
        {
            var col = _buf[i];
            if (col == null) continue;
            var hb = col.GetComponent<Hurtbox>() ?? col.GetComponentInParent<Hurtbox>();
            if (hb == null || !hb.Active || _hitThisAttack.Contains(hb)) continue;

            _hitThisAttack.Add(hb);
            hb.ReceiveHit(_current.damage * w.damageMult, _current.groggy * w.groggyMult, facing);
        }
    }

    /// <summary>facing 기준 로컬 오프셋(x=전방, y=좌)을 월드 방향으로 회전.</summary>
    public static Vector2 RotateByAngle(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
