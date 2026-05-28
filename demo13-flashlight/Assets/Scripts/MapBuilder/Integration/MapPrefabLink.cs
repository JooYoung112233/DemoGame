using UnityEngine;

namespace IsometricMapEditor
{
    /// <summary>
    /// 프리팹과 원본 MapData를 연결하는 컴포넌트.
    /// Save Prefab 시 자동 추가, Load Prefab 시 이 참조로 MapData를 복원.
    /// </summary>
    public class MapPrefabLink : MonoBehaviour
    {
        public MapData sourceMapData;
    }
}
