using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal sealed class FormalArenaTextureImporter : AssetPostprocessor
    {
        private const string ArenaFormalPrefix = "Assets/Resources/Art/Arena/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArenaFormalPrefix, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
