using UnityEngine;

namespace TopDownMapEditor
{
    public class BuildingStateTransition
    {
        public string instanceId;
        public string fromVariantId;
        public string toVariantId;
        public TransitionEffect effect;
        public float duration;
        public float elapsed;
        public bool isComplete;

        public BuildingStateTransition(string instanceId, string from, string to, TransitionEffect effect, float duration)
        {
            this.instanceId = instanceId;
            fromVariantId = from;
            toVariantId = to;
            this.effect = effect;
            this.duration = duration;
            elapsed = 0f;
            isComplete = effect == TransitionEffect.Instant;
        }

        public float Progress => duration > 0 ? Mathf.Clamp01(elapsed / duration) : 1f;

        public void Update(float deltaTime)
        {
            if (isComplete) return;
            elapsed += deltaTime;
            if (elapsed >= duration) isComplete = true;
        }
    }
}
