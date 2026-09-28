using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Tests
{
    public sealed class PieceBasePresentationContractTests
    {
        [Test]
        public void DefaultSkin_UsesApprovedCanvasAnchorAndResource()
        {
            Assert.That(PieceBasePresentationContract.DefaultSkinId,
                Is.EqualTo("base.harborWoodIron.v04"));
            Assert.That(PieceBasePresentationContract.DefaultResourceId,
                Is.EqualTo("BASE-HARBOR-WOOD-IRON-MAIN-v04"));
            Assert.That(PieceBasePresentationContract.CanvasWidth, Is.EqualTo(2048));
            Assert.That(PieceBasePresentationContract.CanvasHeight, Is.EqualTo(768));
            Assert.That(PieceBasePresentationContract.AnchorPixelX, Is.EqualTo(1024));
            Assert.That(PieceBasePresentationContract.AnchorPixelYTopOrigin, Is.EqualTo(330));
            Assert.That(PieceBasePresentationContract.FootAnchorTopNormalizedX,
                Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(PieceBasePresentationContract.FootAnchorTopNormalizedY,
                Is.EqualTo(0.43f).Within(0.0001f));
        }

        [Test]
        public void RuntimeWidth_AdaptsFromVisibleCharacterWidth()
        {
            Assert.That(PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(130f),
                Is.EqualTo(162.5f).Within(0.001f));
            Assert.That(PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(220f),
                Is.EqualTo(275f).Within(0.001f));
            Assert.That(PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(400f),
                Is.EqualTo(275.2f).Within(0.001f));
        }

        [Test]
        public void SkinLookup_IsReplaceableWithoutChangingAnchor()
        {
            PieceBaseSkinDefinition skin;
            Assert.That(PieceBasePresentationContract.TryGetSkin(
                PieceBasePresentationContract.DefaultSkinId, out skin), Is.True);
            Assert.That(skin.ResourcesPath, Is.EqualTo(
                "Art/BattlePieces/BASE-HARBOR-WOOD-IRON-MAIN-v04"));
            Assert.That(PieceBasePresentationContract.TryGetSkin("missing", out skin), Is.False);
            Assert.That(PieceBasePresentationContract.FootAnchorTopNormalizedX, Is.EqualTo(0.5f));
            Assert.That(PieceBasePresentationContract.FootAnchorTopNormalizedY, Is.EqualTo(0.43f));
        }

        [Test]
        public void ImportedDefaultSkin_UsesApprovedSpriteSettings()
        {
            const string path = "Assets/Resources/Art/BattlePieces/BASE-HARBOR-WOOD-IRON-MAIN-v04.png";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(PieceBasePresentationContract.CanvasWidth), path);
            Assert.That(texture.height, Is.EqualTo(PieceBasePresentationContract.CanvasHeight), path);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            Assert.That(importer, Is.Not.Null, path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
            Assert.That(settings.spritePivot.x,
                Is.EqualTo(PieceBasePresentationContract.FootAnchorTopNormalizedX).Within(0.0001f), path);
            Assert.That(settings.spritePivot.y,
                Is.EqualTo(PieceBasePresentationContract.FootAnchorTopNormalizedY).Within(0.0001f), path);
        }
    }
}
