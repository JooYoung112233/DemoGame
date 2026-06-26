using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전역 UI 테마 — Tarkov 거래창 톤의 단일 진실(SSOT) 팔레트.
/// 파란끼를 제거한 어두운 웜그레이/올리브 배경 + 탄(tan) 강조.
/// 모든 절차적 UI(코드 빌드 uGUI)는 색을 여기서 가져와 통일감을 유지한다.
///
/// 사용 예:
///   img.color = UITheme.Panel;
///   UITheme.Configure(text, "제목", 16, TextAnchor.MiddleLeft, UITheme.TextBright);
/// </summary>
public static class UITheme
{
    // ── 배경 계층 (어두움 → 밝음) ─────────────────────────────
    public static readonly Color Backdrop   = new Color(0.085f, 0.082f, 0.072f, 0.99f);  // 전체화면 모달 배경
    public static readonly Color Panel       = new Color(0.135f, 0.130f, 0.115f, 1.00f);  // 패널/카드
    public static readonly Color PanelAlt    = new Color(0.115f, 0.110f, 0.098f, 1.00f);  // 슬롯/리스트 배경
    public static readonly Color Header       = new Color(0.155f, 0.150f, 0.130f, 1.00f);  // 헤더 바
    public static readonly Color Cell         = new Color(0.175f, 0.168f, 0.148f, 1.00f);  // 셀
    public static readonly Color CellHover    = new Color(0.24f,  0.23f,  0.20f,  1.00f);
    public static readonly Color CellPressed  = new Color(0.28f,  0.24f,  0.14f,  1.00f);
    public static readonly Color Gridline     = new Color(0.05f,  0.048f, 0.042f, 1.00f);  // 셀 사이 그리드선
    public static readonly Color Divider      = new Color(0.26f,  0.25f,  0.21f,  1.00f);  // 구분선

    // ── 강조 ─────────────────────────────────────────────────
    public static readonly Color Accent       = new Color(0.40f,  0.34f,  0.18f,  1.00f);  // 선택(탄)
    public static readonly Color AccentBright  = new Color(0.62f,  0.54f,  0.30f,  1.00f);  // 강조 텍스트/테두리
    public static readonly Color Gold          = new Color(0.86f,  0.74f,  0.42f,  1.00f);  // 가격/통화

    // ── 텍스트 ───────────────────────────────────────────────
    public static readonly Color TextBright   = new Color(0.92f,  0.90f,  0.84f,  1.00f);  // 기본 밝은 글자
    public static readonly Color TextMuted    = new Color(0.54f,  0.51f,  0.44f,  1.00f);  // 보조/뮤트
    public static readonly Color TextDim       = new Color(0.40f,  0.38f,  0.33f,  1.00f);  // 비활성

    // ── 시맨틱 액션 ──────────────────────────────────────────
    public static readonly Color Buy          = new Color(0.20f,  0.34f,  0.16f,  1.00f);  // 올리브 그린(구매/긍정)
    public static readonly Color BuyHi        = new Color(0.28f,  0.46f,  0.22f,  1.00f);
    public static readonly Color Sell         = new Color(0.46f,  0.28f,  0.10f,  1.00f);  // 갈색(판매)
    public static readonly Color SellHi       = new Color(0.60f,  0.38f,  0.14f,  1.00f);
    public static readonly Color Danger       = new Color(0.40f,  0.14f,  0.12f,  1.00f);  // 닫기/위험
    public static readonly Color Positive     = new Color(0.50f,  0.90f,  0.60f,  1.00f);  // 성공 텍스트
    public static readonly Color Negative     = new Color(0.92f,  0.42f,  0.32f,  1.00f);  // 실패 텍스트

    /// <summary>희귀도별 아이템 셀 배경색 — 인벤/상점/창고 격자 공통(SSOT).</summary>
    public static Color RarityBg(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:    return new Color(0.25f, 0.25f, 0.30f, 0.9f);
            case ItemRarity.Uncommon:  return new Color(0.15f, 0.30f, 0.15f, 0.9f);
            case ItemRarity.Rare:      return new Color(0.15f, 0.20f, 0.40f, 0.9f);
            case ItemRarity.Epic:      return new Color(0.25f, 0.15f, 0.35f, 0.9f);
            case ItemRarity.Legendary: return new Color(0.35f, 0.30f, 0.10f, 0.9f);
            default:                   return new Color(0.20f, 0.20f, 0.25f, 0.9f);
        }
    }

    static Font _font;
    public static Font Font =>
        _font != null ? _font : (_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

    /// <summary>Text 컴포넌트를 테마 폰트로 일괄 설정.</summary>
    public static void Configure(Text t, string text, int size, TextAnchor anchor, Color color)
    {
        if (t == null) return;
        t.font      = Font;
        t.fontSize  = size;
        t.alignment = anchor;
        t.color     = color;
        t.text      = text;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
    }
}
