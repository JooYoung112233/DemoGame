using System.Collections.Generic;

namespace IsometricMapEditor
{
    public class SpawnConditionEvaluator
    {
        public bool EvaluateAll(List<SpawnCondition> conditions)
        {
            if (conditions == null || conditions.Count == 0) return true;

            foreach (var cond in conditions)
            {
                if (!Evaluate(cond))
                    return false;
            }
            return true;
        }

        public bool Evaluate(SpawnCondition condition)
        {
            switch (condition.type)
            {
                case SpawnConditionType.Always:
                    return true;

                case SpawnConditionType.Probability:
                    if (float.TryParse(condition.param, out float prob))
                        return UnityEngine.Random.value <= prob;
                    return true;

                case SpawnConditionType.TimeOfDay:
                    return EvaluateTimeRange(condition.param);

                case SpawnConditionType.Season:
                case SpawnConditionType.Weather:
                case SpawnConditionType.QuestComplete:
                case SpawnConditionType.PlayerLevel:
                case SpawnConditionType.Custom:
                    return true;

                default:
                    return true;
            }
        }

        bool EvaluateTimeRange(string param)
        {
            if (string.IsNullOrEmpty(param)) return true;
            var parts = param.Split('-');
            if (parts.Length != 2) return true;

            if (!TryParseTime(parts[0], out float start) || !TryParseTime(parts[1], out float end))
                return true;

            float now = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;
            if (start < end)
                return now >= start && now < end;
            return now >= start || now < end;
        }

        bool TryParseTime(string timeStr, out float hours)
        {
            hours = 0;
            var timeParts = timeStr.Trim().Split(':');
            if (timeParts.Length == 2 &&
                int.TryParse(timeParts[0], out int h) &&
                int.TryParse(timeParts[1], out int m))
            {
                hours = h + m / 60f;
                return true;
            }
            return float.TryParse(timeStr.Trim(), out hours);
        }
    }
}
