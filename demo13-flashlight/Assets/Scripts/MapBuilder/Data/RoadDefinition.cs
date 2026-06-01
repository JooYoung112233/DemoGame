using UnityEngine;

namespace TopDownMapEditor
{
    [CreateAssetMenu(menuName = "Top-Down Map/Road Definition")]
    public class RoadDefinition : ScriptableObject
    {
        public string roadId;
        public string displayName;
        public Sprite[] autoTileSprites = new Sprite[16];
        public int sortingOffset;

        public Sprite GetSprite(int bitmask)
        {
            if (bitmask < 0 || bitmask >= autoTileSprites.Length)
                return autoTileSprites[0];
            return autoTileSprites[bitmask] ?? autoTileSprites[0];
        }
    }
}
