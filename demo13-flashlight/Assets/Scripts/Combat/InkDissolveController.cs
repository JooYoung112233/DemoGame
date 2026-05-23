using UnityEngine;
using System.Collections;

public class InkDissolveController : MonoBehaviour
{
    [SerializeField] float dissolveDuration = 0.6f;
    [SerializeField] Renderer targetRenderer;

    Material instanceMaterial;
    static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    void Awake()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();

        if (targetRenderer != null)
            instanceMaterial = targetRenderer.material;
    }

    public void PlayDissolve()
    {
        StartCoroutine(DissolveRoutine());
    }

    IEnumerator DissolveRoutine()
    {
        float timer = 0f;

        while (timer < dissolveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dissolveDuration);

            if (instanceMaterial != null)
                instanceMaterial.SetFloat(DissolveAmountID, t);

            yield return null;
        }

        gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (instanceMaterial != null)
            Destroy(instanceMaterial);
    }
}
