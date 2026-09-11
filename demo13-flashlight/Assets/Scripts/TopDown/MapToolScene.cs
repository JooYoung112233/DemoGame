using UnityEngine.SceneManagement;

/// <summary>
/// 맵툴(편집/미리보기) 씬인지 판별한다. 이 씬에서 Play하면 게임 시스템
/// (UIManager의 게임 HUD·체력/재화, 플레이어 자동 스폰 등)을 띄우지 않는다.
/// 씬 이름에 "MapTool"이 들어가면 맵툴 씬으로 본다. (예: MapTool2D)
/// </summary>
public static class MapToolScene
{
    public static bool IsActive
    {
        get
        {
            var n = SceneManager.GetActiveScene().name;
            return !string.IsNullOrEmpty(n)
                && n.IndexOf("MapTool", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
