using UnityEngine;

/// <summary>
/// 카메라, 스폰 등 게임 전반 설정. ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "Demo/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Camera")]
    public float orthoSize = 7f;
    public float cameraAngle = 55f;
    public float cameraDistance = 20f;
    public float cameraYaw = 0f;
    public float cameraSmoothSpeed = 8f;

    [Header("Spawn Zones")]
    public SpawnZoneData[] spawnZones = new SpawnZoneData[]
    {
        new SpawnZoneData { position = new Vector3(3, 0, 7), size = new Vector3(4, 0, 4), enemyCount = 2 },
        new SpawnZoneData { position = new Vector3(10, 0, 4), size = new Vector3(3, 0, 3), enemyCount = 1 },
    };

    [System.Serializable]
    public class SpawnZoneData
    {
        public Vector3 position;
        public Vector3 size = new Vector3(4, 0, 4);
        public int enemyCount = 2;
    }
}
