// ────────────────────────────────────────────────────────────────────────────
// (DEPRECATED) 밸런스 에디터는 GameControlPanel 로 통합되었습니다.
//   → Tools ▸ TopDown ▸ 밸런스·컨트롤  의 "CSV 밸런스" 탭에서 동일 기능 사용.
//
// 이 파일은 "Unity 밖(파일 시스템)에서 .cs 를 지웠을 때" 생기는 stale 컴파일 참조
//   error CS2001: Source file '...BalanceEditorWindow.cs' could not be found
// 를 없애기 위한 **빈 자리표시자**입니다(메뉴/창 없음 → 중복 안 생김).
//
// 완전히 제거하려면 반드시 **Unity 프로젝트 창에서** 이 파일을 우클릭 → Delete 하세요.
//   (OS 탐색기/터미널로 지우면 다시 같은 CS2001 이 납니다 — 에디터 안에서 지워야 깔끔히 정리됨.)
// ────────────────────────────────────────────────────────────────────────────
#if UNITY_EDITOR
namespace Game.Editor
{
    internal static class BalanceEditorWindowDeprecated { }
}
#endif
