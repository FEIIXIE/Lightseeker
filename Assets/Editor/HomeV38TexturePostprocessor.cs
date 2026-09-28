using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    /// <summary>
    /// The locked home master is 1080 x 2400. Unity's default 2048 import cap
    /// would silently resize it, so all E36 layers are kept at source resolution.
    /// </summary>
    internal sealed class HomeV38TexturePostprocessor : AssetPostprocessor
    {
        private const string RuntimeLayerRoot = "Assets/Resources/Art/HomeV38/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(RuntimeLayerRoot, System.StringComparison.Ordinal)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.textureShape = TextureImporterShape.Texture2D;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
