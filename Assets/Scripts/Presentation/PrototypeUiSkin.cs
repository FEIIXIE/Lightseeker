using System;
using System.Collections.Generic;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    internal enum UiButtonSemantic
    {
        Primary,
        Secondary,
        Danger,
        Reward
    }

    /// <summary>
    /// Shared IMGUI presentation layer. The default instance stays on v1.4; a separate
    /// light-theme instance can opt one page into the approved v1.9 trial without changing
    /// any other screen. Resource misses retain a quiet procedural fallback.
    /// </summary>
    internal sealed class PrototypeUiSkin
    {
        private const string ResourceRoot = "Art/GlobalUI/";
        private readonly string resourceSuffix;
        private readonly bool lightTheme;
        private readonly Color textPrimary;
        private readonly Color textMuted;

        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<string, GUIStyle> buttonStyles = new Dictionary<string, GUIStyle>(StringComparer.Ordinal);
        private readonly Dictionary<string, GUIStyle> tabStyles = new Dictionary<string, GUIStyle>(StringComparer.Ordinal);
        private readonly Dictionary<string, GUIStyle> surfaceStyles = new Dictionary<string, GUIStyle>(StringComparer.Ordinal);
        private readonly Dictionary<Color, Texture2D> fallbackTextures = new Dictionary<Color, Texture2D>();
        private GUISkin appliedSkin;
        private Font unifiedFont;

        public PrototypeUiSkin() : this("02", false)
        {
        }

        public PrototypeUiSkin(string resourceSuffix, bool lightTheme)
        {
            this.resourceSuffix = string.IsNullOrEmpty(resourceSuffix) ? "02" : resourceSuffix;
            this.lightTheme = lightTheme;
            textPrimary = lightTheme ? Hex("263B40") : Hex("E9EEE8");
            textMuted = lightTheme ? Hex("40595F") : Hex("74878A");
        }

        public void ApplyGlobalControls(GUISkin skin)
        {
            if (skin == null || appliedSkin == skin) return;
            appliedSkin = skin;
            var track = Load(Resource("ART-UI-SCROLL-TRACK"), Hex(lightTheme ? "D7E0DB" : "122229"));
            var thumb = Load(Resource("ART-UI-SCROLL-THUMB"), Hex(lightTheme ? "7FA3A9" : "82959A"));
            skin.verticalScrollbar.fixedWidth = 24f;
            skin.verticalScrollbar.border = new RectOffset(12, 12, 40, 40);
            skin.verticalScrollbar.normal.background = track;
            skin.verticalScrollbar.hover.background = track;
            skin.verticalScrollbar.active.background = track;
            skin.verticalScrollbarThumb.fixedWidth = 24f;
            skin.verticalScrollbarThumb.border = new RectOffset(12, 12, 40, 40);
            skin.verticalScrollbarThumb.normal.background = thumb;
            skin.verticalScrollbarThumb.hover.background = thumb;
            skin.verticalScrollbarThumb.active.background = thumb;
        }

        public void ApplyFont(Font font)
        {
            if (font == null) return;
            unifiedFont = font;
            foreach (var style in buttonStyles.Values) style.font = font;
            foreach (var style in tabStyles.Values) style.font = font;
            foreach (var style in surfaceStyles.Values) style.font = font;
        }

        public GUIStyle Button(UiButtonSemantic semantic, int fontSize, bool enabled)
        {
            var key = semantic + ":" + fontSize + ":" + enabled;
            GUIStyle style;
            if (buttonStyles.TryGetValue(key, out style)) return style;

            var semanticId = semantic.ToString().ToUpperInvariant();
            var normal = enabled
                ? Load(Resource("ART-UI-BUTTON-" + semanticId + "-NORMAL"), SemanticFallback(semantic))
                : Load(Resource("ART-UI-BUTTON-" + semanticId + "-DISABLED"), Hex(lightTheme ? "C8D0CB" : "69767A"));
            var pressed = enabled
                ? Load(Resource("ART-UI-BUTTON-" + semanticId + "-PRESSED"), SemanticFallback(semantic) * 0.82f)
                : normal;

            // Some card actions are only 28-46 design pixels high. Any fixed nine-slice
            // border large enough to preserve the 512x160 source corners becomes wider
            // than those controls and folds the rounded ends into polygon-like shapes.
            // Compact controls therefore scale the complete v1.4 texture; larger buttons
            // keep nine-slicing so their centre can grow without thickening the outline.
            var compact = !lightTheme && fontSize <= 15;

            style = new GUIStyle(GUI.skin.button)
            {
                font = unifiedFont,
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                border = compact ? new RectOffset(0, 0, 0, 0) :
                    lightTheme ? new RectOffset(38, 38, 30, 30) : new RectOffset(40, 40, 24, 24),
                padding = compact ? new RectOffset(10, 10, 3, 3) : new RectOffset(18, 18, 7, 7)
            };
            SetBackground(style.normal, normal, enabled ? textPrimary : textMuted);
            SetBackground(style.hover, normal, enabled ? textPrimary : textMuted);
            SetBackground(style.focused, normal, enabled ? textPrimary : textMuted);
            SetBackground(style.active, pressed, enabled ? textPrimary : textMuted);
            buttonStyles[key] = style;
            return style;
        }

        public GUIStyle Tab(bool active, int fontSize)
        {
            var key = active + ":" + fontSize;
            GUIStyle style;
            if (tabStyles.TryGetValue(key, out style)) return style;
            var background = Load(Resource(active ? "ART-UI-TAB-ACTIVE" : "ART-UI-TAB-INACTIVE"),
                active ? Hex(lightTheme ? "DCEAE7" : "17343A") : Hex(lightTheme ? "F2F0E8" : "172A31"));
            style = new GUIStyle(GUI.skin.button)
            {
                font = unifiedFont,
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                border = lightTheme ? new RectOffset(34, 34, 28, 28) : new RectOffset(36, 36, 30, 30),
                padding = new RectOffset(14, 14, 6, 6)
            };
            SetBackground(style.normal, background, textPrimary);
            SetBackground(style.hover, background, textPrimary);
            SetBackground(style.focused, background, textPrimary);
            SetBackground(style.active, background, textPrimary);
            tabStyles[key] = style;
            return style;
        }

        public void DrawPanel(Rect area, bool ornamented)
        {
            DrawSurface(area, ornamented ? "panel.ornamented" : "panel.plain",
                Resource(ornamented ? "ART-UI-PANEL-ORNAMENTED" : "ART-UI-PANEL-PLAIN"),
                lightTheme ? new RectOffset(34, 34, 34, 34) : new RectOffset(36, 36, 36, 36),
                Hex(lightTheme ? (ornamented ? "E7E9E0" : "F1F0E7") : (ornamented ? "122229" : "172A31")));
        }

        public void DrawListRow(Rect area, bool selected)
        {
            DrawSurface(area, selected ? "row.selected" : "row.normal",
                Resource(selected ? "ART-UI-LIST-ROW-SELECTED" : "ART-UI-LIST-ROW-NORMAL"),
                lightTheme ? new RectOffset(32, 32, 28, 28) : new RectOffset(34, 34, 30, 30),
                Hex(lightTheme ? (selected ? "E1ECE9" : "F3F1E9") : (selected ? "17343A" : "172A31")));
        }

        public void DrawPriceTag(Rect area, string text, int fontSize, bool enabled)
        {
            var resourceId = Resource(enabled
                ? "ART-UI-BUTTON-REWARD-NORMAL"
                : "ART-UI-BUTTON-REWARD-DISABLED");
            var fallback = enabled
                ? SemanticFallback(UiButtonSemantic.Reward)
                : Hex(lightTheme ? "C8D0CB" : "69767A");
            GUI.color = Color.white;
            // The source price frame is approximately 3.35:1. ScaleToFit keeps both
            // rounded ends intact on the compact recruit card instead of stretching or
            // folding them as a tiny nine-sliced button.
            GUI.DrawTexture(area, Load(resourceId, fallback), ScaleMode.ScaleToFit, true);
            GUI.Label(area, text, new GUIStyle(GUI.skin.label)
            {
                font = unifiedFont,
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = enabled ? textPrimary : textMuted }
            });
        }

        public void DrawBottomNavigation(Rect area)
        {
            DrawTexture(area, Resource("ART-UI-BOTTOM-NAV-BG"), Hex(lightTheme ? "ECEDE5" : "122229"));
        }

        public void DrawDivider(Rect area)
        {
            DrawTexture(area, Resource("ART-UI-DIVIDER-LANTERN-TIDE"), Hex(lightTheme ? "7EA1A7" : "47616A"));
        }

        public void DrawSectionTitleBand(Rect area)
        {
            DrawTexture(area, Resource("ART-UI-SECTION-TITLE-BAND"), Hex(lightTheme ? "ECEDE5" : "122229"));
        }

        private void DrawSurface(Rect area, string styleKey, string resourceId, RectOffset border, Color fallback)
        {
            GUIStyle style;
            if (!surfaceStyles.TryGetValue(styleKey, out style))
            {
                style = new GUIStyle(GUI.skin.box)
                {
                    border = border,
                    padding = new RectOffset(0, 0, 0, 0)
                };
                style.normal.background = Load(resourceId, fallback);
                surfaceStyles[styleKey] = style;
            }
            GUI.Box(area, GUIContent.none, style);
        }

        private void DrawTexture(Rect area, string resourceId, Color fallback)
        {
            GUI.color = Color.white;
            GUI.DrawTexture(area, Load(resourceId, fallback), ScaleMode.StretchToFill, true);
        }

        private Texture2D Load(string resourceId, Color fallback)
        {
            Texture2D texture;
            if (textures.TryGetValue(resourceId, out texture)) return texture;
            texture = Resources.Load<Texture2D>(ResourceRoot + resourceId);
            if (texture == null)
            {
                var sprite = Resources.Load<Sprite>(ResourceRoot + resourceId);
                if (sprite != null) texture = sprite.texture;
            }
            if (texture == null) texture = Fallback(fallback);
            textures[resourceId] = texture;
            return texture;
        }

        private Texture2D Fallback(Color color)
        {
            Texture2D texture;
            if (fallbackTextures.TryGetValue(color, out texture)) return texture;
            texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            fallbackTextures[color] = texture;
            return texture;
        }

        private static void SetBackground(GUIStyleState state, Texture2D background, Color textColor)
        {
            state.background = background;
            state.textColor = textColor;
        }

        private string Resource(string baseId)
        {
            return baseId + "-" + resourceSuffix;
        }

        private Color SemanticFallback(UiButtonSemantic semantic)
        {
            if (lightTheme)
            {
                switch (semantic)
                {
                    case UiButtonSemantic.Primary: return Hex("7FA4AA");
                    case UiButtonSemantic.Secondary: return Hex("DDE5E1");
                    case UiButtonSemantic.Danger: return Hex("A65F67");
                    default: return Hex("C59B4B");
                }
            }
            switch (semantic)
            {
                case UiButtonSemantic.Primary: return Hex("49B7AA");
                case UiButtonSemantic.Secondary: return Hex("56778A");
                case UiButtonSemantic.Danger: return Hex("A45160");
                default: return Hex("B77A35");
            }
        }

        private static Color Hex(string value)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + value, out color) ? color : Color.white;
        }
    }
}
