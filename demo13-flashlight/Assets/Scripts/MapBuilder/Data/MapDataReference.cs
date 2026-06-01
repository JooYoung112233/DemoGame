using UnityEngine;

namespace TopDownMapEditor
{
    /// <summary>
    /// 맵 프리팹에 부착되는 참조 컴포넌트.
    /// 런타임에서 JSON 로드나 맵 정보 식별에 사용.
    /// </summary>
    public class MapDataReference : MonoBehaviour
    {
        public string mapName;
        public string mapId;
        public string jsonFileName;
        public GridSettings gridSettings = new();

        /// <summary>JSON 파일의 프로젝트 내 경로 (Assets/Maps/xxx.json)</summary>
        public string JsonAssetPath => $"Assets/Maps/{jsonFileName}.json";
    }
}
