using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal sealed class FormalLanternTextureImporter : AssetPostprocessor
    {
        private const string LegacyRuntimeRoot =
            "Assets/Resources/Art/Lanterns/Chapter01/GuardTideCircular/";
        private const string HomeModularRuntimeRoot =
            "Assets/Resources/Art/HomeLanternModular/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(LegacyRuntimeRoot, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(HomeModularRuntimeRoot, System.StringComparison.Ordinal)) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }
}
