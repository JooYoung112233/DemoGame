using UnityEngine;

/// <summary>
/// 3D 월드에 표시되는 HP바. 카메라를 향해 빌보드.
/// </summary>
public class HealthBar3D : MonoBehaviour
{
    [SerializeField] Health health;
    [SerializeField] Vector3 offset = new Vector3(0, 1.2f, 0);
    [SerializeField] float barWidth = 0.8f;
    [SerializeField] float barHeight = 0.08f;
    [SerializeField] Color fullColor = Color.green;
    [SerializeField] Color lowColor = Color.red;
    [SerializeField] bool hideWhenFull = true;

    GameObject barBg;
    GameObject barFill;
    Material fillMat;
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        CreateBar();

        if (health != null)
        {
            health.OnDamaged += (_) => UpdateBar();
            health.OnDeath += () => gameObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (mainCam == null || health == null) return;

        // 빌보드
        transform.rotation = mainCam.transform.rotation;

        // 위치 (부모 기준)
        if (transform.parent != null)
            transform.position = transform.parent.position + offset;

        // HP 풀일 때 숨기기
        if (hideWhenFull)
        {
            bool show = health.Percent < 0.99f;
            barBg.SetActive(show);
            barFill.SetActive(show);
        }
    }

    void CreateBar()
    {
        // 배경 (어두운 바)
        barBg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barBg.name = "HPBar_BG";
        barBg.transform.SetParent(transform);
        barBg.transform.localPosition = Vector3.zero;
        barBg.transform.localScale = new Vector3(barWidth, barHeight, 1);
        barBg.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(barBg.GetComponent<MeshCollider>());
        var bgMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        bgMat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f, 0.8f));
        bgMat.SetFloat("_Surface", 1);
        bgMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bgMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bgMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bgMat.renderQueue = 3200;
        barBg.GetComponent<MeshRenderer>().sharedMaterial = bgMat;
        barBg.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // 체력바
        barFill = GameObject.CreatePrimitive(PrimitiveType.Quad);
        barFill.name = "HPBar_Fill";
        barFill.transform.SetParent(transform);
        barFill.transform.localPosition = new Vector3(0, 0, -0.001f);
        barFill.transform.localScale = new Vector3(barWidth, barHeight, 1);
        barFill.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(barFill.GetComponent<MeshCollider>());
        fillMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        fillMat.SetColor("_BaseColor", fullColor);
        fillMat.SetFloat("_Surface", 1);
        fillMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        fillMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        fillMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        fillMat.renderQueue = 3201;
        barFill.GetComponent<MeshRenderer>().sharedMaterial = fillMat;
        barFill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void UpdateBar()
    {
        if (health == null || barFill == null) return;
        float pct = health.Percent;

        // 스케일 조절 (왼쪽 정렬)
        barFill.transform.localScale = new Vector3(barWidth * pct, barHeight, 1);
        barFill.transform.localPosition = new Vector3(
            -barWidth * (1 - pct) * 0.5f, 0, -0.001f
        );

        // 색상 보간
        fillMat.SetColor("_BaseColor", Color.Lerp(lowColor, fullColor, pct));
    }
}
