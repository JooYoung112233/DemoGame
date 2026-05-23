using UnityEngine;
using System.Collections.Generic;

public class ViewCulling : MonoBehaviour
{
    [Header("Culling")]
    [SerializeField] float padding = 5f;
    [SerializeField] float updateInterval = 0.15f;
    [SerializeField] bool cullRenderers = true;
    [SerializeField] bool cullLights = true;
    [SerializeField] bool cullAI = true;
    [SerializeField] bool cullParticles = true;

    [Header("Debug")]
    [SerializeField] bool showBounds;

    Camera cam;
    float timer;
    Bounds viewBounds;

    // 관리 대상 — 한 번 수집 후 캐시
    List<CullTarget> targets = new List<CullTarget>();
    bool collected;

    // Player는 절대 컬링하지 않음
    Transform playerTransform;

    struct CullTarget
    {
        public Transform transform;
        public Renderer renderer;
        public Light light;
        public MonoBehaviour ai;
        public ParticleSystem particle;
        public GameObject root;
        public bool wasActive;
    }

    void Start()
    {
        cam = Camera.main;
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
            playerTransform = playerGO.transform;

        Invoke(nameof(CollectTargets), 0.5f);
    }

    void CollectTargets()
    {
        targets.Clear();

        if (cullRenderers)
            CollectRenderers();
        if (cullLights)
            CollectLights();
        if (cullAI)
            CollectAI();
        if (cullParticles)
            CollectParticles();

        collected = true;
    }

    void CollectRenderers()
    {
        foreach (var r in FindObjectsOfType<Renderer>(true))
        {
            if (IsPlayer(r.transform)) continue;
            if (IsChild(r.transform, "PrototypeHUD")) continue;
            if (IsChild(r.transform, "GameManager")) continue;
            if (r is ParticleSystemRenderer) continue;

            // HP바 등 UI 요소는 부모와 함께 움직이므로 최상위만
            if (r.GetComponentInParent<HealthBar3D>() != null)
                continue;

            targets.Add(new CullTarget
            {
                transform = r.transform,
                renderer = r,
                root = r.gameObject,
                wasActive = r.enabled
            });
        }
    }

    void CollectLights()
    {
        foreach (var l in FindObjectsOfType<Light>(true))
        {
            if (IsPlayer(l.transform)) continue;
            if (l.type == LightType.Directional) continue;

            targets.Add(new CullTarget
            {
                transform = l.transform,
                light = l,
                root = l.gameObject,
                wasActive = l.enabled
            });
        }
    }

    void CollectAI()
    {
        foreach (var ai in FindObjectsOfType<EnemyAI>(true))
        {
            targets.Add(new CullTarget
            {
                transform = ai.transform,
                ai = ai,
                root = ai.gameObject,
                wasActive = ai.enabled
            });
        }
    }

    void CollectParticles()
    {
        foreach (var ps in FindObjectsOfType<ParticleSystem>(true))
        {
            if (IsPlayer(ps.transform)) continue;

            targets.Add(new CullTarget
            {
                transform = ps.transform,
                particle = ps,
                root = ps.gameObject,
                wasActive = ps.gameObject.activeSelf
            });
        }
    }

    void Update()
    {
        if (!collected || cam == null) return;

        timer -= Time.deltaTime;
        if (timer > 0) return;
        timer = updateInterval;

        ComputeViewBounds();
        UpdateCulling();
    }

