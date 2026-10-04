using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Prototype.Persistence
{
    internal static class PrototypeLanternSaveCodec
    {
        public static PrototypeLanternInstanceSaveData[] SaveInventory(LanternInventory inventory)
        {
            if (inventory == null) return new PrototypeLanternInstanceSaveData[0];
            var saved = new PrototypeLanternInstanceSaveData[inventory.Instances.Count];
            for (var index = 0; index < saved.Length; index++)
            {
                var instance = inventory.Instances[index];
                saved[index] = new PrototypeLanternInstanceSaveData
                {
                    lanternInstanceId = instance.LanternInstanceId,
                    templateId = instance.TemplateId,
                    quality = (int)instance.Quality,
                    seasonId = instance.SeasonId,
                    sourceChapterId = instance.SourceChapterId,
                    sourceDifficultyId = instance.SourceDifficultyId,
                    coreEffectId = instance.CoreEffectId,
                    baseFlameStatId = instance.BaseFlameStatId,
                    baseFlameValue = instance.BaseFlameValue,
                    contractStage = instance.ContractStage,
                    modifierRolls = SaveModifiers(instance.ModifierRolls),
                    rewardReceiptId = instance.RewardReceiptId,
                    rulesVersion = instance.RulesVersion,
                    usageLocks = (int)instance.UsageLocks
                };
            }
            return saved;
        }

        public static LanternInventory RestoreInventory(
            PrototypeLanternInstanceSaveData[] saved,
            string activeLanternInstanceId)
        {
            var restored = new List<LanternInstanceState>();
            var restoredIds = new HashSet<string>(StringComparer.Ordinal);
            if (saved != null)
            {
                foreach (var value in saved)
                {
                    if (value == null || string.IsNullOrWhiteSpace(value.lanternInstanceId) ||
                        !restoredIds.Add(value.lanternInstanceId)) continue;
                    try
                    {
                        var quality = Enum.IsDefined(typeof(LanternQuality), value.quality)
                            ? (LanternQuality)value.quality : LanternQuality.Common;
                        var locks = (LanternUsageLock)(value.usageLocks &
                            (int)(LanternUsageLock.Expedition | LanternUsageLock.Defense | LanternUsageLock.Market));
                        restored.Add(new LanternInstanceState(
                            value.lanternInstanceId, value.templateId, quality,
                            value.seasonId, value.sourceChapterId, value.sourceDifficultyId,
                            value.coreEffectId, RestoreModifiers(value.modifierRolls),
                            value.rewardReceiptId, value.rulesVersion, locks,
                            string.IsNullOrWhiteSpace(value.baseFlameStatId)
                                ? FirstChapterLanternFixtures.MaxHealthBasisPointsStatId
                                : value.baseFlameStatId,
                            Math.Max(0, value.baseFlameValue),
                            Math.Max(0, Math.Min(6, value.contractStage))));
                    }
                    catch (ArgumentException)
                    {
                        restoredIds.Remove(value.lanternInstanceId);
                    }
                }
            }
            var inventory = new LanternInventory(restored);
            if (!string.IsNullOrEmpty(activeLanternInstanceId))
                inventory.TrySetActive(activeLanternInstanceId);
            return inventory;
        }

        public static PrototypeLanternSnapshotSaveData SaveSnapshot(LanternSnapshot snapshot)
        {
            if (snapshot == null) return null;
            return new PrototypeLanternSnapshotSaveData
            {
                isNone = snapshot.IsNone,
                lanternInstanceId = snapshot.LanternInstanceId,
                templateId = snapshot.TemplateId,
                quality = (int)snapshot.Quality,
                seasonId = snapshot.SeasonId,
                sourceChapterId = snapshot.SourceChapterId,
                coreEffectId = snapshot.CoreEffectId,
                baseFlameStatId = snapshot.BaseFlameStatId,
                baseFlameValue = snapshot.BaseFlameValue,
                contractStage = snapshot.ContractStage,
                modifierRolls = SaveModifiers(snapshot.ModifierRolls),
                rulesVersion = snapshot.RulesVersion
            };
        }

        public static LanternSnapshot RestoreSnapshot(
            PrototypeLanternSnapshotSaveData saved,
            string fallbackNoneRulesVersion)
        {
            if (saved == null) return LanternSnapshot.None(fallbackNoneRulesVersion);
            try
            {
                var quality = Enum.IsDefined(typeof(LanternQuality), saved.quality)
                    ? (LanternQuality)saved.quality : LanternQuality.Common;
                return LanternSnapshot.Restore(saved.isNone, saved.lanternInstanceId,
                    saved.templateId, quality, saved.seasonId, saved.sourceChapterId,
                    saved.coreEffectId, RestoreModifiers(saved.modifierRolls),
                    string.IsNullOrWhiteSpace(saved.rulesVersion)
                        ? fallbackNoneRulesVersion : saved.rulesVersion,
                    string.IsNullOrWhiteSpace(saved.baseFlameStatId)
                        ? FirstChapterLanternFixtures.MaxHealthBasisPointsStatId
                        : saved.baseFlameStatId,
                    Math.Max(0, saved.baseFlameValue),
                    Math.Max(0, Math.Min(6, saved.contractStage)));
            }
            catch (ArgumentException)
            {
                return LanternSnapshot.None(fallbackNoneRulesVersion);
            }
        }

        public static PrototypeExpeditionRunRewardSnapshotSaveData SaveRewardSnapshot(
            ExpeditionRunRewardSnapshot snapshot)
        {
            if (snapshot == null) return null;
            return new PrototypeExpeditionRunRewardSnapshotSaveData
            {
                difficultyId = snapshot.DifficultyId,
                entryTypeId = snapshot.EntryTypeId,
                paymentTierId = snapshot.PaymentTierId,
                crystalRewards = SaveRewardTable(snapshot.CrystalRewards),
                equipmentRewards = SaveRewardTable(snapshot.EquipmentRewards),
                lanternRewards = SaveRewardTable(snapshot.LanternRewards),
                rulesVersion = snapshot.RulesVersion
            };
        }

        public static ExpeditionRunRewardSnapshot RestoreRewardSnapshot(
            PrototypeExpeditionRunRewardSnapshotSaveData saved,
            ExpeditionRunRewardSnapshot fallback)
        {
            if (saved == null) return fallback;
            try
            {
                return new ExpeditionRunRewardSnapshot(saved.difficultyId, saved.entryTypeId,
                    saved.paymentTierId, RestoreRewardTable(saved.crystalRewards),
                    RestoreRewardTable(saved.equipmentRewards),
                    RestoreRewardTable(saved.lanternRewards), saved.rulesVersion);
            }
            catch (ArgumentException)
            {
                return fallback;
            }
        }

        private static PrototypeLanternModifierSaveData[] SaveModifiers(
            IReadOnlyList<LanternModifierRoll> modifiers)
        {
            if (modifiers == null) return new PrototypeLanternModifierSaveData[0];
            var saved = new PrototypeLanternModifierSaveData[modifiers.Count];
            for (var index = 0; index < saved.Length; index++)
            {
                saved[index] = new PrototypeLanternModifierSaveData
                {
                    modifierId = modifiers[index].ModifierId,
                    value = modifiers[index].Value
                };
            }
            return saved;
        }

        private static LanternModifierRoll[] RestoreModifiers(
            PrototypeLanternModifierSaveData[] saved)
        {
            if (saved == null) return new LanternModifierRoll[0];
            var restored = new List<LanternModifierRoll>();
            foreach (var value in saved)
            {
                if (value == null || string.IsNullOrWhiteSpace(value.modifierId)) continue;
                restored.Add(new LanternModifierRoll(value.modifierId, value.value));
            }
            return restored.ToArray();
        }

        private static PrototypeRewardTableReferenceSaveData SaveRewardTable(
            ExpeditionRewardTableReference table)
        {
            return new PrototypeRewardTableReferenceSaveData
            {
                tableId = table.TableId,
                version = table.Version
            };
        }

        private static ExpeditionRewardTableReference RestoreRewardTable(
            PrototypeRewardTableReferenceSaveData table)
        {
            if (table == null) throw new ArgumentNullException(nameof(table));
            return new ExpeditionRewardTableReference(table.tableId, table.version);
        }
    }
}
