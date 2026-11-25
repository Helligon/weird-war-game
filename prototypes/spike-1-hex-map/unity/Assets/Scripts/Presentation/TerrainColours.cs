using UnityEngine;
using WeirdWar.Spike1.Core;

namespace WeirdWar.Spike1.Presentation
{
    /// <summary>Flat colour per terrain type. No art (VIS-006).</summary>
    public static class TerrainColours
    {
        public static Color For(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Plain: return new Color(0.62f, 0.74f, 0.42f);
                case TerrainType.Mountain: return new Color(0.45f, 0.40f, 0.36f);
                case TerrainType.Sea: return new Color(0.20f, 0.38f, 0.65f);
                case TerrainType.River: return new Color(0.45f, 0.70f, 0.90f);
                default: return Color.magenta;
            }
        }
    }
}
