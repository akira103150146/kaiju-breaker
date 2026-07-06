using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Resolves a <see cref="SpawnLayout"/> formation into concrete world positions (stage-system.md §D.2).
    /// Pure geometry — no Unity object creation, no state — so it is fully EditMode-testable. Positions are
    /// symmetric about x = 0 and bounded within ±<c>fieldWidth/2</c> so waves stay on-screen on the smallest
    /// phone (technical-preferences readability).
    /// </summary>
    public static class SpawnLayoutHelper
    {
        /// <summary>
        /// Compute <paramref name="count"/> spawn positions for the given formation. Returns an empty array
        /// for count ≤ 0.
        /// </summary>
        public static Vector2[] Positions(SpawnLayout layout, int count, float fieldWidth, float spawnY, float columnSpacing)
        {
            if (count <= 0) return System.Array.Empty<Vector2>();
            var result = new Vector2[count];

            switch (layout)
            {
                case SpawnLayout.Center:
                    for (int i = 0; i < count; i++) result[i] = new Vector2(0f, spawnY);
                    break;

                case SpawnLayout.Column:
                    for (int i = 0; i < count; i++) result[i] = new Vector2(0f, spawnY - i * columnSpacing);
                    break;

                case SpawnLayout.HorizontalSpread:
                default:
                    if (count == 1)
                    {
                        result[0] = new Vector2(0f, spawnY);
                    }
                    else
                    {
                        float half = fieldWidth * 0.5f;
                        float step = fieldWidth / (count - 1);
                        for (int i = 0; i < count; i++) result[i] = new Vector2(-half + i * step, spawnY);
                    }
                    break;
            }
            return result;
        }
    }
}
