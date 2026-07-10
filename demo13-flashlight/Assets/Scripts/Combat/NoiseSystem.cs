using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 소음 중앙 허브(정적) — 플레이어가 내는 소음을 등록하고, 적이 "들었는지"를 질의한다.
/// (docs/combat.md 소음 시스템 2026-07-10)
///
/// 두 종류:
///   • 지속 소음(이동): `PlayerNoise`가 매 프레임 `SetPlayerSustained(pos, radius)`로 갱신. 중심 = 플레이어.
///   • 순간 펄스(타격·문): `ReportPulse(pos, radius)` — noisePulseDuration 동안만 유효.
///
/// 적은 `TryHear(listenerPos, out src)`로 한 번에 질의(지속 + 펄스 중 가장 가까운 유효 소스).
/// 정적 상태는 씬 전환에 안전(원시값 + Time 만료). 도메인 리로드 시 리셋.
/// </summary>
public static class NoiseSystem
{
    struct Pulse { public Vector2 pos; public float radius; public float expire; }

    static Vector2 _playerPos;
    static float _playerRadius;          // 지속 소음 반경(0=무음)
    static readonly List<Pulse> _pulses = new List<Pulse>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() { _playerPos = Vector2.zero; _playerRadius = 0f; _pulses.Clear(); }

    /// <summary>지속(이동) 소음 갱신. PlayerNoise가 매 프레임 호출.</summary>
    public static void SetPlayerSustained(Vector2 pos, float radius)
    {
        _playerPos = pos;
        _playerRadius = Mathf.Max(0f, radius);
    }

    /// <summary>순간 펄스 등록(타격·문 등). duration 동안 그 지점 반경 안의 적이 들을 수 있다.</summary>
    public static void ReportPulse(Vector2 pos, float radius, float duration)
    {
        if (radius <= 0f) return;
        _pulses.Add(new Pulse { pos = pos, radius = radius, expire = Time.time + Mathf.Max(0.05f, duration) });
    }

    /// <summary>현재 플레이어 지속 소음 반경(UI용).</summary>
    public static float PlayerSustainedRadius => _playerRadius;

    /// <summary>listener가 지금 들을 수 있는 소음이 있으면 true + 가장 가까운 소스 위치.
    /// 지속(플레이어) + 유효 펄스 중 listener를 반경 안에 포함하는 것들 가운데 최근접.</summary>
    public static bool TryHear(Vector2 listenerPos, out Vector2 source)
    {
        source = default;
        float best = float.MaxValue;
        bool heard = false;

        // 지속(이동) 소음
        if (_playerRadius > 0f)
        {
            float d = Vector2.Distance(listenerPos, _playerPos);
            if (d <= _playerRadius && d < best) { best = d; source = _playerPos; heard = true; }
        }

        // 순간 펄스(만료 제거하며 검사)
        for (int i = _pulses.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _pulses[i].expire) { _pulses.RemoveAt(i); continue; }
            float d = Vector2.Distance(listenerPos, _pulses[i].pos);
            if (d <= _pulses[i].radius && d < best) { best = d; source = _pulses[i].pos; heard = true; }
        }
        return heard;
    }
}
