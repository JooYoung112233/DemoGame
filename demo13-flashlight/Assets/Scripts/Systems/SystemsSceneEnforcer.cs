using UnityEngine;

/// <summary>
/// (2D 시절) Systems 씬의 Global Light2D만 남기고 게임플레이 씬의 글로벌 라이트를 끄던 컴포넌트.
/// 3D 전환으로 Light2D가 없어져 할 일이 없다 — Systems.unity에 배치돼 있어 빠진 스크립트가 생기지 않도록
/// 클래스만 남긴다(2026-09-12 시스템 정리 5단계). 씬에서 떼어 낸 뒤 파일을 지우면 된다.
/// </summary>
[DefaultExecutionOrder(-900)]
public class SystemsSceneEnforcer : MonoBehaviour
{
}
