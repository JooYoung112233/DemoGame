using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class EscapePointManager : MonoBehaviour
    {
        readonly List<EscapePoint> _escapePoints = new();
        SpawnConditionEvaluator _conditionEvaluator;

        public void Initialize(List<EscapePoint> escapePoints)
        {
            _escapePoints.Clear();
            _escapePoints.AddRange(escapePoints);
            _conditionEvaluator = new SpawnConditionEvaluator();
        }

        public EscapePoint GetNearestEscape(Vector2Int playerGrid)
        {
            EscapePoint nearest = null;
            float minDist = float.MaxValue;

            foreach (var ep in _escapePoints)
            {
                if (!IsAvailable(ep)) continue;

                float dist = Vector2Int.Distance(playerGrid, ep.gridPosition);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = ep;
                }
            }
            return nearest;
        }

        public List<EscapePoint> GetAvailableEscapes()
        {
            var result = new List<EscapePoint>();
            foreach (var ep in _escapePoints)
            {
                if (IsAvailable(ep))
                    result.Add(ep);
            }
            return result;
        }

        public bool IsAvailable(EscapePoint ep)
        {
            if (ep.isHidden) return false;
            return _conditionEvaluator.EvaluateAll(ep.availableConditions);
        }

        public void RevealHidden(string escapeId)
        {
            var ep = _escapePoints.Find(e => e.escapeId == escapeId);
            if (ep != null) ep.isHidden = false;
        }
    }
}
