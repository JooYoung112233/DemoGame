/// <summary>
/// 상호작용 가능한 오브젝트 공통 인터페이스.
/// InteractableObject가 기본 구현.
/// </summary>
public interface IInteractable
{
    /// <summary>프롬프트에 표시할 텍스트 (예: "조사하기")</summary>
    string PromptText { get; }

    /// <summary>현재 상호작용 가능한 상태인지</summary>
    bool CanInteract { get; }

    /// <summary>상호작용 가능 거리</summary>
    float InteractRange { get; }

    /// <summary>상호작용 실행</summary>
    void Interact(PlayerController player);
}
