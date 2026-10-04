using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal sealed class FormalHarborTextureImporter : AssetPostprocessor
    {
        private const string HarborV110Prefix = "Assets/Resources/Art/Harbor/V110/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(HarborV110Prefix, System.StringComparison.Ordinal)) return;

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
