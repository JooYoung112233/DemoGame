using UnityEngine;
using System.Collections.Generic;

namespace IsometricMapEditor
{
    /// <summary>
    /// Stub: roof hide functionality removed. Kept for compatibility with existing scenes.
    /// </summary>
    public class RoofHideController : MonoBehaviour
    {
        public float hideDistance = 2f;
        public float fadeDuration = 0.3f;

        public void Initialize(MapData map, BuildingRenderer renderer, Transform player) { }
    }
}
