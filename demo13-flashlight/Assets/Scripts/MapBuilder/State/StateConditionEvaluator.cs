using UnityEngine;

namespace TopDownMapEditor
{
    public class StateConditionEvaluator
    {
        public bool Evaluate(TransitionConditionType type, string param)
        {
            switch (type)
            {
                case TransitionConditionType.Manual:
                    return false;

                case TransitionConditionType.TimeOfDay:
                    return EvaluateTimeOfDay(param);

                case TransitionConditionType.Season:
                case TransitionConditionType.DamageThreshold:
                case TransitionConditionType.QuestComplete:
                case TransitionConditionType.Custom:
                    return false;

                default:
                    return false;
            }
        }

        bool EvaluateTimeOfDay(string param)
        {
            if (string.IsNullOrEmpty(param)) return false;

            var parts = param.Split('-');
            if (parts.Length != 2) return false;

            if (!float.TryParse(parts[0], out float start) || !float.TryParse(parts[1], out float end))
                return false;

            float currentHour = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;

            if (start < end)
                return currentHour >= start && currentHour < end;
            else
                return currentHour >= start || currentHour < end;
        }
    }
}
