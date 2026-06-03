using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Systems 씬의 글로벌 조명이 "유일 글로벌"이 되도록, 게임플레이 씬이 들고 온 다른
/// Global Light2D를 로드 시 비활성화한다. (카메라/AudioListener 중복은 CameraFollow가 처리)
///
/// 게임플레이 씬에 예전 SceneLightingBuilder로 만든 Global Light2D가 남아 있어도
/// 밝기가 2배로 겹치지 않게 막아 준다. Systems의 글로벌은 ownedGlobal로 지정해 보존.
/// </summary>
public class SystemsSceneEnforcer : MonoBehaviour
{
    [Tooltip("Systems 씬이 소유한 글로벌 라이트(이건 유지). 빌더가 연결함.")]
    [SerializeField] Light2D ownedGlobal;

    void Awake()
    {
        if (ownedGlobal == null)
            foreach (var l in FindObjectsByType<Light2D>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global) { ownedGlobal = l; break; }
    }

    void OnEnable()  => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void Start()     => DisableForeignGlobals();

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => DisableForeignGlobals();

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
