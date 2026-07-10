using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// 레거시 UnityEngine.Input 폴링 API를 신 Input System(Keyboard/Mouse.current)으로
/// 감싼 호환 셰임. 기존 코드의 Input.xxx 호출을 GameInput.xxx 로 1:1 치환하기 위한 것.
///
/// 배경: 프로젝트가 Active Input Handling = "Input System Package (New)" 전용으로 전환되면서
/// 레거시 UnityEngine.Input 직접 사용이 폐기/예외 대상이 됨. 입력 처리는 전부 폴링 구조라
/// 액션 에셋·콜백으로 재배선하지 않고 폴링→폴링으로 그대로 옮긴다.
///
/// 디바이스(Keyboard/Mouse)가 없을 수 있으므로 모든 접근에 null 가드를 둔다.
/// </summary>
public static class GameInput
{
    // ── 키보드 (+ 게임패드 버튼 병합) ─────────────────────────
    // 각 키 조회는 키보드 상태 OR 매핑된 게임패드 버튼으로 합쳐진다.
    // (매핑은 아래 GamepadButtonFor 참조 — 셰임 확장이라 호출처는 그대로.)
    public static bool GetKey(KeyCode code)
    {
        var k = Keyboard.current;
        if (k != null)
        {
            var key = Map(code);
            if (key != Key.None && k[key].isPressed) return true;
        }
        var gb = GamepadButtonFor(code);
        return gb != null && gb.isPressed;
    }

    public static bool GetKeyDown(KeyCode code)
    {
        var k = Keyboard.current;
        if (k != null)
        {
            var key = Map(code);
            if (key != Key.None && k[key].wasPressedThisFrame) return true;
        }
        var gb = GamepadButtonFor(code);
        return gb != null && gb.wasPressedThisFrame;
    }

    public static bool GetKeyUp(KeyCode code)
    {
        var k = Keyboard.current;
        if (k != null)
        {
            var key = Map(code);
            if (key != Key.None && k[key].wasReleasedThisFrame) return true;
        }
        var gb = GamepadButtonFor(code);
        return gb != null && gb.wasReleasedThisFrame;
    }

    // ── 마우스 버튼 (0=좌, 1=우, 2=중) ────────────────────────
    static ButtonControl MouseButton(int button)
    {
        var m = Mouse.current;
        if (m == null) return null;
        switch (button)
        {
            case 0: return m.leftButton;
            case 1: return m.rightButton;
            case 2: return m.middleButton;
            default: return null;
        }
    }

    // 마우스 버튼은 게임패드 트리거와도 병합한다 (0=우트리거=약공격, 1=좌트리거=강공격 차징).
    static ButtonControl GamepadForMouse(int button)
    {
        var g = Gamepad.current;
        if (g == null) return null;
        // UI 열림 중엔 트리거를 마우스 클릭으로 병합하지 않는다.
        // (인벤토리 드래그/분할 등이 마지막 커서 위치에서 유령 조작되는 것을 막음. 공격은 UI 열림 시 어차피 봉쇄.)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIOpen()) return null;
        switch (button)
        {
            case 0: return g.rightTrigger;
            case 1: return g.leftTrigger;
            default: return null;
        }
    }

    public static bool GetMouseButton(int button)
    {
        var b = MouseButton(button);
        if (b != null && b.isPressed) return true;
        var gb = GamepadForMouse(button);
        return gb != null && gb.isPressed;
    }

    public static bool GetMouseButtonDown(int button)
    {
        var b = MouseButton(button);
        if (b != null && b.wasPressedThisFrame) return true;
        var gb = GamepadForMouse(button);
        return gb != null && gb.wasPressedThisFrame;
    }

    public static bool GetMouseButtonUp(int button)
    {
        var b = MouseButton(button);
        if (b != null && b.wasReleasedThisFrame) return true;
        var gb = GamepadForMouse(button);
        return gb != null && gb.wasReleasedThisFrame;
    }

    // ── 마우스 위치/휠 ────────────────────────────────────────
    public static Vector3 mousePosition
    {
        get
        {
            var m = Mouse.current;
            if (m == null) return Vector3.zero;
            Vector2 p = m.position.ReadValue();
            return new Vector3(p.x, p.y, 0f);
        }
    }

    /// <summary>
    /// 레거시 mouseScrollDelta 대응. 신 시스템 raw 스크롤 값은 플랫폼에 따라 ±120 등으로
    /// 스케일이 다를 수 있으나, 본 프로젝트는 부호(>0 / <0)만 사용하므로 그대로 전달한다.
    /// </summary>
    public static Vector2 mouseScrollDelta
    {
        get
        {
            var m = Mouse.current;
            return m == null ? Vector2.zero : m.scroll.ReadValue();
        }
    }

