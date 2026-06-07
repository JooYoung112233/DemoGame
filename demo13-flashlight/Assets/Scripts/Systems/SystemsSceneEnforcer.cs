using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Systems 씬의 글로벌 조명이 "유일 글로벌"이 되도록, 게임플레이 씬이 들고 온 다른
/// Global Light2D를 로드 시 비활성화한다. (카메라/AudioListener 중복은 CameraFollow가 처리)
///
/// 게임플레이 씬에 예전 SceneLightingBuilder로 만든 Global Light2D가 남아 있어도
/// 밝기가 2배로 겹치지 않게 막아 준다. Systems의 글로벌은 ownedGlobal로 지정해 보존.
///
/// "More than one global light" 경고 방지:
///   - Awake()에서 즉시 외부 글로벌을 끔 (Start보다 빠름)
///   - sceneLoaded 콜백에서 새로 로드된 씬의 글로벌도 끔
///   - 지연 정리: Awake() 직후 프레임에서 한 번 더 정리 (씬 로드 직후 OnEnable보다 먼저 잡기 어려우므로)
/// </summary>
[DefaultExecutionOrder(-900)]
public class SystemsSceneEnforcer : MonoBehaviour
{
    [Tooltip("Systems 씬이 소유한 글로벌 라이트(이건 유지). 빌더가 연결함.")]
    [SerializeField] Light2D ownedGlobal;

    void Awake()
    {
        if (ownedGlobal == null)
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) { ownedGlobal = l; break; }

        DisableForeignGlobals();
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // sceneLoaded 콜백 시점에는 새 씬의 오브젝트가 이미 OnEnable까지 완료.
        // 여기서 끄면 이후 프레임부터 중복이 사라진다 (첫 프레임 경고 1회는 불가피).
        DisableForeignGlobals();
    }

    void DisableForeignGlobals()
    {
        var all = FindObjectsByType<Light2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var l in all)
        {
            if (l == null || l.lightType != Light2D.LightType.Global) continue;
            if (ownedGlobal != null)
            {
                if (l != ownedGlobal) l.enabled = false;     // Systems 글로벌만 남기고 끔
            }
            else if (SystemsScene.IsGameplayScene(l.gameObject.scene))
            {
                l.enabled = false;                           // 폴백: 게임플레이 씬 글로벌만 끔
            }
        }
    }
}
