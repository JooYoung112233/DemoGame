using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 주의 끌기(유인) — **던진 물건이 떨어진 지점**만 등록되고, 적이 그걸 조사하러 간다.
///
/// 2026-09-09 볼륨 축소: 소음 시스템(`PlayerNoise`/`NoiseSystem`) 전체 폐기.
///   걷기·달리기·웅크림의 지속 소음, 타격/총성/문 소음, HUD 귀 아이콘 — 전부 없앴다.
///   적 발견은 **시야**가 유일한 축이고, 여기 남은 건 투척물 유인 하나뿐이다.
///   (투척물은 유지 결정이라 유인 수단이 없으면 돌이 아무 일도 못 한다.)
///
/// 정적 상태는 씬 전환에 안전(원시값 + Time 만료). 도메인 리로드 시 리셋.
/// </summary>
public static class Distraction
{
    struct Ping { public Vector2 pos; public float expire; public float radius; }

    static readonly List<Ping> _pings = new List<Ping>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset() => _pings.Clear();

    /// <summary>착탄 지점 등록. duration 동안 반경 안의 적이 조사하러 온다.</summary>
    public static void Report(Vector2 pos, float radius, float duration)
    {
        if (radius <= 0f) return;
        _pings.Add(new Ping { pos = pos, radius = radius, expire = Time.time + Mathf.Max(0.05f, duration) });
    }

    /// <summary>listener가 지금 반응할 유인이 있으면 true + 최근접 지점.</summary>
    public static bool TrySense(Vector2 listenerPos, out Vector2 source)
    {
        source = default;
        float best = float.MaxValue;
        bool found = false;

        for (int i = _pings.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _pings[i].expire) { _pings.RemoveAt(i); continue; }
            float d = Vector2.Distance(listenerPos, _pings[i].pos);
            if (d <= _pings[i].radius && d < best) { best = d; source = _pings[i].pos; found = true; }
        }
        return found;
    }
}
