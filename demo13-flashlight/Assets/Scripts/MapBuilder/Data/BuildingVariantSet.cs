using UnityEngine;
using System.Collections.Generic;

namespace TopDownMapEditor
{
    [CreateAssetMenu(menuName = "Top-Down Map/Building Variant Set")]
    public class BuildingVariantSet : ScriptableObject
    {
        public string defaultVariantId;
        public List<BuildingVariant> variants = new();
        public List<VariantTransitionRule> transitionRules = new();

        public BuildingVariant GetVariant(string variantId)
        {
            return variants.Find(v => v.variantId == variantId);
        }

        public BuildingVariant GetDefault()
        {
            return GetVariant(defaultVariantId) ?? (variants.Count > 0 ? variants[0] : null);
        }
    }
}
