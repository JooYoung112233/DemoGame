using UnityEngine;

public class FogOfWarSystem : MonoBehaviour
{
    [Header("Fog Settings")]
    [SerializeField] int mapWidth = 64;
    [SerializeField] int mapHeight = 64;
    [SerializeField] float cellSize = 1f;
    [SerializeField] Vector2 mapOrigin = new Vector2(-32, -32);

    [Header("Vision")]
    [SerializeField] int dayVisionRadius = 6;
    [SerializeField] int nightBaseRadius = 2;
    [SerializeField] int flashlightRadius = 7;
    [SerializeField] float flashlightAngle = 90f;

    [Header("Rendering")]
    [SerializeField] Material fogMaterial;
    [SerializeField] Color unexploredColor = new Color(0, 0, 0, 1f);
    [SerializeField] Color exploredDimColor = new Color(0, 0, 0, 0.7f);
    [SerializeField] float revealSpeed = 8f;

    [Header("References")]
    [SerializeField] Transform player;
    [SerializeField] FlashlightController flashlight;
    [SerializeField] DayNightCycle dayNight;

    Texture2D fogTexture;
    Color[] fogColors;
    byte[] explored;
    byte[] currentVisible;
    MeshRenderer fogRenderer;

    void Start()
    {
        CreateFogMesh();
        fogTexture = new Texture2D(mapWidth, mapHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        fogColors = new Color[mapWidth * mapHeight];
        explored = new byte[mapWidth * mapHeight];
        currentVisible = new byte[mapWidth * mapHeight];

        for (int i = 0; i < fogColors.Length; i++)
            fogColors[i] = unexploredColor;

        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();

        if (fogMaterial != null)
            fogMaterial.mainTexture = fogTexture;
        else
            fogRenderer.material.mainTexture = fogTexture;
    }

    void CreateFogMesh()
    {
        var go = new GameObject("FogMesh");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(
            mapOrigin.x + mapWidth * cellSize * 0.5f,
            mapOrigin.y + mapHeight * cellSize * 0.5f,
            0
        );
        go.transform.localScale = new Vector3(mapWidth * cellSize, mapHeight * cellSize, 1);

        var meshFilter = go.AddComponent<MeshFilter>();
        fogRenderer = go.AddComponent<MeshRenderer>();

        var mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new(-0.5f, -0.5f, 0), new(0.5f, -0.5f, 0),
            new(0.5f, 0.5f, 0), new(-0.5f, 0.5f, 0)
        };
        mesh.uv = new Vector2[]
        {
            new(0, 0), new(1, 0), new(1, 1), new(0, 1)
        };
        mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
        meshFilter.mesh = mesh;

        if (fogMaterial != null)
            fogRenderer.material = new Material(fogMaterial);
        else
            fogRenderer.material = new Material(Shader.Find("Sprites/Default"));

        fogRenderer.sortingOrder = 100;
    }

    void Update()
    {
        if (player == null) return;

        System.Array.Clear(currentVisible, 0, currentVisible.Length);

        Vector2Int playerCell = WorldToCell(player.position);
        bool isNight = dayNight != null && dayNight.IsNight;

        if (isNight)
        {
            RevealCircle(playerCell, nightBaseRadius);

            if (flashlight != null && flashlight.IsOn)
            {
                Vector2 facingDir = player.GetComponent<PlayerController>()?.FacingDirection ?? Vector2.right;
                RevealCone(playerCell, facingDir, flashlightRadius, flashlightAngle);
            }
        }
        else
        {
            RevealCircle(playerCell, dayVisionRadius);
        }

        UpdateFogTexture();
    }

    void RevealCircle(Vector2Int center, int radius)
    {
        int r2 = radius * radius;
        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > r2) continue;
                int cx = center.x + dx;
                int cy = center.y + dy;
                if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight) continue;

                int idx = cy * mapWidth + cx;
                currentVisible[idx] = 1;
                explored[idx] = 1;
            }
        }
    }

    void RevealCone(Vector2Int center, Vector2 direction, int radius, float angle)
    {
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;
        float dirAngle = Mathf.Atan2(direction.y, direction.x);
        int r2 = radius * radius;

        for (int dy = -radius; dy <= radius; dy++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                if (dx * dx + dy * dy > r2) continue;
                if (dx == 0 && dy == 0) continue;

                float cellAngle = Mathf.Atan2(dy, dx);
                float angleDiff = Mathf.Abs(Mathf.DeltaAngle(dirAngle * Mathf.Rad2Deg, cellAngle * Mathf.Rad2Deg)) * Mathf.Deg2Rad;

                if (angleDiff > halfAngle) continue;

                int cx = center.x + dx;
                int cy = center.y + dy;
                if (cx < 0 || cx >= mapWidth || cy < 0 || cy >= mapHeight) continue;

                int idx = cy * mapWidth + cx;
                currentVisible[idx] = 1;
                explored[idx] = 1;
            }
        }
    }

    void UpdateFogTexture()
    {
        float dt = Time.deltaTime * revealSpeed;
        for (int i = 0; i < fogColors.Length; i++)
        {
            Color target;
            if (currentVisible[i] == 1)
                target = Color.clear;
            else if (explored[i] == 1)
                target = exploredDimColor;
            else
                target = unexploredColor;

            fogColors[i] = Color.Lerp(fogColors[i], target, dt);
        }

        fogTexture.SetPixels(fogColors);
        fogTexture.Apply();
    }

    Vector2Int WorldToCell(Vector3 worldPos)
    {
        int x = Mathf.FloorToInt((worldPos.x - mapOrigin.x) / cellSize);
        int y = Mathf.FloorToInt((worldPos.y - mapOrigin.y) / cellSize);
        return new Vector2Int(
            Mathf.Clamp(x, 0, mapWidth - 1),
            Mathf.Clamp(y, 0, mapHeight - 1)
        );
    }
}
