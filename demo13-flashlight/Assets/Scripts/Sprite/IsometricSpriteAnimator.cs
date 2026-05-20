using UnityEngine;

/// <summary>
/// 8방향 스프라이트 시트 애니메이션.
/// 스프라이트 시트는 세로로 프레임이 나열된 형태.
/// 방향별로 별도 텍스처를 참조.
/// </summary>
public class IsometricSpriteAnimator : MonoBehaviour
{
    [Header("Animation Sheets (8-dir)")]
    [SerializeField] SpriteSheet[] idleSheets = new SpriteSheet[8];
    [SerializeField] SpriteSheet[] walkSheets = new SpriteSheet[8];
    [SerializeField] SpriteSheet[] attackSheets = new SpriteSheet[8];
    [SerializeField] SpriteSheet[] deathSheets = new SpriteSheet[8];
    [SerializeField] SpriteSheet[] getHitSheets = new SpriteSheet[8];

    [Header("Settings")]
    [SerializeField] float frameRate = 10f;
    [SerializeField] MeshRenderer targetRenderer;
    [SerializeField] float spriteScale = 1.5f;

    // 방향 순서: 0=Bot, 1=LeftBot, 2=Left, 3=LeftTop, 4=Top, 5=RightTop, 6=Right, 7=RightBot
    string currentAnim = "idle";
    int currentDir = 0;
    int currentFrame = 0;
    float frameTimer;
    bool isPlaying = true;
    bool looping = true;
    Material mat;
    System.Action onAnimComplete;

    public string CurrentAnim => currentAnim;
    public bool IsAnimComplete { get; private set; }

    void Start()
    {
        if (targetRenderer != null)
            mat = targetRenderer.material;
    }

    void Update()
    {
        if (!isPlaying || mat == null) return;

        frameTimer += Time.deltaTime;
        if (frameTimer < 1f / frameRate) return;
        frameTimer = 0f;

        var sheets = GetCurrentSheets();
        if (sheets == null || currentDir >= sheets.Length) return;
        var sheet = sheets[currentDir];
        if (sheet.texture == null) return;

        currentFrame++;
        if (currentFrame >= sheet.frameCount)
        {
            if (looping)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = sheet.frameCount - 1;
                isPlaying = false;
                IsAnimComplete = true;
                onAnimComplete?.Invoke();
                return;
            }
        }

        ApplyFrame(sheet);
    }

    public void SetDirection(Vector3 worldDir)
    {
        if (worldDir.sqrMagnitude < 0.01f) return;
        // 8방향 각도 계산 (XZ 평면)
        float angle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // 각도 → 8방향 인덱스
        // 0°=Top(4), 45°=RightTop(5), 90°=Right(6), 135°=RightBot(7)
        // 180°=Bot(0), 225°=LeftBot(1), 270°=Left(2), 315°=LeftTop(3)
        int dir = Mathf.RoundToInt(angle / 45f) % 8;
        int[] mapping = { 4, 5, 6, 7, 0, 1, 2, 3 };
        currentDir = mapping[dir];
    }

    public void Play(string animName, bool loop = true, System.Action onComplete = null)
    {
        if (currentAnim == animName && isPlaying) return;
        currentAnim = animName;
        currentFrame = 0;
        frameTimer = 0f;
        isPlaying = true;
        looping = loop;
        IsAnimComplete = false;
        onAnimComplete = onComplete;

        var sheets = GetCurrentSheets();
        if (sheets != null && currentDir < sheets.Length && sheets[currentDir].texture != null)
            ApplyFrame(sheets[currentDir]);
    }

    public void PlayOneShot(string animName, System.Action onComplete = null)
    {
        currentAnim = animName;
        currentFrame = 0;
        frameTimer = 0f;
        isPlaying = true;
        looping = false;
        IsAnimComplete = false;
        onAnimComplete = onComplete;

        var sheets = GetCurrentSheets();
        if (sheets != null && currentDir < sheets.Length && sheets[currentDir].texture != null)
            ApplyFrame(sheets[currentDir]);
    }

    void ApplyFrame(SpriteSheet sheet)
    {
        if (mat == null || sheet.texture == null) return;
        mat.mainTexture = sheet.texture;

        float frameH = 1f / sheet.frameCount;
        float y = 1f - (currentFrame + 1) * frameH;
        mat.mainTextureScale = new Vector2(1, frameH);
        mat.mainTextureOffset = new Vector2(0, y);
    }

    SpriteSheet[] GetCurrentSheets()
    {
        return currentAnim switch
        {
            "idle" => idleSheets,
            "walk" => walkSheets,
            "attack" => attackSheets,
            "death" => deathSheets,
            "gethit" => getHitSheets,
            _ => idleSheets
        };
    }

    [System.Serializable]
    public struct SpriteSheet
    {
        public Texture2D texture;
        public int frameCount;
    }
}
