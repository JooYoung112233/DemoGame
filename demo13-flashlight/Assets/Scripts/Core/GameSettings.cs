using UnityEngine;

/// <summary>
/// 게임 설정 값의 단일 저장소. PlayerPrefs 영속(세이브 슬롯과 분리, 프로젝트 첫 PlayerPrefs 사용처 — 접두사 "settings.").
/// - 볼륨: 마스터 = AudioListener.volume 즉시 반영. BGM/SFX는 배율만 보관 — 오디오 재생 인프라가 아직 없어
///   향후 재생 코드가 GameSettings.BgmVolume/SfxVolume을 읽는 계약(docs/dev-roadmap.md 설정 메뉴 1차).
/// - 해상도/창모드: 빌드에서만 실제 적용(에디터 게임뷰는 저장만). 저장값이 없으면 OS 기본을 건드리지 않는다.
/// </summary>
public static class GameSettings
{
    const string KeyMaster = "settings.volume.master";
    const string KeyBgm    = "settings.volume.bgm";
    const string KeySfx    = "settings.volume.sfx";
    const string KeyResW   = "settings.screen.width";
    const string KeyResH   = "settings.screen.height";
    const string KeyMode   = "settings.screen.mode";

    static bool loaded;
    static float master = 1f, bgm = 1f, sfx = 1f;

    static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        master = PlayerPrefs.GetFloat(KeyMaster, 1f);
        bgm    = PlayerPrefs.GetFloat(KeyBgm, 1f);
        sfx    = PlayerPrefs.GetFloat(KeySfx, 1f);
    }

    public static float MasterVolume
    {
        get { EnsureLoaded(); return master; }
        set
        {
            EnsureLoaded();
            master = Mathf.Clamp01(value);
            AudioListener.volume = master;
            PlayerPrefs.SetFloat(KeyMaster, master);
        }
    }

    /// <summary>BGM 배율(0~1). 재생 코드가 곱해 쓴다 — 최종 음량 = AudioListener(마스터) × BgmVolume.</summary>
    public static float BgmVolume
    {
        get { EnsureLoaded(); return bgm; }
        set { EnsureLoaded(); bgm = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeyBgm, bgm); }
    }

    /// <summary>SFX 배율(0~1). 재생 코드가 곱해 쓴다.</summary>
    public static float SfxVolume
    {
        get { EnsureLoaded(); return sfx; }
        set { EnsureLoaded(); sfx = Mathf.Clamp01(value); PlayerPrefs.SetFloat(KeySfx, sfx); }
    }

    /// <summary>저장된 해상도(없으면 0×0 = 미설정).</summary>
    public static Vector2Int SavedResolution =>
        new Vector2Int(PlayerPrefs.GetInt(KeyResW, 0), PlayerPrefs.GetInt(KeyResH, 0));

    public static FullScreenMode SavedScreenMode =>
        (FullScreenMode)PlayerPrefs.GetInt(KeyMode, (int)FullScreenMode.FullScreenWindow);

    /// <summary>해상도+창모드 저장 후 적용(빌드 전용 — 에디터는 저장만).</summary>
    public static void ApplyResolution(int width, int height, FullScreenMode mode)
    {
        PlayerPrefs.SetInt(KeyResW, width);
        PlayerPrefs.SetInt(KeyResH, height);
        PlayerPrefs.SetInt(KeyMode, (int)mode);
        PlayerPrefs.Save();   // 해상도 변경은 드물고 중요 — 즉시 flush(강제 종료 대비)
        if (!Application.isEditor && width > 0 && height > 0)
            Screen.SetResolution(width, height, mode);
    }

    /// <summary>보류 중인 설정 변경을 디스크에 flush — 설정 패널 닫힐 때 호출(강제 종료 시 유실 방지).</summary>
    public static void Flush() => PlayerPrefs.Save();

    /// <summary>부팅 시 저장된 설정 적용 — 볼륨은 항상, 해상도는 저장값이 있을 때만.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ApplyOnBoot()
    {
        EnsureLoaded();
        AudioListener.volume = master;
        var res = SavedResolution;
        if (!Application.isEditor && res.x > 0 && res.y > 0)
            Screen.SetResolution(res.x, res.y, SavedScreenMode);
    }
}
