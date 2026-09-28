using System;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    /// <summary>
    /// Keeps the layered White Tide battlefield package on the art hand-off contract.
    /// Reimports remain deterministic instead of depending on local Inspector state.
    /// </summary>
    internal sealed class PrototypeBattleArtImporter : AssetPostprocessor
    {
        private const string BattleArtRoot = "Assets/Resources/Art/Battle/";
        private const string HomeArtRoot = "Assets/Resources/Art/Home/";
        private const string MapArtRoot = "Assets/Resources/Art/Map/";
        private const string GlobalUiRoot = "Assets/Resources/Art/GlobalUI/";
        private const string ExpeditionUiV46Root = "Assets/Resources/Art/ExpeditionUI/V46/";
        private const string ExpeditionUiV47Root = "Assets/Resources/Art/ExpeditionUI/V47/";
        private const string FormationBattleV94Root = "Assets/Resources/Art/FormationBattle/V94/";
        private const string OpaqueFarSea = "ART-BATTLE-WHITE-TIDE-FAR-SEA-01.png";
        private const string OpaqueBattlefield = "ART-BATTLE-CHAPTER-01-ABANDONED-SIGNAL-YARD-01.png";
        private const string OpaqueBattlefieldReadability =
            "ART-BATTLE-CHAPTER-01-ABANDONED-SIGNAL-YARD-READABILITY-02.png";
        private const string OpaqueMapSeaPrefix = "ART-MAP-WHITE-TIDE-SEA-TILE-";
        private const string MapFogEdgePrefix = "ART-MAP-WHITE-TIDE-FOG-EDGE-";
        private const string HomeIslandV3 = "ART-HOME-CH01-LIGHTHOUSE-ISLAND-04.png";
        private const string HomeFlagV3 = "ART-HOME-LIGHTHOUSE-FLAG-4F-02.png";

        private void OnPreprocessTexture()
        {
            var isBattleArt = assetPath.StartsWith(BattleArtRoot, StringComparison.OrdinalIgnoreCase);
            var isHomeArt = assetPath.StartsWith(HomeArtRoot, StringComparison.OrdinalIgnoreCase);
            var isMapArt = assetPath.StartsWith(MapArtRoot, StringComparison.OrdinalIgnoreCase);
            var isGlobalUiV19 = assetPath.StartsWith(GlobalUiRoot, StringComparison.OrdinalIgnoreCase) &&
                                assetPath.EndsWith("-03.png", StringComparison.OrdinalIgnoreCase);
            var isExpeditionUi = assetPath.StartsWith(ExpeditionUiV46Root, StringComparison.OrdinalIgnoreCase) ||
                                 assetPath.StartsWith(ExpeditionUiV47Root, StringComparison.OrdinalIgnoreCase);
            var isFormationBattleV94 = assetPath.StartsWith(FormationBattleV94Root,
                StringComparison.OrdinalIgnoreCase);
            if (!isBattleArt && !isHomeArt && !isMapArt && !isGlobalUiV19 && !isExpeditionUi &&
                !isFormationBattleV94) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            textureSettings.spriteAlignment = (int)SpriteAlignment.Custom;
            if (assetPath.IndexOf(MapFogEdgePrefix, StringComparison.OrdinalIgnoreCase) >= 0)
                textureSettings.spritePivot = new Vector2(1f, 0.5f);
            else if (assetPath.EndsWith(HomeIslandV3, StringComparison.OrdinalIgnoreCase))
                textureSettings.spritePivot = new Vector2(0.5f, 0.86f);
            else if (assetPath.EndsWith(HomeFlagV3, StringComparison.OrdinalIgnoreCase))
                textureSettings.spritePivot = new Vector2(0.08f, 0.27f);
            else textureSettings.spritePivot = new Vector2(0.5f, 0.5f);
            if (isGlobalUiV19)
            {
                textureSettings.spriteBorder = GlobalUiBorder(assetPath);
            }
            else if (isFormationBattleV94 &&
                     assetPath.IndexOf("TILE-SQUARE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                textureSettings.spriteBorder = new Vector4(10f, 10f, 10f, 10f);
            }
            importer.SetTextureSettings(textureSettings);
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            var isMapSea = assetPath.IndexOf(OpaqueMapSeaPrefix, StringComparison.OrdinalIgnoreCase) >= 0;
            importer.wrapModeU = isMapSea ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.wrapModeV = TextureWrapMode.Clamp;
            importer.wrapModeW = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 2048;
            var opaqueHome = isHomeArt &&
                             (assetPath.IndexOf("SEA-CHART-BG", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              assetPath.IndexOf("SEA-CHART-BACKGROUND", StringComparison.OrdinalIgnoreCase) >= 0 ||
                              assetPath.IndexOf("HERO-CENTERED", StringComparison.OrdinalIgnoreCase) >= 0);
            var opaqueExpeditionUnderlay = isExpeditionUi &&
                                           assetPath.IndexOf("runtime-underlay", StringComparison.OrdinalIgnoreCase) >= 0;
            importer.alphaIsTransparency = !opaqueHome && !opaqueExpeditionUnderlay &&
                                           !assetPath.EndsWith(OpaqueFarSea, StringComparison.OrdinalIgnoreCase) &&
                                           !assetPath.EndsWith(OpaqueBattlefield, StringComparison.OrdinalIgnoreCase) &&
                                           !assetPath.EndsWith(OpaqueBattlefieldReadability, StringComparison.OrdinalIgnoreCase) &&
                                           !isMapSea;
            if (isHomeArt)
            {
                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(android);
            }
            else if (isExpeditionUi || isFormationBattleV94)
            {
                var android = importer.GetPlatformTextureSettings("Android");
                android.overridden = true;
                android.maxTextureSize = 2048;
                android.format = opaqueExpeditionUnderlay
                    ? TextureImporterFormat.ASTC_6x6
                    : TextureImporterFormat.ASTC_4x4;
                importer.SetPlatformTextureSettings(android);
            }
        }

        private static Vector4 GlobalUiBorder(string path)
        {
            if (path.IndexOf("PANEL-", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector4(34f, 34f, 34f, 34f);
            if (path.IndexOf("BUTTON-", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector4(38f, 30f, 38f, 30f);
            if (path.IndexOf("LIST-ROW-", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector4(32f, 28f, 32f, 28f);
            if (path.IndexOf("TAB-", StringComparison.OrdinalIgnoreCase) >= 0)
                return new Vector4(34f, 28f, 34f, 28f);
            return Vector4.zero;
        }
    }
}
