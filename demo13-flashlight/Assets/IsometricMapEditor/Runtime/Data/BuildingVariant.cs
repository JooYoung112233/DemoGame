using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    [System.Serializable]
    public class BuildingVariant
    {
        public string variantId;
        public string displayName;
        public Sprite baseSprite;
        public Sprite roofSprite;
        public Material materialOverride;
        public Color tintColor = Color.white;
        public TransitionEffect transitionEffect = TransitionEffect.Instant;
        public float transitionDuration = 0.5f;
        public List<VariantAddon> addons = new();
    }

    [System.Serializable]
    public class VariantAddon
    {
        public string addonId;
        public GameObject prefab;
        public Vector3 localOffset;
        public bool activeInThisVariant = true;
    }

    public enum TransitionEffect
    {
        Instant,
        CrossFade,
        Dissolve
    }

    [System.Serializable]
    public class VariantTransitionRule
    {
        public string fromVariantId;
        public string toVariantId;
        public TransitionConditionType conditionType;
        public string conditionParam;
        public bool autoTransition = true;
    }

    public enum TransitionConditionType
    {
        Manual,
        TimeOfDay,
        Season,
        DamageThreshold,
        QuestComplete,
        Custom
    }
}
