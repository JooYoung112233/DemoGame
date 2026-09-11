using UnityEngine;

/// <summary>
/// **캐릭터 배치** — 시설 앞으로 보내고, 시설을 바라보게 하고, 자세를 잡는다.
///
/// 침대=눕기, 조리대=앞에 서기, 대기=의자에 앉기. 자세 클립이 아직 없어서
/// 지금은 위치·방향까지만 반영한다(앉기·눕기 클립은 후속).
/// </summary>
public partial class HideoutDiorama
{
    /// <summary>캐릭터를 앵커 자리에 놓고 시설을 바라보게 한다.</summary>
    void PlaceAt(HideoutFacilityAnchor a, bool instant)
    {
        if (_player == null || a == null) return;

        Vector3 pos = a.StandPosition;
        _player.transform.position = pos;
        var rb = _player.GetComponent<Rigidbody>();
        if (rb != null) rb.position = pos;
        Physics.SyncTransforms();

        // 시설을 바라본다. 기울기는 카메라가 주므로 yaw만.
        if (_view != null)
        {
            Vector2 plan = Plan3D.ToPlan(a.FaceDirection);
            float yaw = Mathf.Atan2(plan.x, plan.y) * Mathf.Rad2Deg;
            _view.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        ApplyPose(a.pose);

        // ⚠️ SnapToTarget은 **캐릭터** 기준이라 방 고정 프레임을 깨뜨린다. 쓰지 않는다.
    }

    /// <summary>자세 적용. ⚠️ 앉기·눕기 애니메이션은 아직 없다(치비 = idle/walk/run 3종).
    /// 애니메이터에 같은 이름의 파라미터가 있을 때만 걸고, 없으면 조용히 넘어간다
    /// — 없는 파라미터를 건드리면 매 클릭마다 경고가 쌓인다. 제작되면 자동으로 살아난다.</summary>
    void ApplyPose(HideoutFacilityAnchor.Pose pose)
    {
        if (_view == null) return;
        var an = _view.GetComponentInChildren<Animator>();
        if (an == null || an.runtimeAnimatorController == null) return;

        string wanted = pose.ToString();   // Stand / Sit / Lie
        foreach (var p in an.parameters)
        {
            if (p.type != AnimatorControllerParameterType.Trigger || p.name != wanted) continue;
            an.SetTrigger(wanted);
            return;
        }
    }
}
