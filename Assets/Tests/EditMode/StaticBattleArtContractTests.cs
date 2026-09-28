using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Tests
{
    public sealed class StaticBattleArtContractTests
    {
        [Test]
        public void ChapterOnePack_HasSixteenUniqueDefinitionsAndResources()
        {
            Assert.That(StaticBattleArtContract.Entries.Count, Is.EqualTo(16));
            var definitionIds = new HashSet<string>();
            var resourceIds = new HashSet<string>();
            foreach (var entry in StaticBattleArtContract.Entries)
            {
                Assert.That(definitionIds.Add(entry.DefinitionId), Is.True, entry.DefinitionId);
                Assert.That(resourceIds.Add(entry.ResourceId), Is.True, entry.ResourceId);
                Assert.That(entry.ResourceId, Does.StartWith("ART-"));
            }
        }

        [Test]
        public void PresentationScale_MatchesApprovedSharedLibraryManifest()
        {
            foreach (var entry in StaticBattleArtContract.Entries)
            {
                var expected = entry.Tier == StaticBattleArtTier.Elite ||
                               entry.Tier == StaticBattleArtTier.Boss
                    ? 1.5f
                    : entry.DefinitionId == "enemy.n4.skirmisher" ? 1.2f : 1f;
                Assert.That(entry.PresentationScale, Is.EqualTo(expected).Within(0.0001f), entry.DefinitionId);

                var expectedShadow = entry.Tier == StaticBattleArtTier.Elite
                    ? 1.25f
                    : entry.Tier == StaticBattleArtTier.Boss ? 1.55f : 1f;
                Assert.That(entry.ShadowScale, Is.EqualTo(expectedShadow).Within(0.0001f), entry.DefinitionId);
            }
        }

        [Test]
        public void SharedCanvasPivotAndGroundLine_MatchHandoff()
        {
            Assert.That(StaticBattleArtContract.CanvasWidth, Is.EqualTo(1212));
            Assert.That(StaticBattleArtContract.CanvasHeight, Is.EqualTo(1297));
            Assert.That(StaticBattleArtContract.GroundLineTopOrigin, Is.EqualTo(1281));
            Assert.That(StaticBattleArtContract.PivotX, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(StaticBattleArtContract.PivotY, Is.EqualTo(0.0123f).Within(0.0001f));
            Assert.That(StaticBattleArtContract.FormalBattleFlipX, Is.False,
                "Formal battle uses fixed front-facing masters and must not mirror them.");
        }

        [Test]
        public void MistShepherd_RetainsApprovedIdentityAndBossScale()
        {
            var entry = StaticBattleArtContract.Get("enemy.b2.hunter");
            Assert.That(entry.ResourceId, Is.EqualTo("ART-MONSTER-CH01-B2-MIST-SHEPHERD-01"));
            Assert.That(entry.DisplayNameZh, Is.EqualTo("引雾牧者"));
            Assert.That(entry.DisplayNameEn, Is.EqualTo("Mist Shepherd"));
            Assert.That(entry.Tier, Is.EqualTo(StaticBattleArtTier.Boss));
            Assert.That(entry.PresentationScale, Is.EqualTo(1.5f));
            Assert.That(entry.ShadowScale, Is.EqualTo(1.55f));
        }

        [Test]
        public void EchoBellkeeper_UsesApprovedBurdenFrameMaster()
        {
            var entry = StaticBattleArtContract.Get("enemy.e3.caster");
            Assert.That(entry.ResourceId, Is.EqualTo("ART-MONSTER-CH01-E3-ECHO-BELLKEEPER-02"));
            Assert.That(entry.MinX, Is.EqualTo(303));
            Assert.That(entry.MinY, Is.EqualTo(370));
            Assert.That(entry.MaxX, Is.EqualTo(910));
            Assert.That(entry.MaxY, Is.EqualTo(1269));
            Assert.That(entry.Tier, Is.EqualTo(StaticBattleArtTier.Elite));
            Assert.That(entry.PresentationScale, Is.EqualTo(1.5f));
            Assert.That(entry.ShadowScale, Is.EqualTo(1.25f));
        }

        [Test]
        public void MistLeapingRay_UsesApprovedScaleAndVerticalOffset()
        {
            var entry = StaticBattleArtContract.Get("enemy.n4.skirmisher");
            Assert.That(entry.PresentationScale, Is.EqualTo(1.2f).Within(0.0001f));
            Assert.That(entry.VisualYOffsetRelativeToUnitHeight, Is.EqualTo(-0.12f).Within(0.0001f));
        }

        [Test]
        public void PlayerFallbacks_UseCanonicalCommonRarityArt()
        {
            var expected = new Dictionary<string, string>
            {
                { "unit.guardian", "ART-CHAR-C01-LAMPGUARD-COMMON-v02" },
                { "unit.healer", "ART-CHAR-C02-LAMPLADY-COMMON-v01" },
                { "unit.thornGuard", "ART-CHAR-C03-RANGER-COMMON-v03" },
                { "unit.archer", "ART-CHAR-C04-HERBALIST-COMMON-v01" },
                { "unit.starblade", "ART-CHAR-C05-STONEWARD-COMMON-v01" },
                { "unit.starpriest", "ART-CHAR-C06-EMBERHAND-COMMON-v01" }
            };

            foreach (var pair in expected)
                Assert.That(StaticBattleArtContract.Get(pair.Key).ResourceId,
                    Is.EqualTo(pair.Value), pair.Key);
        }

        [Test]
        public void OlinFiveRarities_UseApprovedFixedFrontMasters()
        {
            var expected = new Dictionary<string, string>
            {
                { "common", "ART-CHAR-C01-LAMPGUARD-COMMON-v02" },
                { "uncommon", "ART-CHAR-C01-LAMPGUARD-UNCOMMON-v02" },
                { "rare", "ART-CHAR-C01-LAMPGUARD-RARE-v02" },
                { "epic", "ART-CHAR-C01-OLIN-EPIC-BREAKWATER-TOWER-GUARD-v06" },
                { "legendary", "ART-CHAR-C01-OLIN-LEGENDARY-TIDEGATE-TOWER-WARDEN-v04" }
            };

            foreach (var pair in expected)
            {
                CharacterRarityArtEntry entry;
                Assert.That(CharacterRarityArtContract.TryGet(
                    "unit.guardian", pair.Key, out entry), Is.True, pair.Key);
                Assert.That(entry.ResourceId, Is.EqualTo(pair.Value), pair.Key);
            }
        }

        [Test]
        public void ImportedSprites_UseFormalFullRectContract()
        {
            foreach (var entry in StaticBattleArtContract.Entries)
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
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                Assert.That(settings.spritePivot.x, Is.EqualTo(StaticBattleArtContract.PivotX).Within(0.0001f), path);
                Assert.That(settings.spritePivot.y, Is.EqualTo(StaticBattleArtContract.PivotY).Within(0.0001f), path);
            }
        }
    }
}
