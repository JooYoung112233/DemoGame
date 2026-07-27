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

    /// <summary>현재 재생 프레임.</summary>
    public int CurrentFrame => _current != null ? _current.FrameAtTime(_timer) : 0;

    /// <summary>다음 콤보로 캔슬 입력을 받을 수 있는 시점인지.</summary>
    public bool CanCancel =>
        _current != null && _current.cancelFromFrame >= 0 && CurrentFrame >= _current.cancelFromFrame;

    public float NormalizedTime =>
        (_current != null && _current.Duration > 0f) ? Mathf.Clamp01(_timer / _current.Duration) : 0f;

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
        // 2026-07-11: 스윙 시작 순간의 방향을 **고정(스냅샷)**한다.
        //   예전엔 ScanWindow가 매 프레임 _facing()을 새로 읽어 히트박스가 마우스를 실시간 추종 →
        //   스윙 도중 마우스를 휙 돌리면 박스가 캐릭터 주위를 쓸고 지나가 **등 뒤 적까지 맞았다.**
        _lockedFacing = _facing != null ? _facing() : (Vector2)transform.right;
        if (_lockedFacing.sqrMagnitude < 0.0001f) _lockedFacing = Vector2.right;
    }

    Vector2 _lockedFacing = Vector2.right;

    public void Cancel()
    {
        _performing = false;
        _current = null;
    }

    void Update()
    {
        if (!_performing || _current == null) return;

        _timer += Time.deltaTime;
        int frame = CurrentFrame;

        var windows = _current.windows;
        for (int i = 0; i < windows.Count; i++)
        {
            var w = windows[i];
            if (w != null && w.IsActiveAtFrame(frame)) ScanWindow(w);
        }

        if (_timer >= _current.Duration) Cancel();
    }

    void ScanWindow(HitWindow w)
    {
        // 스윙 시작 시 고정한 방향을 쓴다(실시간 추종 금지 — Perform 참조).
        Vector2 facing = _lockedFacing;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector2.right;
        float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        Vector2 center = (Vector2)transform.position + RotateByAngle(w.offset, facingAngle);

        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        filter.SetLayerMask(targetMask);
        int count = w.shape == HitboxShape.Box
            ? Physics2D.OverlapBox(center, w.boxSize, facingAngle + w.angle, filter, _buf)
            : Physics2D.OverlapCircle(center, w.radius, filter, _buf);

        bool landed = false;
        for (int i = 0; i < count; i++)
        {
            var col = _buf[i];
            if (col == null) continue;
            var hb = col.GetComponent<Hurtbox>() ?? col.GetComponentInParent<Hurtbox>();
            if (hb == null || !hb.Active || _hitThisAttack.Contains(hb)) continue;

            _hitThisAttack.Add(hb);
            hb.ReceiveHit(_current.damage * w.damageMult, _current.groggy * w.groggyMult, facing);
            landed = true;
        }

        // 타격 성공(적중) 시 소음 펄스 — 스윙/헛방은 무음(2026-07-11). 플레이어·적 공용이라
        // "플레이어가 때림/맞음" 모두 impact 소음이 됨(반경 안 다른 적이 조사하러 옴).
        if (landed)
        {
            float r = GameTuning.Instance != null ? GameTuning.Instance.noiseAttack : 14f;
            PlayerNoise.Pulse(center, r);
        }

        // 강공(데이터 플래그) 적중 시 히트스탑 + 카메라 셰이크/줌 — 한 번만
        if (landed && _current.hitstop)
        {
            Hitstop.Do(_current.hitstopDuration);
            if (CameraFollow.Instance != null)
            {
                CameraFollow.Instance.Shake(0.14f, 0.18f);
                CameraFollow.Instance.ZoomPunch(0.05f, 0.18f);
            }
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
