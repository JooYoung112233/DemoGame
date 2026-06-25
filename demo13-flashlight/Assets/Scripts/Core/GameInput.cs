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
    // ── 키보드 ───────────────────────────────────────────────
    public static bool GetKey(KeyCode code)
    {
        var k = Keyboard.current;
        if (k == null) return false;
        var key = Map(code);
        return key != Key.None && k[key].isPressed;
    }

    public static bool GetKeyDown(KeyCode code)
    {
        var k = Keyboard.current;
        if (k == null) return false;
        var key = Map(code);
        return key != Key.None && k[key].wasPressedThisFrame;
    }

    public static bool GetKeyUp(KeyCode code)
    {
        var k = Keyboard.current;
        if (k == null) return false;
        var key = Map(code);
        return key != Key.None && k[key].wasReleasedThisFrame;
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

    public static bool GetMouseButton(int button)
    {
        var b = MouseButton(button);
        return b != null && b.isPressed;
    }

    public static bool GetMouseButtonDown(int button)
    {
        var b = MouseButton(button);
        return b != null && b.wasPressedThisFrame;
    }

    public static bool GetMouseButtonUp(int button)
    {
        var b = MouseButton(button);
        return b != null && b.wasReleasedThisFrame;
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
                float v = 0f;
                if (GetKey(KeyCode.D) || GetKey(KeyCode.RightArrow)) v += 1f;
                if (GetKey(KeyCode.A) || GetKey(KeyCode.LeftArrow)) v -= 1f;
                return v;
            }
            case "Vertical":
            {
                float v = 0f;
                if (GetKey(KeyCode.W) || GetKey(KeyCode.UpArrow)) v += 1f;
                if (GetKey(KeyCode.S) || GetKey(KeyCode.DownArrow)) v -= 1f;
                return v;
            }
            default:
                return 0f;
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
