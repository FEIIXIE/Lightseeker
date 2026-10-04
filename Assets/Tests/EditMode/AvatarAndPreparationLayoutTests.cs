using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Tests
{
    public sealed class AvatarAndPreparationLayoutTests
    {
        [Test]
        public void AvatarPack_UsesSixCharactersAndFiveRarities()
        {
            Assert.That(AvatarArtContract.Entries.Count, Is.EqualTo(30));
            var definitions = new HashSet<string>();
            var resources = new HashSet<string>();
            foreach (var entry in AvatarArtContract.Entries)
            {
                Assert.That(definitions.Add(entry.DefinitionId + "|" + entry.QualityId), Is.True,
                    entry.DefinitionId + "|" + entry.QualityId);
                Assert.That(resources.Add(entry.ResourceId), Is.True, entry.ResourceId);
            }

            AvatarArtEntry herbalist;
            Assert.That(AvatarArtContract.TryGet("unit.archer", out herbalist), Is.True);
            Assert.That(herbalist.ResourceId, Is.EqualTo("ART-AVATAR-C04-HERBALIST-COMMON-v01"));
            AvatarArtEntry legendaryHerbalist;
            Assert.That(AvatarArtContract.TryGet("unit.archer", "legendary", out legendaryHerbalist), Is.True);
            Assert.That(legendaryHerbalist.ResourceId,
                Is.EqualTo("ART-AVATAR-C04-MILA-LEGENDARY-TIDEMARSH-MASTER-v01"));
            Assert.That(AvatarArtContract.RequiredLayerOrder,
                Is.EqualTo("SlotSurface -> Avatar -> ForegroundFrameAndInfo"));
            Assert.That(AvatarArtContract.OccupiedSlotSurfaceResourceId,
                Does.StartWith("Slots/"));
            Assert.That(AvatarArtContract.SlotForegroundRimResourceId,
                Does.StartWith("Slots/"));
            Assert.That(AvatarArtContract.OccupiedSlotSurfaceSemanticKey,
                Is.EqualTo("surface.avatar-slot.occupied.104"));
            Assert.That(AvatarArtContract.SlotForegroundRimSemanticKey,
                Is.EqualTo("frame.avatar-slot.foreground.104"));

            Assert.That(AvatarArtContract.ContextSpecs.Count, Is.EqualTo(5));
            AssertContext(AvatarArtContext.SlotCircle, 104, 104, 10, 10, 84, 84);
            AssertContext(AvatarArtContext.PreparationList, 90, 104, 0, 7, 90, 90);
            AssertContext(AvatarArtContext.Recruit, 92, 86, 3, 0, 86, 86);
            AssertContext(AvatarArtContext.DragGhost, 156, 210, 0, 27, 156, 156);
            AssertContext(AvatarArtContext.Detail, 500, 370, 65, 0, 370, 370);
        }

        private static void AssertContext(AvatarArtContext context, int width, int height,
            int entityX, int entityY, int entityWidth, int entityHeight)
        {
            var spec = AvatarArtContract.GetContextSpec(context);
            Assert.That(spec.CanvasWidth, Is.EqualTo(width));
            Assert.That(spec.CanvasHeight, Is.EqualTo(height));
            Assert.That(spec.EntityX, Is.EqualTo(entityX));
            Assert.That(spec.EntityY, Is.EqualTo(entityY));
            Assert.That(spec.EntityWidth, Is.EqualTo(entityWidth));
            Assert.That(spec.EntityHeight, Is.EqualTo(entityHeight));
        }

        [Test]
        public void ImportedAvatars_UseAllFiveNativeContextContracts()
        {
            foreach (var entry in AvatarArtContract.Entries)
            {
                foreach (var spec in AvatarArtContract.ContextSpecs)
                {
                    var path = "Assets/Resources/" + spec.ResourcePath(entry.ResourceId) + ".png";
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    Assert.That(texture, Is.Not.Null, path);
                    Assert.That(texture.width, Is.EqualTo(spec.CanvasWidth), path);
                    Assert.That(texture.height, Is.EqualTo(spec.CanvasHeight), path);

                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    Assert.That(importer, Is.Not.Null, path);
                    Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                    Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Single), path);
                    Assert.That(importer.mipmapEnabled, Is.False, path);
                    Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), path);
                    Assert.That(importer.textureCompression,
                        Is.EqualTo(TextureImporterCompression.Uncompressed), path);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    Assert.That(settings.spriteMeshType, Is.EqualTo(SpriteMeshType.FullRect), path);
                    Assert.That(settings.spritePivot.x,
                        Is.EqualTo(AvatarArtContract.PivotX).Within(0.0001f), path);
                    Assert.That(settings.spritePivot.y,
                        Is.EqualTo(AvatarArtContract.PivotY).Within(0.0001f), path);
                }
            }

            var legacyMasters = AssetDatabase.FindAssets("t:Texture2D",
                new[] { "Assets/Resources/Art/Avatars" });
            Assert.That(legacyMasters.Length, Is.EqualTo(150),
                "Only the 150 accepted context derivatives may remain in runtime Resources.");
        }

        [Test]
        public void LegacyHeight_UsesRequestedFormationSeparation()
        {
            var layout = PreparationBattlefieldLayoutMath.Calculate(1920f);
            Assert.That(layout.EnemyFrontFootY, Is.EqualTo(640f));
            Assert.That(layout.EnemyBackFootY, Is.EqualTo(830f));
            Assert.That(layout.PlayerFrontFootY, Is.EqualTo(1140f));
            Assert.That(layout.PlayerBackFootY, Is.EqualTo(1330f));
            Assert.That(layout.StartButtonY, Is.EqualTo(1370f));
        }

        [Test]
        public void FortyByEightyNineHeight_SeparatesNamesLevelsAndFooter()
        {
            var layout = PreparationBattlefieldLayoutMath.Calculate(2400f);
            Assert.That(layout.EnemyFrontFootY, Is.EqualTo(1010f).Within(0.01f));
            Assert.That(layout.EnemyBackFootY, Is.EqualTo(670f).Within(0.01f));
            Assert.That(layout.PlayerFrontFootY, Is.EqualTo(1350f).Within(0.01f));
            Assert.That(layout.PlayerBackFootY, Is.EqualTo(1710f).Within(0.01f));
            Assert.That(layout.PlayerBackFootY - layout.PlayerFrontFootY, Is.EqualTo(360f).Within(0.01f));

            const float eliteVisibleHeight = 189f;
            const float expandedNameBottomOffset = 26f + 38f;
            const float levelBottomHeadGap = 10f;
            var enemyRearNameBottom = layout.EnemyBackFootY + expandedNameBottomOffset;
            var enemyFrontLevelBottom = layout.EnemyFrontFootY - eliteVisibleHeight - levelBottomHeadGap;
            Assert.That(enemyFrontLevelBottom - enemyRearNameBottom, Is.GreaterThanOrEqualTo(20f));

            var playerLowerNameBottom = layout.PlayerBackFootY + expandedNameBottomOffset;
            var buffTop = FormationBattleV94Layout.BuffSingleRowY -
                          FormationBattleV94Layout.BuffTileSize * 0.5f;
            Assert.That(buffTop - playerLowerNameBottom, Is.GreaterThanOrEqualTo(24f));
            Assert.That(layout.StartButtonY, Is.EqualTo(FormationBattleV94Layout.StartControl.Y));
            Assert.That(layout.ToolsY + FormationBattleV94Layout.RecycleControl.Height * 0.5f,
                Is.EqualTo(layout.StartButtonY + FormationBattleV94Layout.StartControl.Height * 0.5f)
                    .Within(0.01f));
            Assert.That(layout.BuffY, Is.EqualTo(FormationBattleV94Layout.BuffSingleRowY));
            Assert.That(layout.BenchLabelY, Is.EqualTo(FormationBattleV94Layout.BenchLabel.Y));
            Assert.That(layout.BenchY, Is.EqualTo(FormationBattleV94Layout.BenchCenterY -
                FormationBattleV94Layout.BenchSlotDiameter * 0.5f));

            Assert.That(FormationBattleV94Layout.UnitFootPosition(true, 0).X, Is.EqualTo(205f));
            Assert.That(FormationBattleV94Layout.UnitFootPosition(true, 1).X, Is.EqualTo(540f));
            Assert.That(FormationBattleV94Layout.UnitFootPosition(true, 2).X, Is.EqualTo(875f));
        }

        [Test]
        public void SeekerSafeHeight_KeepsFooterInsideContentAndClearOfFormation()
        {
            const float seekerSafeContentHeight = 2250f;
            var layout = PreparationBattlefieldLayoutMath.Calculate(seekerSafeContentHeight);
            var benchBottom = layout.BenchY + FormationBattleV94Layout.BenchSlotDiameter;
            var lowerNameBottom = layout.PlayerBackFootY + 76f;
            var buffTop = layout.BuffY - FormationBattleV94Layout.BuffTileSize * 0.5f;
            var buffBottom = layout.BuffY + FormationBattleV94Layout.BuffTileSize * 0.5f;

            Assert.That(benchBottom, Is.LessThanOrEqualTo(seekerSafeContentHeight - 24f));
            Assert.That(buffTop - lowerNameBottom, Is.GreaterThanOrEqualTo(24f));
            Assert.That(layout.StartButtonY - buffBottom, Is.GreaterThanOrEqualTo(24f));
            Assert.That(layout.ToolsY + FormationBattleV94Layout.RecycleControl.Height * 0.5f,
                Is.EqualTo(layout.StartButtonY + FormationBattleV94Layout.StartControl.Height * 0.5f)
                    .Within(0.01f));
            Assert.That(layout.BenchY, Is.GreaterThan(layout.ToolsY));
        }

        [Test]
        public void V94BossFootprint_ReservesVerticalCenterPair()
        {
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(0), Is.EqualTo(1));
            Assert.That(FormationBattleV94Layout.IsBossReservedSlot(1), Is.True);
            Assert.That(FormationBattleV94Layout.IsBossReservedSlot(4), Is.True);
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(1), Is.EqualTo(0));
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(2), Is.EqualTo(2));
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(3), Is.EqualTo(3));
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(4), Is.EqualTo(5));
            Assert.That(FormationBattleV94Layout.BossFormationSlotForEnemyIndex(5), Is.EqualTo(-1));
        }

        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(5, 5)]
        [TestCase(6, 5)]
        [TestCase(12, 5)]
        public void V94BuffLayout_HidesZeroAndCapsAtFive(int requested, int visible)
        {
            var centers = FormationBattleV94Layout.BuffCenters(requested);
            Assert.That(centers.Count, Is.EqualTo(visible));
            foreach (var point in centers)
                Assert.That(point.Y, Is.EqualTo(FormationBattleV94Layout.BuffSingleRowY));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(5)]
        [TestCase(6)]
        public void V94StatusGroups_AreCenteredForEveryDisplayedCount(int displayed)
        {
            var centers = FormationBattleV94Layout.CenteredStatusGroupCenters(
                displayed, FormationBattleV94Layout.SynergyCenterY);
            var totalX = 0f;
            foreach (var point in centers) totalX += point.X;

            Assert.That(centers.Count, Is.EqualTo(displayed));
            Assert.That(totalX / centers.Count,
                Is.EqualTo(FormationBattleV94Layout.CanvasWidth * 0.5f).Within(0.01f));
            Assert.That(FormationBattleV94Layout.BuffTileSize,
                Is.EqualTo(FormationBattleV94Layout.SynergyTileSize));
        }

        [Test]
        public void V94Bench_ProvidesSixVisibleSlotsBelowStartControl()
        {
            for (var index = 0; index < 6; index++)
            {
                var slot = FormationBattleV94Layout.BenchSlot(index);
                Assert.That(slot.Y, Is.GreaterThan(FormationBattleV94Layout.StartControl.Y +
                    FormationBattleV94Layout.StartControl.Height));
                Assert.That(slot.X, Is.GreaterThanOrEqualTo(0f));
                Assert.That(slot.X + slot.Width, Is.LessThanOrEqualTo(FormationBattleV94Layout.CanvasWidth));
            }
        }

        [Test]
        public void V94FormationOverlayExit_UsesSharedCubicPreset()
        {
            Assert.That(FormationBattleV94Layout.FormationOverlayAlpha(0f), Is.EqualTo(1f));
            Assert.That(FormationBattleV94Layout.FormationOverlayAlpha(1f), Is.EqualTo(0f));
            Assert.That(FormationBattleV94Layout.FormationOverlayScale(0f), Is.EqualTo(1f));
            Assert.That(FormationBattleV94Layout.FormationOverlayScale(1f),
                Is.EqualTo(FormationBattleV94Layout.TransitionEndScale).Within(0.0001f));
        }
    }
}
