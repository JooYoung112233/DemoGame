using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 다국어 텍스트 로더.
/// Resources/Story/Locale/{lang}.json 에서 key-value 문자열 로드.
///
/// 사용:
///   StoryLocale.Instance.Get("S000_N01")  → "……머리가 아프다."
///   StoryLocale.Instance.SetLanguage("en") → 영어로 전환
/// </summary>
public class StoryLocale : MonoBehaviour
{
    public static StoryLocale Instance { get; private set; }

    [SerializeField] string defaultLanguage = "ko";

    string currentLanguage;
    Dictionary<string, string> strings = new Dictionary<string, string>();

    public string CurrentLanguage => currentLanguage;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetLanguage(defaultLanguage);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 언어 변경. 해당 로케일 파일을 로드.
    /// </summary>
    public void SetLanguage(string lang)
    {
        currentLanguage = lang;
        strings.Clear();

        var asset = Resources.Load<TextAsset>($"Story/Locale/{lang}");
        if (asset == null)
        {
            Debug.LogWarning($"[Locale] 로케일 파일 없음: Story/Locale/{lang}.json");
            return;
        }

        var data = JsonUtility.FromJson<LocaleData>(asset.text);
        if (data?.entries == null)
        {
            Debug.LogWarning($"[Locale] 파싱 실패: {lang}.json");
            return;
        }

        foreach (var entry in data.entries)
        {
            if (!string.IsNullOrEmpty(entry.key))
                strings[entry.key] = entry.value;
        }

        Debug.Log($"[Locale] 로드 완료: {lang} ({strings.Count}개 항목)");
    }

    /// <summary>
    /// 로케일 키로 텍스트 조회. 없으면 키 자체를 반환 (디버그 편의).
    /// </summary>
    public string Get(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";
        if (strings.TryGetValue(key, out var value)) return value;
        return $"[{key}]"; // 미번역 표시
    }

    /// <summary>
    /// 여러 키를 한번에 변환.
    /// </summary>
    public string[] GetAll(string[] keys)
    {
        if (keys == null) return new string[0];
        var result = new string[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            result[i] = Get(keys[i]);
        return result;
    }

    /// <summary>
    /// 키가 존재하는지 확인.
    /// </summary>
    public bool HasKey(string key)
    {
        return !string.IsNullOrEmpty(key) && strings.ContainsKey(key);
    }
}
