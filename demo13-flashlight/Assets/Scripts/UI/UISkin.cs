using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시안 스프라이트 스킨 헬퍼 (ui-prefab-plan §4-B). 빌드 코드가 패널/버튼 Image에 9-slice 스프라이트를 입힌다.
/// `Assets/Resources/UI/Image/`의 스프라이트를 Resources.Load로 읽어 적용 — **스프라이트가 없으면(미임포트/미셋업)
/// 아무것도 안 하고 기존 색을 유지**(폴백 안전). 시맨틱 색(희귀도/HP/내구도 등)은 호출측 UITheme 유지.
///
/// 사전: Tools/TopDown/UI/9-slice 자산 셋업 (스프라이트 Sprite화 + 보더). 스킨 후 프리팹 재베이크.
/// </summary>
public static class UISkin
{
    const string Dir = "UI/Image/";   // Resources 경로 (확장자 없음)

    static Sprite Load(string key)
    {
        var s = Resources.Load<Sprite>(Dir + key);
        return s;   // null이면 호출측이 스킵(기존 색 유지)
    }

    /// <summary>Image에 9-slice 스프라이트 적용. tint 주면 그 색으로(시맨틱 색 보존용), 없으면 흰색(원색).</summary>
    public static void Slice(Image img, string key, Color? tint = null)
    {
        if (img == null) return;
        var s = Load(key);
        if (s == null) return;                 // 폴백: 스프라이트 없으면 기존 색 그대로
        img.sprite = s;
        img.type = Image.Type.Sliced;
        img.color = tint ?? Color.white;
    }

    // ── 역할별 단축 (시안 매핑표) ─────────────────────────────
    public static void Panel(Image img)            => Slice(img, "box");          // 패널/팝업 프레임
    public static void StoragePanel(Image img)     => Slice(img, "storage");      // 창고 큰 패널
    public static void IconBox(Image img, Color? tint = null) => Slice(img, "itembox", tint); // 아이콘 박스
    public static void Cell(Image img, Color? tint = null)    => Slice(img, "storage_box", tint); // 격자 셀
    public static void ButtonPrimary(Image img)    => Slice(img, "btn");          // 밝은 종이 버튼
    public static void ButtonSecondary(Image img)  => Slice(img, "btnb");         // 어두운 버튼
    public static void TabOn(Image img)            => Slice(img, "storage_btn_on");
    public static void TabOff(Image img)           => Slice(img, "storage_btn_off");
}
