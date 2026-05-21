using UnityEngine;

/// <summary>
/// 마우스 위치에 크로스헤어 표시 + 적 타겟팅.
/// 적이 사거리 안이면 빨간색, 밖이면 노란색, 없으면 흰색.
/// </summary>
public class CrosshairUI : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] float size = 22f;
    [SerializeField] float gap = 5f;
    [SerializeField] float thickness = 2f;
    [SerializeField] float selectRadius = 1.0f; // 마우스 월드 좌표 기준 선택 반경

    [Header("Colors")]
    [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] Color inRangeColor = new Color(1f, 0.2f, 0.2f, 1f);
    [SerializeField] Color outOfRangeColor = new Color(1f, 1f, 0.3f, 0.7f);

    Camera mainCam;
    Texture2D pixel;
    Transform playerTransform;

    EnemyAI targetedEnemy;
    EnemyOutline currentOutline;
    bool isTargetInRange;
    float attackRange;

    public EnemyAI TargetedEnemy => targetedEnemy;
    public bool IsTargetInRange => isTargetInRange;
    public Vector3 MouseWorldPos { get; private set; }

    void Start()
    {
        mainCam = Camera.main;
        Cursor.visible = false;

        pixel = new Texture2D(1, 1);
        pixel.SetPixel(0, 0, Color.white);
        pixel.Apply();

        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null) playerTransform = playerGO.transform;
    }

    void Update()
    {
        if (mainCam == null) return;

        // 마우스 → 월드 좌표 (바닥 평면)
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
            MouseWorldPos = ray.GetPoint(dist);

        // 공격 사거리 갱신
        if (playerTransform != null)
        {
            var combat = playerTransform.GetComponent<PlayerCombat>();
            if (combat != null) attackRange = combat.GetAttackRange();
        }

        UpdateTarget();
    }

    void UpdateTarget()
    {
        // 가장 가까운 적 찾기
        var enemies = FindObjectsOfType<EnemyAI>();
        EnemyAI closest = null;
        float closestDist = selectRadius;

        Vector3 mouseFlat = MouseWorldPos;
        mouseFlat.y = 0;

        foreach (var e in enemies)
        {
            var hp = e.GetComponent<Health>();
            if (hp != null && hp.IsDead) continue;

            Vector3 ePos = e.transform.position;
            ePos.y = 0;
            float d = Vector3.Distance(ePos, mouseFlat);
            if (d < closestDist)
            {
                closestDist = d;
                closest = e;
            }
        }

        // 아웃라인 갱신
        if (closest != targetedEnemy)
        {
            if (currentOutline != null) currentOutline.SetHighlight(false);
            targetedEnemy = closest;
            currentOutline = targetedEnemy != null
                ? targetedEnemy.GetComponent<EnemyOutline>()
                : null;
        }

        // 사거리 판정
        if (targetedEnemy != null && playerTransform != null)
        {
            float distToPlayer = Vector3.Distance(
                playerTransform.position, targetedEnemy.transform.position);
            isTargetInRange = distToPlayer <= attackRange;

            if (currentOutline != null)
                currentOutline.SetHighlight(true, isTargetInRange);
        }
        else
        {
            isTargetInRange = false;
            if (currentOutline != null)
            {
                currentOutline.SetHighlight(false);
                currentOutline = null;
            }
        }
    }

    void OnGUI()
    {
        Vector2 c = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);

        Color color = normalColor;
        if (targetedEnemy != null)
            color = isTargetInRange ? inRangeColor : outOfRangeColor;

        GUI.color = color;

        float half = size * 0.5f;
        float g = gap;
        float t = thickness;

        // 상하좌우 선
        GUI.DrawTexture(new Rect(c.x - t * 0.5f, c.y - half, t, half - g), pixel);
        GUI.DrawTexture(new Rect(c.x - t * 0.5f, c.y + g, t, half - g), pixel);
        GUI.DrawTexture(new Rect(c.x - half, c.y - t * 0.5f, half - g, t), pixel);
        GUI.DrawTexture(new Rect(c.x + g, c.y - t * 0.5f, half - g, t), pixel);

        // 중앙 점
        GUI.DrawTexture(new Rect(c.x - 1, c.y - 1, 2, 2), pixel);

        GUI.color = Color.white;
    }

    void OnDestroy() => Cursor.visible = true;
    void OnApplicationFocus(bool focus) => Cursor.visible = !focus;
}
