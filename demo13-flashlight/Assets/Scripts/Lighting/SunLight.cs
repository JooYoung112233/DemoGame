using UnityEngine;

/// <summary>
/// "이게 이 씬의 태양이다"라고 표시하는 꼬리표. <see cref="DayNightCycle"/>이 몰 대상을 고를 때 본다.
///
/// 왜 필요한가: 태양은 <b>맵 씬</b>에 있고 <see cref="DayNightCycle"/>은 Systems 씬에 상주한다.
/// 씬에 디렉셔널이 둘 이상 있으면(레이드 맵 태양 + 예전에 PlayerRig에 넣어 둔 태양처럼)
/// "먼저 찾은 것"을 몰게 되는데, 그러면 <b>화면을 실제로 밝히는 태양은 그대로 대낮에 멈춰 있다.</b>
/// 실제로 그 상태였고, 값을 재도 몰리는 쪽만 정상으로 보여 한참 안 드러났다.
///
/// <see cref="followDayNight"/>가 false면 낮밤이 건드리지 않는다 — 은신처처럼
/// **의도적으로 시간과 무관하게** 고정된 조명을 쓰는 씬을 위한 것이다.
///
/// 설계: docs/3d-migration.md Stage 2
/// </summary>
[RequireComponent(typeof(Light))]
public class SunLight : MonoBehaviour
{
    [Tooltip("낮밤 주기가 이 라이트를 몰지 여부. 끄면 구운 값 그대로 유지된다(은신처 등).")]
    public bool followDayNight = true;
}
