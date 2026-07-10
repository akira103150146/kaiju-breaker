using System.Collections.Generic;
using KaijuBreaker.Content;
using UnityEngine;

namespace KaijuBreaker.Stage
{
    /// <summary>
    /// Rasterises each <see cref="EnemyShape"/> to a small white runtime sprite, cached and shared across every
    /// spawn (no per-enemy texture allocation). A distinct shape + the def's <see cref="EnemyDef.BodyColor"/>
    /// (applied via the SpriteRenderer tint) lets the player tell mob types apart at a glance without bespoke
    /// art — the placeholder identity layer (enemy-roster-expansion.md §4.2). Shapes are drawn white so a single
    /// texture per shape serves every colour. Replaced by authored sprites when art lands.
    /// </summary>
    public static class EnemyShapeSprites
    {
        private const int Res = 64;                 // texture resolution (square)
        private const float PixelsPerUnit = Res;    // 1 world unit per texture => Sprite is 1×1 world at scale 1

        private static readonly Dictionary<EnemyShape, Sprite> _cache = new Dictionary<EnemyShape, Sprite>();

        /// <summary>The cached 1×1-world white sprite for <paramref name="shape"/> (built on first use).</summary>
        public static Sprite For(EnemyShape shape)
        {
            if (_cache.TryGetValue(shape, out var s) && s != null) return s;
            s = Build(shape);
            _cache[shape] = s;
            return s;
        }

        private static Sprite Build(EnemyShape shape)
        {
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var px = new Color32[Res * Res];
            for (int y = 0; y < Res; y++)
                for (int x = 0; x < Res; x++)
                {
                    // Normalised coords in [-1, 1] centred on the texture.
                    float u = (x + 0.5f) / Res * 2f - 1f;
                    float v = (y + 0.5f) / Res * 2f - 1f;
                    bool inside = Inside(shape, u, v);
                    px[y * Res + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            tex.SetPixels32(px);
            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), PixelsPerUnit);
            sprite.name = "EnemyShape_" + shape;
            return sprite;
        }

        // Point-in-shape test in the [-1, 1] square. r = 0.92 keeps a hair of transparent margin so edges anti-alias.
        private static bool Inside(EnemyShape shape, float u, float v)
        {
            const float r = 0.92f;
            switch (shape)
            {
                case EnemyShape.Circle:
                    return u * u + v * v <= r * r;
                case EnemyShape.Diamond:
                    return Mathf.Abs(u) + Mathf.Abs(v) <= r;
                case EnemyShape.Triangle:
                {
                    // Upward-pointing filled triangle inscribed in the square.
                    float t = (v + r) / (2f * r);                 // 0 at bottom, 1 at apex
                    return v >= -r && v <= r && Mathf.Abs(u) <= r * (1f - t);
                }
                case EnemyShape.Chevron:
                {
                    // Downward arrowhead (reads as a diving attacker): two stacked triangles minus a notch.
                    float t = (r - v) / (2f * r);                 // 0 at top, 1 at bottom point
                    bool outer = v <= r && v >= -r && Mathf.Abs(u) <= r * (1f - Mathf.Abs(t - 0.0f));
                    bool notch = v > 0.15f && Mathf.Abs(u) <= r * (1f - (r - v) / (2f * r)) - 0.5f;
                    return outer && !notch;
                }
                case EnemyShape.Hexagon:
                {
                    // Flat-top hexagon via the standard half-plane test.
                    float ax = Mathf.Abs(u), ay = Mathf.Abs(v);
                    return ax <= r * 0.87f && ay <= r && (ay + ax * 0.577f) <= r;
                }
                case EnemyShape.Square:
                default:
                    return Mathf.Abs(u) <= r && Mathf.Abs(v) <= r;
            }
        }
    }
}
