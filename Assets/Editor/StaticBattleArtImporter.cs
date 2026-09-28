using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal sealed class StaticBattleArtImporter : AssetPostprocessor
    {
        private const string AssetFolder = "Assets/Resources/Art/BattleUnits/";
        private const string AvatarAssetFolder = "Assets/Resources/Art/Avatars/";
        private const string AvatarSlotAssetFolder = "Assets/Resources/Art/Avatars/SlotCircular/";

        [InitializeOnLoadMethod]
        private static void EnforcePackAfterScriptReload()
        {
            EditorApplication.delayCall += () =>
            {
                foreach (var entry in StaticBattleArtContract.Entries)
                {
                    var path = AssetFolder + entry.ResourceId + ".png";
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || !NeedsConfiguration(importer)) continue;
                    Configure(importer);
                    importer.SaveAndReimport();
                }
                foreach (var entry in AvatarArtContract.Entries)
                {
                    EnforceAvatar(AvatarAssetFolder + entry.ResourceId + ".png");
                    EnforceAvatar(AvatarSlotAssetFolder + entry.ResourceId +
                                  AvatarArtContract.SlotResourceSuffix + ".png");
                }
            };
        }

        private void OnPreprocessTexture()
        {
            var importer = assetImporter as TextureImporter;
            if (importer == null) return;
            if (assetPath.StartsWith(AssetFolder, System.StringComparison.Ordinal)) Configure(importer);
            else if (assetPath.StartsWith(AvatarAssetFolder, System.StringComparison.Ordinal)) ConfigureAvatar(importer);
        }

        private static void EnforceAvatar(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || !NeedsAvatarConfiguration(importer)) return;
            ConfigureAvatar(importer);
            importer.SaveAndReimport();
        }

        private static void Configure(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(StaticBattleArtContract.PivotX, StaticBattleArtContract.PivotY);
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }

        private static bool NeedsConfiguration(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return importer.textureType != TextureImporterType.Sprite ||
                   importer.spriteImportMode != SpriteImportMode.Single ||
                   settings.spriteMeshType != SpriteMeshType.FullRect ||
                   settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                   Vector2.Distance(settings.spritePivot,
                       new Vector2(StaticBattleArtContract.PivotX, StaticBattleArtContract.PivotY)) > 0.0001f ||
                   importer.mipmapEnabled ||
                   importer.filterMode != FilterMode.Bilinear ||
                   importer.textureCompression != TextureImporterCompression.Uncompressed;
        }

        private static void ConfigureAvatar(TextureImporter importer)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = new Vector2(AvatarArtContract.PivotX, AvatarArtContract.PivotY);
            importer.SetTextureSettings(settings);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
        }

        private static bool NeedsAvatarConfiguration(TextureImporter importer)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return importer.textureType != TextureImporterType.Sprite ||
                   importer.spriteImportMode != SpriteImportMode.Single ||
                   settings.spriteMeshType != SpriteMeshType.FullRect ||
                   settings.spriteAlignment != (int)SpriteAlignment.Custom ||
                   Vector2.Distance(settings.spritePivot,
                       new Vector2(AvatarArtContract.PivotX, AvatarArtContract.PivotY)) > 0.0001f ||
                   importer.mipmapEnabled ||
                   importer.filterMode != FilterMode.Bilinear ||
                   importer.textureCompression != TextureImporterCompression.Uncompressed;
        }
    }
}
