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
    readonly Collider[] _buf = new Collider[16];

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
        _lockedFacing = _facing != null ? _facing() : Plan3D.ToPlan(transform.forward);
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

    static readonly RaycastHit[] _losBuf = new RaycastHit[8];

    /// <summary>몸 판정 높이 — 히트박스가 지면에 눌리지 않게 가슴 높이를 기준으로 잡는다.</summary>
    const float BodyMidY = 0.9f;
    const float BodyHalfH = 0.9f;

    /// <summary>공격자→대상 사이에 솔리드 벽이 있는지. 트리거·플레이어·적 레이어는 무시.</summary>
    static bool IsBlockedByWall(Vector3 from, Collider target)
    {
        Vector3 to = target.bounds.ClosestPoint(from);
        Vector3 d = to - from;
        float dist = d.magnitude;
        if (dist < 0.05f) return false;   // 몸에 붙어 있으면 검사 불필요

        int playerL = LayerMask.NameToLayer("Player");
        int enemyL  = LayerMask.NameToLayer("Enemy");

        int n = Physics.RaycastNonAlloc(from, d / dist, _losBuf, dist, ~0, QueryTriggerInteraction.Collide);
        for (int i = 0; i < n; i++)
        {
            var c = _losBuf[i].collider;
            if (c == null || c.isTrigger) continue;                 // 트리거(루트·존)는 시야를 막지 않음
            if (c == target || c.transform.IsChildOf(target.transform)) continue;
            int l = c.gameObject.layer;
            if (l == playerL || l == enemyL) continue;              // 캐릭터끼리는 서로를 막지 않음
            return true;                                            // 솔리드 벽 — 판정 무효
        }
        return false;
    }

    void ScanWindow(HitWindow w)
    {
        // 스윙 시작 시 고정한 방향을 쓴다(실시간 추종 금지 — Perform 참조).
        Vector2 facing = _lockedFacing;
        if (facing.sqrMagnitude < 0.0001f) facing = Vector2.right;
        float facingAngle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;

        // 히트박스 중심 — 평면 계산은 2D 그대로 두고 마지막에만 월드로 올린다.
        Vector2 centerPlan = Plan3D.ToPlan(transform.position) + RotateByAngle(w.offset, facingAngle);
        Vector3 center = Plan3D.ToWorld(centerPlan, transform.position.y + BodyMidY);

        int count;
        if (w.shape == HitboxShape.Box)
        {
            // 박스는 **전방(local Z)이 사거리**, local X가 폭이다(2D의 boxSize.x=사거리, y=폭과 같은 뜻).
            Vector2 boxDir = RotateByAngle(facing, w.angle);
            Quaternion rot = Plan3D.LookRotation(boxDir, transform.rotation);
            Vector3 half = new Vector3(w.boxSize.y * 0.5f, BodyHalfH, w.boxSize.x * 0.5f);
            count = Physics.OverlapBoxNonAlloc(center, half, _buf, rot, targetMask, QueryTriggerInteraction.Collide);
        }
        else
        {
            count = Physics.OverlapSphereNonAlloc(center, w.radius, _buf, targetMask, QueryTriggerInteraction.Collide);
        }

        Vector3 fromWorld = Plan3D.ToWorld(Plan3D.ToPlan(transform.position), transform.position.y + BodyMidY);

        bool landed = false;
        int dbgNoHurtbox = 0, dbgInactive = 0, dbgDup = 0, dbgWalled = 0;
        for (int i = 0; i < count; i++)
        {
            var col = _buf[i];
            if (col == null) continue;
            var hb = col.GetComponent<Hurtbox>() ?? col.GetComponentInParent<Hurtbox>();
            if (hb == null)               { dbgNoHurtbox++; continue; }
            if (!hb.Active)               { dbgInactive++;  continue; }
            if (_hitThisAttack.Contains(hb)) { dbgDup++;    continue; }

            // 2026-07-11: 벽 관통 판정 차단 — 공격자와 대상 사이에 **솔리드(비트리거)** 가 있으면 무효.
            //   예전엔 LOS 검사가 없어 벽을 사이에 두고도 사거리 안이면 그냥 맞았다.
            //   트리거(루트 상자·존)는 통과시켜야 하므로 비트리거만 차단으로 센다.
            if (IsBlockedByWall(fromWorld, col)) { dbgWalled++; continue; }

            _hitThisAttack.Add(hb);

            // ── 사거리 감쇠 (2026-07-29 사용자: "단검이면 사거리 5 안에서 때려야 하는데
            //    1에서 때리면 100%, 4.5에서 때리면 60% 이런 느낌") ──
            //    끝에 걸쳐 맞히면 스치는 것이고, 파고들어 때리면 제대로 들어간다.
            //    "닿기만 하면 같은 데미지"면 무기 사거리가 길수록 무조건 이득이라 거리 판단이 사라진다.
            float reach = w.shape == HitboxShape.Box
                ? w.offset.x + w.boxSize.x * 0.5f
                : w.offset.magnitude + w.radius;
            // ⚠️ 거리는 **평면 거리**다. 단차 위 적을 때릴 때 높이차가 사거리를 먹으면 안 된다.
            float dist = Plan3D.PlanDistance(transform.position, col.bounds.ClosestPoint(transform.position));
            float falloff = RangeFalloff(dist, reach);

            // ── 부위 (2026-07-29 사용자: "근접도 조준 똑같이 넣어줘") ──
            //    플레이어는 조준점(마우스 → 몸 위 한 점)이 그대로 부위가 된다. 적은 조준점이 없으니 가중 랜덤.
            Vector3? aim = AimPoint != null ? AimPoint() : null;

            hb.ReceiveHitAt(_current.damage * w.damageMult * falloff,
                            _current.groggy * w.groggyMult, facing, aim);
            landed = true;
        }

        // ★ 진단(2026-07-29) — "때려도 아무 일도 안 난다"를 눈으로 좁히기 위한 임시 로그.
        //   판정이 왜 안 닿는지는 겉으로 전혀 안 보인다: 콜라이더를 못 잡은 건지, 허트박스가 없는 건지,
        //   꺼져 있는 건지, 벽에 막힌 건지가 다 똑같이 "아무 일 없음"으로 보인다.
        //   F1 ▸ "타격 판정 로그"로 켠다. 원인을 잡으면 지운다.
        if (DebugLog)
            Debug.Log($"[타격] {name} 스캔 {count}개 → 적중 {(landed ? 1 : 0)} " +
                      $"| 허트박스없음 {dbgNoHurtbox} · 꺼짐 {dbgInactive} · 중복 {dbgDup} · 벽막힘 {dbgWalled} " +
                      $"| 중심({center.x:F1},{center.y:F1}) 마스크 {targetMask.value}");

        // 타격 성공(적중) 시 소음 펄스 — 스윙/헛방은 무음(2026-07-11). 플레이어·적 공용이라
        // "플레이어가 때림/맞음" 모두 impact 소음이 됨(반경 안 다른 적이 조사하러 옴).
        if (landed)
        {
            float r = GameTuning.Instance != null ? GameTuning.Instance.noiseAttack : 14f;
            PlayerNoise.Pulse(Plan3D.ToPlan(center), r);
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

    /// <summary>조준점 공급자(플레이어만 설정). null이면 조준 없는 공격 = 부위 가중 랜덤.</summary>
    public System.Func<Vector3?> AimPoint;

    /// <summary>타격 판정 진단 로그(F1에서 토글). 원인을 잡으면 지운다.</summary>
    public static bool DebugLog;

    /// <summary>사거리 감쇠 — 품 안(사거리의 NearBand 이내)은 100%, 끝은 FarMult까지 선형으로 준다.
    /// GameTuning에 값이 있으면 그걸 쓴다(밸런스는 Control Panel에서 만진다).</summary>
    public static float RangeFalloff(float dist, float reach)
    {
        if (reach <= 0.01f) return 1f;
        var gt = GameTuning.Instance;
        float nearBand = gt != null ? gt.meleeFalloffNear : 0.35f;   // 이 비율까지는 감쇠 없음
        float farMult  = gt != null ? gt.meleeFalloffFar  : 0.6f;    // 사거리 끝에서의 배율
        float t = Mathf.Clamp01(dist / reach);
        if (t <= nearBand) return 1f;
        float k = Mathf.InverseLerp(nearBand, 1f, t);
        return Mathf.Lerp(1f, farMult, k);
    }

    /// <summary>facing 기준 로컬 오프셋(x=전방, y=좌)을 월드 방향으로 회전.</summary>
    public static Vector2 RotateByAngle(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
