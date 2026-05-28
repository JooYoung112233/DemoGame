using System.Collections.Generic;
using System.Linq;

namespace IsometricMapEditor
{
    public class InteriorConnectionResolver
    {
        readonly List<InteriorConnection> _connections = new();

        public void LoadConnections(List<InteriorConnection> connections)
        {
            _connections.Clear();
            _connections.AddRange(connections);
        }

        public InteriorConnection FindConnection(string fromMapId, string fromConnectionPointId)
        {
            return _connections.FirstOrDefault(c =>
                c.fromMapId == fromMapId && c.fromConnectionPointId == fromConnectionPointId);
        }

        public List<InteriorConnection> GetConnectionsFrom(string mapId)
        {
            return _connections.Where(c => c.fromMapId == mapId || c.toMapId == mapId).ToList();
        }

        public (string targetMapId, string targetConnectionId) ResolveTarget(string fromMapId, string fromPointId)
        {
            var conn = FindConnection(fromMapId, fromPointId);
            if (conn == null) return (null, null);

            if (conn.fromMapId == fromMapId)
                return (conn.toMapId, conn.toConnectionPointId);
            else
                return (conn.fromMapId, conn.fromConnectionPointId);
        }
    }
}
