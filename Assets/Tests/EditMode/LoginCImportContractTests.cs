using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Tests
{
    public sealed class LoginCImportContractTests
    {
        private const string BackgroundPath =
            "Assets/Resources/Art/LoginC/V102/modules/" +
            "ART-UI-LOGIN-C-BACKGROUND-1080x2400-v1.0.2.png";

        [Test]
        public void AcceptedBackground_RemainsNativeSizeInUnity()
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            Assert.That(texture, Is.Not.Null, BackgroundPath);
            Assert.That(texture.width, Is.EqualTo(1080), BackgroundPath);
            Assert.That(texture.height, Is.EqualTo(2400), BackgroundPath);

            var importer = AssetImporter.GetAtPath(BackgroundPath) as TextureImporter;
            Assert.That(importer, Is.Not.Null, BackgroundPath);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), BackgroundPath);
            Assert.That(importer.maxTextureSize, Is.EqualTo(4096), BackgroundPath);
            Assert.That(importer.textureCompression,
                Is.EqualTo(TextureImporterCompression.Uncompressed), BackgroundPath);
        }
    }
}