    void ComputeViewBounds()
    {
        float orthoH = cam.orthographicSize;
        float orthoW = orthoH * cam.aspect;

        // 오쏘 카메라가 바라보는 XZ 평면 범위 계산
        // 카메라가 기울어져 있으므로 바닥 평면(Y=0)에 투영
        Vector3 camPos = cam.transform.position;
        Vector3 camFwd = cam.transform.forward;
        Vector3 camRight = cam.transform.right;
        Vector3 camUp = cam.transform.up;

        // 뷰 4 코너를 Y=0 평면에 레이캐스트
        Vector3[] corners = new Vector3[4];
        corners[0] = camPos + (-camRight * orthoW + camUp * orthoH); // top-left
        corners[1] = camPos + (camRight * orthoW + camUp * orthoH);  // top-right
        corners[2] = camPos + (-camRight * orthoW - camUp * orthoH); // bot-left
        corners[3] = camPos + (camRight * orthoW - camUp * orthoH);  // bot-right

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        Plane ground = new Plane(Vector3.up, Vector3.zero);

        for (int i = 0; i < 4; i++)
        {
            Ray ray = new Ray(corners[i], camFwd);
            if (ground.Raycast(ray, out float dist))
            {
                Vector3 hit = ray.GetPoint(dist);
                minX = Mathf.Min(minX, hit.x);
                maxX = Mathf.Max(maxX, hit.x);
                minZ = Mathf.Min(minZ, hit.z);
                maxZ = Mathf.Max(maxZ, hit.z);
            }
            else
            {
                // 카메라가 바닥을 안 보는 경우 폴백
                Vector3 flat = corners[i];
                flat.y = 0;
                minX = Mathf.Min(minX, flat.x);
                maxX = Mathf.Max(maxX, flat.x);
                minZ = Mathf.Min(minZ, flat.z);
                maxZ = Mathf.Max(maxZ, flat.z);
            }
        }

        // 패딩 적용
        minX -= padding;
        maxX += padding;
        minZ -= padding;
        maxZ += padding;

        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 100f, maxZ - minZ);
        viewBounds = new Bounds(center, size);
    }

    void UpdateCulling()
    {
        for (int i = targets.Count - 1; i >= 0; i--)
        {
            var t = targets[i];

            // 파괴된 오브젝트 제거
            if (t.root == null || t.transform == null)
            {
                targets.RemoveAt(i);
                continue;
            }

            Vector3 pos = t.transform.position;
            bool inView = viewBounds.Contains(new Vector3(pos.x, 0, pos.z));

            if (t.renderer != null)
                t.renderer.enabled = inView;
            else if (t.light != null)
                t.light.enabled = inView;
            else if (t.ai != null)
                t.ai.enabled = inView;
            else if (t.particle != null)
            {
                if (inView && !t.particle.isPlaying)
                    t.particle.Play();
                else if (!inView && t.particle.isPlaying)
                    t.particle.Pause();
            }
        }
    }

    void OnDisable()
    {
        // 시스템 비활성화 시 모든 대상 복원
        foreach (var t in targets)
        {
            if (t.root == null) continue;
            if (t.renderer != null) t.renderer.enabled = true;
            if (t.light != null) t.light.enabled = true;
            if (t.ai != null) t.ai.enabled = true;
            if (t.particle != null && !t.particle.isPlaying) t.particle.Play();
        }
    }

    bool IsPlayer(Transform t)
    {
        if (playerTransform == null) return false;
        return t == playerTransform || t.IsChildOf(playerTransform);
    }

    static bool IsChild(Transform t, string rootName)
    {
        var current = t;
        while (current != null)
        {
            if (current.name == rootName) return true;
            current = current.parent;
        }
        return false;
    }

    // 새로 스폰된 오브젝트 등록
    public void Register(GameObject go)
    {
        if (go == null) return;

        var r = go.GetComponent<Renderer>();
        if (r != null)
        {
            targets.Add(new CullTarget
            {
                transform = r.transform,
                renderer = r,
                root = r.gameObject,
                wasActive = r.enabled
            });
        }

        var l = go.GetComponent<Light>();
        if (l != null && l.type != LightType.Directional)
        {
            targets.Add(new CullTarget
            {
                transform = l.transform,
                light = l,
                root = l.gameObject,
                wasActive = l.enabled
            });
        }

        var ai = go.GetComponent<EnemyAI>();
        if (ai != null)
        {
            targets.Add(new CullTarget
            {
                transform = ai.transform,
                ai = ai,
                root = ai.gameObject,
                wasActive = ai.enabled
            });
        }
    }

    void OnDrawGizmos()
    {
        if (!showBounds || viewBounds.size == Vector3.zero) return;

        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Vector3 drawCenter = viewBounds.center;
        drawCenter.y = 0.2f;
        Vector3 drawSize = viewBounds.size;
        drawSize.y = 0.4f;
        Gizmos.DrawCube(drawCenter, drawSize);

        Gizmos.color = new Color(0, 1, 0, 0.6f);
        Gizmos.DrawWireCube(drawCenter, drawSize);
    }
}