    // ── 가상 축 (레거시 Input Manager 기본값 재현) ────────────
    public static float GetAxisRaw(string axis)
    {
        switch (axis)
        {
            case "Horizontal":
            {
                var g = Gamepad.current;
                if (g != null)
                {
                    float d = DeadzoneAxis(g.leftStick.x.ReadValue());
                    if (d != 0f) return d;   // 스틱 밀면 아날로그 우선(재스케일)
                }
                float v = 0f;
                if (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow)) v += 1f;
                if (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow)) v -= 1f;
                return v;
            }
            case "Vertical":
            {
                var g = Gamepad.current;
                if (g != null)
                {
                    float d = DeadzoneAxis(g.leftStick.y.ReadValue());
                    if (d != 0f) return d;
                }
                float v = 0f;
                if (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow)) v += 1f;
                if (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow)) v -= 1f;
                return v;
            }
            default:
                return 0f;
        }
    }

    // ── 게임패드 ─────────────────────────────────────────────
    // 셰임 확장 (T0/T1). 조준은 오른쪽 스틱, 이동은 왼쪽 스틱, 버튼은 KeyCode/마우스에 병합.
    // 방향 소스 전환: 오른쪽 스틱/버튼을 쓰면 PadActive=true(마우스 조준 무시),
    // 마우스를 물리적으로 움직이면 false로 복귀 → 키보드·패드 매끄럽게 혼용.
    const float StickDeadzone = 0.30f;
    static bool _padActive;

    // 정적 셰임 필드는 도메인 리로드로만 초기화되므로, 플레이 시작마다 명시 리셋한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetState() { _padActive = false; }

    /// <summary>스틱 데드존 재스케일 — 경계에서 0→0.30 속도 점프 없이 매끄럽게 차오르게.</summary>
    static float DeadzoneAxis(float v)
    {
        float a = Mathf.Abs(v);
        if (a <= StickDeadzone) return 0f;
        return Mathf.Sign(v) * (a - StickDeadzone) / (1f - StickDeadzone);
    }

    /// <summary>현재 방향 입력이 게임패드 기준인지(마우스 조준을 대체). TopDownPlayer가 매 프레임 Tick 후 조회.</summary>
    public static bool PadActive => _padActive;

    /// <summary>오른쪽 스틱 조준 벡터(데드존 적용, 미입력 시 zero — 호출부가 마지막 방향 유지).</summary>
    public static Vector2 AimStick
    {
        get
        {
            var g = Gamepad.current;
            if (g == null) return Vector2.zero;
            Vector2 v = g.rightStick.ReadValue();
            return v.sqrMagnitude < StickDeadzone * StickDeadzone ? Vector2.zero : v;
        }
    }

    /// <summary>매 프레임 1회 호출 — 패드/마우스 마지막 사용 디바이스로 PadActive 갱신.</summary>
    public static void Tick()
    {
        var g = Gamepad.current;
        if (g != null)
        {
            Vector2 rs = g.rightStick.ReadValue();
            Vector2 ls = g.leftStick.ReadValue();
            float dz2 = StickDeadzone * StickDeadzone;
            if (rs.sqrMagnitude > dz2 || ls.sqrMagnitude > dz2 || AnyPadButtonPressed(g))
                _padActive = true;
        }
        var m = Mouse.current;
        if (m != null && (m.delta.ReadValue().sqrMagnitude > 4f
                          || m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame))
            _padActive = false;
    }

    static bool AnyPadButtonPressed(Gamepad g)
    {
        return g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame
            || g.buttonNorth.wasPressedThisFrame || g.buttonWest.wasPressedThisFrame
            || g.leftTrigger.wasPressedThisFrame || g.rightTrigger.wasPressedThisFrame
            || g.leftShoulder.wasPressedThisFrame || g.rightShoulder.wasPressedThisFrame
            || g.startButton.wasPressedThisFrame || g.selectButton.wasPressedThisFrame
            || g.leftStickButton.wasPressedThisFrame || g.rightStickButton.wasPressedThisFrame
            || g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame
            || g.dpad.left.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame;
    }

    // KeyCode → 게임패드 버튼 매핑 (레이드 조작 T1).
    //  E=상호작용→A(남) / Space=구르기→B(동) / LeftShift=달리기→L3 / C=앉기→Y(북)
    //  Escape=일시정지·닫기→Start / Tab=인벤·캐릭터→Select / Alpha1~4=퀵슬롯→D패드(상우하좌)
    static ButtonControl GamepadButtonFor(KeyCode code)
    {
        var g = Gamepad.current;
        if (g == null) return null;
        switch (code)
        {
            case KeyCode.E:         return g.buttonSouth;
            case KeyCode.Space:     return g.buttonEast;
            case KeyCode.LeftShift: return g.leftStickButton;
            case KeyCode.C:         return g.buttonNorth;
            case KeyCode.Escape:    return g.startButton;
            case KeyCode.Tab:       return g.selectButton;
            case KeyCode.Alpha1:    return g.dpad.up;
            case KeyCode.Alpha2:    return g.dpad.right;
            case KeyCode.Alpha3:    return g.dpad.down;
            case KeyCode.Alpha4:    return g.dpad.left;
            default:                return null;
        }
    }

    // ── KeyCode → Key 매핑 ────────────────────────────────────
    static readonly Dictionary<KeyCode, Key> _map = BuildMap();

    static Key Map(KeyCode code)
    {
        return _map.TryGetValue(code, out var key) ? key : Key.None;
    }

    static Dictionary<KeyCode, Key> BuildMap()
    {
        var d = new Dictionary<KeyCode, Key>(128);

        // 알파벳 A~Z (양쪽 enum 모두 알파벳순 연속)
        for (int i = 0; i <= (KeyCode.Z - KeyCode.A); i++)
            d[KeyCode.A + i] = Key.A + i;

        // 상단 숫자열 0~9
        d[KeyCode.Alpha0] = Key.Digit0; d[KeyCode.Alpha1] = Key.Digit1;
        d[KeyCode.Alpha2] = Key.Digit2; d[KeyCode.Alpha3] = Key.Digit3;
        d[KeyCode.Alpha4] = Key.Digit4; d[KeyCode.Alpha5] = Key.Digit5;
        d[KeyCode.Alpha6] = Key.Digit6; d[KeyCode.Alpha7] = Key.Digit7;
        d[KeyCode.Alpha8] = Key.Digit8; d[KeyCode.Alpha9] = Key.Digit9;

        // 키패드 숫자열 0~9
        d[KeyCode.Keypad0] = Key.Numpad0; d[KeyCode.Keypad1] = Key.Numpad1;
        d[KeyCode.Keypad2] = Key.Numpad2; d[KeyCode.Keypad3] = Key.Numpad3;
        d[KeyCode.Keypad4] = Key.Numpad4; d[KeyCode.Keypad5] = Key.Numpad5;
        d[KeyCode.Keypad6] = Key.Numpad6; d[KeyCode.Keypad7] = Key.Numpad7;
        d[KeyCode.Keypad8] = Key.Numpad8; d[KeyCode.Keypad9] = Key.Numpad9;
        d[KeyCode.KeypadEnter] = Key.NumpadEnter;
        d[KeyCode.KeypadDivide] = Key.NumpadDivide;
        d[KeyCode.KeypadMultiply] = Key.NumpadMultiply;
        d[KeyCode.KeypadMinus] = Key.NumpadMinus;
        d[KeyCode.KeypadPlus] = Key.NumpadPlus;
        d[KeyCode.KeypadPeriod] = Key.NumpadPeriod;

        // 펑션키 F1~F12
        for (int i = 0; i <= (KeyCode.F12 - KeyCode.F1); i++)
            d[KeyCode.F1 + i] = Key.F1 + i;

        // 방향키
        d[KeyCode.UpArrow] = Key.UpArrow;
        d[KeyCode.DownArrow] = Key.DownArrow;
        d[KeyCode.LeftArrow] = Key.LeftArrow;
        d[KeyCode.RightArrow] = Key.RightArrow;

        // 수식/제어키
        d[KeyCode.LeftShift] = Key.LeftShift;
        d[KeyCode.RightShift] = Key.RightShift;
        d[KeyCode.LeftControl] = Key.LeftCtrl;
        d[KeyCode.RightControl] = Key.RightCtrl;
        d[KeyCode.LeftAlt] = Key.LeftAlt;
        d[KeyCode.RightAlt] = Key.RightAlt;
        d[KeyCode.LeftCommand] = Key.LeftMeta;
        d[KeyCode.RightCommand] = Key.RightMeta;

        // 기타 자주 쓰는 키
        d[KeyCode.Space] = Key.Space;
        d[KeyCode.Return] = Key.Enter;
        d[KeyCode.Escape] = Key.Escape;
        d[KeyCode.Tab] = Key.Tab;
        d[KeyCode.Backspace] = Key.Backspace;
        d[KeyCode.Delete] = Key.Delete;
        d[KeyCode.Insert] = Key.Insert;
        d[KeyCode.Home] = Key.Home;
        d[KeyCode.End] = Key.End;
        d[KeyCode.PageUp] = Key.PageUp;
        d[KeyCode.PageDown] = Key.PageDown;
        d[KeyCode.CapsLock] = Key.CapsLock;
        d[KeyCode.Minus] = Key.Minus;
        d[KeyCode.Equals] = Key.Equals;
        d[KeyCode.LeftBracket] = Key.LeftBracket;
        d[KeyCode.RightBracket] = Key.RightBracket;
        d[KeyCode.Semicolon] = Key.Semicolon;
        d[KeyCode.Quote] = Key.Quote;
        d[KeyCode.BackQuote] = Key.Backquote;
        d[KeyCode.Comma] = Key.Comma;
        d[KeyCode.Period] = Key.Period;
        d[KeyCode.Slash] = Key.Slash;
        d[KeyCode.Backslash] = Key.Backslash;

        return d;
    }
}
