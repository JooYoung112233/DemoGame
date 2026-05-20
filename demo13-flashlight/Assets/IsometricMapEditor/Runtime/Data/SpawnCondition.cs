namespace IsometricMapEditor
{
    [System.Serializable]
    public class SpawnCondition
    {
        public SpawnConditionType type;
        public string param;
    }

    public enum SpawnConditionType
    {
        Always,
        Probability,
        TimeOfDay,
        Season,
        Weather,
        QuestComplete,
        PlayerLevel,
        Custom
    }
}
