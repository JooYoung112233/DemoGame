namespace TopDownMapEditor
{
    [System.Serializable]
    public class InteriorConnection
    {
        public string connectionId;
        public string label;
        public InteriorConnectionType type;
        public string fromMapId;
        public string fromConnectionPointId;
        public string toMapId;
        public string toConnectionPointId;
    }

    public enum InteriorConnectionType
    {
        Door,
        Tunnel,
        Skywalk,
        Underground,
        Elevator
    }
}
