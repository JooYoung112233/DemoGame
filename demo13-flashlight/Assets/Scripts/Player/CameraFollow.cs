using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 카메라 추적. 에디터에서 설정한 위치/회전/orthoSize를 그대로 유지.
/// Play 시 플레이어와의 오프셋만 계산해서 따라감.
/// 씬 전환 후에도 DontDestroyOnLoad 플레이어를 자동 재탐색.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float smoothSpeed = 8f;

    Vector3 offset;
    Camera cam;
    bool offsetInitialized;

    void Start()
    {
        cam = GetComponent<Camera>();
        FindTarget();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 씬 전환 시 오프셋 재계산 허용
        offsetInitialized = false;
        target = null;
        FindTarget();
    }

    void FindTarget()
    {
        if (target == null)
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
                target = playerGO.transform;
        }

        if (target != null && !offsetInitialized)
        {
            offset = transform.position - target.position;
            offsetInitialized = true;

            // 씬 전환 직후 즉시 스냅 (Lerp 지연 없이)
            transform.position = target.position + offset;
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            FindTarget();
            if (target == null) return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.unscaledDeltaTime);
    }
}
