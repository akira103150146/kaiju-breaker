using UnityEngine;

namespace KaijuBreaker.App.Gameplay
{
    /// <summary>
    /// Shared IMGUI look for the placeholder UI: the free Ark Pixel font + a small set of runtime-generated
    /// cold-palette textures (bordered panel/button, HP-bar frame/fill, soft ring) and matching GUIStyles,
    /// aligned to the art bible (cold family; warm reserved for threat/defeat). One lazily-built theme so the
    /// title screen, HUD, results, and on-screen controls read as one system. This is a stop-gap for the
    /// eventual UGUI + TMP pass (ADR-0006); it needs no asset authoring beyond the font reference.
    /// </summary>
    public static class GameUiSkin
    {
        // Art-bible cold palette (+ warm accents).
        public static readonly Color Bg = new Color(0.04f, 0.055f, 0.10f, 1f);
        public static readonly Color Panel = new Color(0.06f, 0.09f, 0.16f, 0.94f);
        public static readonly Color Cyan = new Color(0.25f, 0.97f, 1f, 1f);
        public static readonly Color CyanDim = new Color(0f, 0.75f, 0.88f, 1f);
        public static readonly Color Ink = new Color(0.9f, 0.96f, 1f, 1f);
        public static readonly Color Warm = new Color(1f, 0.55f, 0.28f, 1f);
        public static readonly Color Danger = new Color(1f, 0.34f, 0.30f, 1f);

        private static bool _built;
        private static Font _font;

        public static Texture2D White { get; private set; }
        public static Texture2D Ring { get; private set; }         // soft radial disc
        public static Texture2D Panel9 { get; private set; }       // bordered panel (9-slice)
        public static Texture2D Button9 { get; private set; }      // bordered button
        public static Texture2D ButtonHot9 { get; private set; }   // bordered button (hover/active)

        public static GUIStyle TitleStyle { get; private set; }
        public static GUIStyle HeadingStyle { get; private set; }
        public static GUIStyle LabelStyle { get; private set; }
        public static GUIStyle SmallStyle { get; private set; }
        public static GUIStyle PanelStyle { get; private set; }
        public static GUIStyle ButtonStyle { get; private set; }
        public static GUIStyle SelectedButtonStyle { get; private set; }

        /// <summary>Build the theme once. Safe to call every frame; rebuilds only if the font changed.</summary>
        public static void EnsureBuilt(Font font)
        {
            if (_built && _font == font) return;
            _font = font;

            White = Solid(Color.white);
            Ring = MakeRing(48);
            Panel9 = MakeBordered(16, Panel, Cyan, 2);
            Button9 = MakeBordered(16, new Color(0.09f, 0.15f, 0.26f, 0.96f), CyanDim, 2);
            ButtonHot9 = MakeBordered(16, new Color(0.13f, 0.24f, 0.40f, 1f), Cyan, 2);

            var border = new RectOffset(6, 6, 6, 6);
            PanelStyle = new GUIStyle { border = border };
            PanelStyle.normal.background = Panel9;

            TitleStyle = Text(font, 40, Cyan, FontStyle.Bold, TextAnchor.MiddleCenter);
            HeadingStyle = Text(font, 26, Ink, FontStyle.Bold, TextAnchor.MiddleCenter);
            LabelStyle = Text(font, 18, Ink, FontStyle.Normal, TextAnchor.MiddleCenter);
            SmallStyle = Text(font, 14, CyanDim, FontStyle.Normal, TextAnchor.MiddleCenter);

            ButtonStyle = new GUIStyle
            {
                border = border,
                padding = new RectOffset(12, 12, 8, 8),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                font = font
            };
            ButtonStyle.normal.background = Button9; ButtonStyle.normal.textColor = Cyan;
            ButtonStyle.hover.background = ButtonHot9; ButtonStyle.hover.textColor = Color.white;
            ButtonStyle.active.background = ButtonHot9; ButtonStyle.active.textColor = Color.white;

            // Persistently "hot" variant for the currently-selected option in a group.
            SelectedButtonStyle = new GUIStyle(ButtonStyle);
            SelectedButtonStyle.normal.background = ButtonHot9;
            SelectedButtonStyle.normal.textColor = Color.white;

            _built = true;
        }

        private static GUIStyle Text(Font font, int size, Color c, FontStyle style, TextAnchor anchor)
        {
            var s = new GUIStyle { fontSize = size, fontStyle = style, alignment = anchor, font = font, wordWrap = false };
            s.normal.textColor = c;
            return s;
        }

        /// <summary>Draw a filled bar (frame + fill) in screen space at <paramref name="rect"/>, fill fraction 0..1.</summary>
        public static void Bar(Rect rect, float fill01, Color fillColor)
        {
            fill01 = Mathf.Clamp01(fill01);
            var prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f); GUI.DrawTexture(rect, White);           // backing
            GUI.color = fillColor;
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * fill01, rect.height - 2), White);
            GUI.color = new Color(Cyan.r, Cyan.g, Cyan.b, 0.7f);                                // 1px frame
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), White);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), White);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), White);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), White);
            GUI.color = prev;
        }

        private static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); t.filterMode = FilterMode.Point;
            return t;
        }

        private static Texture2D MakeRing(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) + (y - r + 0.5f) * (y - r + 0.5f)) / r;
                    float a = d > 1f ? 0f : Mathf.SmoothStep(0f, 1f, 1f - d) * 0.7f + (d > 0.82f && d <= 1f ? 0.3f : 0f);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(a)));
                }
            t.Apply(); t.filterMode = FilterMode.Bilinear;
            return t;
        }

        private static Texture2D MakeBordered(int size, Color fill, Color border, int bw)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool edge = x < bw || y < bw || x >= size - bw || y >= size - bw;
                    t.SetPixel(x, y, edge ? border : fill);
                }
            t.Apply(); t.filterMode = FilterMode.Point;
            return t;
        }
    }
}
