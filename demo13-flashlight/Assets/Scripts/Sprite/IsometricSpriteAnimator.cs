using UnityEngine;

/// <summary>
/// 8방향 그리드 스프라이트 시트 애니메이션.
/// 시트는 columns x rows 그리드, 왼쪽→오른쪽, 위→아래 순서.
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

    // 방향: 0=Bot, 1=LeftBot, 2=Left, 3=LeftTop, 4=Top, 5=RightTop, 6=Right, 7=RightBot
    string currentAnim = "";
    int currentDir = 0;
    int currentFrame = 0;
    float frameTimer;
    bool isPlaying = false;
    bool looping = true;
    Material mat;
    System.Action onAnimComplete;

    public string CurrentAnim => currentAnim;
    public bool IsAnimComplete { get; private set; }

    void Start()
    {
        if (targetRenderer != null)
        {
            mat = targetRenderer.material;
            Play("idle");
        }
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
        float angle = Mathf.Atan2(worldDir.x, worldDir.z) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

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

        mat.SetTexture("_MainTex", sheet.texture);
        mat.SetFloat("_Columns", sheet.columns);
        mat.SetFloat("_Rows", sheet.rows);
        mat.SetFloat("_CurrentFrame", currentFrame);
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
        public int columns;
        public int rows;
        public int frameCount;
    }
}
