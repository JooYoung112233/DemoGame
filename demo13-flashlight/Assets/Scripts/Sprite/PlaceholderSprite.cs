using UnityEngine;

/// <summary>
/// 임시 placeholder 스프라이트 제공 (아트 에셋 들어오기 전 prototyping용).
/// 1x1 흰 사각/원. 색은 SpriteRenderer.color로 틴트.
/// </summary>
public static class PlaceholderSprite
{
    static Sprite _square;
    static Sprite _circle;

    /// <summary>1유닛 흰 사각 스프라이트.</summary>
    public static Sprite Square
    {
        get
        {
            if (_square == null)
            {
                var tex = Texture2D.whiteTexture;
                _square = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
                _square.name = "PlaceholderSquare";
            }
            return _square;
        }
    }

    /// <summary>1유닛 흰 원 스프라이트 (32x32 절차 생성).</summary>
    public static Sprite Circle
    {
        get
        {
            if (_circle == null)
            {
                const int S = 32;
                var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
                float r = S * 0.5f;
                for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = x + 0.5f - r, dy = y + 0.5f - r;
                    bool inside = dx * dx + dy * dy <= r * r;
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
                tex.Apply();
                tex.filterMode = FilterMode.Bilinear;
                _circle = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
                _circle.name = "PlaceholderCircle";
            }
            return _circle;
        }
    }
}
