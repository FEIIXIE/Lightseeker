using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Tests
{
    public sealed class CharacterRarityArtContractTests
    {
        private static readonly string[] QualityIds =
        {
            "common", "uncommon", "rare", "epic", "legendary"
        };

        [Test]
        public void LaunchCharacters_HaveAllThirtyCanonicalRarityEntries()
        {
            Assert.That(CharacterRarityArtContract.Entries.Count, Is.EqualTo(30));
            var keys = new HashSet<string>();
            foreach (var entry in CharacterRarityArtContract.Entries)
                Assert.That(keys.Add(entry.DefinitionId + "|" + entry.QualityId), Is.True);

            foreach (var definitionId in new[]
            {
                "unit.guardian", "unit.healer", "unit.thornGuard",
                "unit.archer", "unit.starblade", "unit.starpriest"
            })
            foreach (var qualityId in QualityIds)
            {
                CharacterRarityArtEntry entry;
                Assert.That(CharacterRarityArtContract.TryGet(definitionId, qualityId, out entry),
                    Is.True, definitionId + " / " + qualityId);
            }
        }

        [Test]
        public void Ranger_UsesBowstringCorrectedV03ForEveryRarity()
        {
            foreach (var qualityId in QualityIds)
            {
                CharacterRarityArtEntry entry;
                Assert.That(CharacterRarityArtContract.TryGet(
                    "unit.thornGuard", qualityId, out entry), Is.True);
                Assert.That(entry.ResourceId, Does.EndWith("v03"), qualityId);
            }
        }

        [Test]
        public void ImportedRaritySprites_UseSharedCanvasAndPivot()
        {
            foreach (var entry in CharacterRarityArtContract.Entries)
            {
                var path = "Assets/Resources/Art/BattleUnits/" + entry.ResourceId + ".png";
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                Assert.That(texture, Is.Not.Null, path);
                Assert.That(texture.width, Is.EqualTo(StaticBattleArtContract.CanvasWidth), path);
                Assert.That(texture.height, Is.EqualTo(StaticBattleArtContract.CanvasHeight), path);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                Assert.That(importer, Is.Not.Null, path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                Assert.That(importer.mipmapEnabled, Is.False, path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                Assert.That(settings.spritePivot.x,
                    Is.EqualTo(StaticBattleArtContract.PivotX).Within(0.0001f), path);
                Assert.That(settings.spritePivot.y,
                    Is.EqualTo(StaticBattleArtContract.PivotY).Within(0.0001f), path);
            }
        }
    }
}
