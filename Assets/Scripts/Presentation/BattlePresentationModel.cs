using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    /// <summary>
    /// Stable presentation identifiers. Animator, SpriteRenderer and pooled VFX views can
    /// consume these without becoming part of deterministic combat resolution.
    /// </summary>
    internal static class BattlePresentationIds
    {
        public const string Idle = "animation.idle";
        public const string Move = "animation.move";
        public const string Attack = "animation.attack";
        public const string Heal = "animation.heal";
        public const string ReceiveHeal = "animation.receive-heal";
        public const string Hit = "animation.hit";
        public const string Defeat = "animation.defeat";

        public const string AttackEffect = "effect.attack.basic";
        public const string HealEffect = "effect.heal.basic";
        public const string DefeatEffect = "effect.defeat.basic";

        public const string FeetAnchor = "anchor.feet";
        public const string BodyAnchor = "anchor.body";
        public const string HealthBarAnchor = "anchor.healthbar";
        public const string WeaponAnchor = "anchor.weapon";
        public const string SkillOriginAnchor = "anchor.skill-origin";
        public const string HitAnchor = "anchor.hit";
    }

    internal static class BattleProjectileProfileIds
    {
        public const string LightHit = "PP_LIGHT_HIT";
        public const string LightHeal = "PP_LIGHT_HEAL";
        public const string ArrowStandard = "PP_ARROW_STANDARD";
        public const string SeedAttack = "PP_SEED_ATTACK";
        public const string SeedHeal = "PP_SEED_HEAL";
        public const string HiddenSpringSeed = "PP_HIDDEN_SPRING_SEED";
        public const string ArrowWhiteTrail = "PP_ARROW_WHITE_TRAIL";
        public const string FirePot = "PP_FIRE_POT";
        public const string CrossbowStandard = "PP_CROSSBOW_STANDARD";
        public const string CrossbowHeavy = "PP_CROSSBOW_HEAVY";
        public const string ClothHeal = "PP_CLOTH_HEAL";
    }

    internal static class BattleFeedbackProfileIds
    {
        public const string HitFeedback = "HIT_FEEDBACK";
        public const string HealFeedback = "HEAL_FEEDBACK";
        public const string HealFeedbackRing = "HEAL_FEEDBACK_RING";
        public const string HealFeedbackRise = "HEAL_FEEDBACK_RISE";
        public const string ShieldOverlayGeneric = "SHIELD_OVERLAY_GENERIC";
        public const string OpenDawn = "FX_C01_OPEN_DAWN";
        public const string OlinTide = "FX_C01_OLIN_TIDE";
        public const string RescueBeam = "PP_RESCUE_BEAM";
        public const string HitReaction = "HIT_REACTION";
        public const string HostileHitFeedbackV2 = "HIT_FEEDBACK_HOSTILE_V2";
        public const string C03WhiteMark = "FX_C03_WHITE_MARK";
        public const string C03MarkTransfer = "FX_C03_MARK_TRANSFER";
        public const string C04AfterSpringRelease = "FX_C04_AFTER_SPRING_RELEASE";
    }

    internal static class BattleProjectileProfiles
    {
        public const float BaseDisplayWidth = 96f;
        public const float DefaultProjectileDisplayScale = 1.5f;
        public const string LightHitResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-LIGHT-HIT-R01-v02-accepted";
        public const string LightHealResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-LIGHT-HEAL-R02-v02-accepted";
        public const string ArrowStandardResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-ARROW-STANDARD-R03-v02-accepted";
        public const string SeedAttackResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-SEED-ATTACK-R08-v01-accepted";
        public const string SeedHealResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-SEED-HEAL-R09-v01-accepted";
        public const string HiddenSpringSeedResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-HIDDEN-SPRING-SEED-R10-v01-accepted";
        public const string ArrowWhiteTrailResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-ARROW-WHITE-TRAIL-R07-v01-accepted";
        public const string FirePotResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-FIRE-POT-R11-v01-accepted";
        public const string CrossbowStandardResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-CROSSBOW-STANDARD-R12-v01-accepted";
        public const string CrossbowHeavyResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-CROSSBOW-HEAVY-R13-v01-accepted";
        public const string ClothHealResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-CLOTH-HEAL-R14-v01-accepted";
        public const string HitFeedbackResourcePath =
            "Art/VFX/ART-VFX-REVISION-HIT-FEEDBACK-R04-v02-accepted";
        public const string HealFeedbackResourcePath =
            "Art/VFX/ART-VFX-REVISION-HEAL-FEEDBACK-R05-v02-accepted";
        public const string HealFeedbackRingResourcePath =
            "Art/VFX/ART-VFX-REVISION-HEAL-FEEDBACK-RING-R18-v01-accepted";
        public const string HealFeedbackRiseResourcePath =
            "Art/VFX/ART-VFX-REVISION-HEAL-FEEDBACK-RISE-STAR-R29-v01-accepted";
        public const string HealFeedbackRiseLegacyResourcePath =
            "Art/VFX/ART-VFX-REVISION-HEAL-FEEDBACK-RISE-V2-R25-v01-accepted";
        public const string ShieldOverlayGenericResourcePath =
            "Art/VFX/ART-VFX-REVISION-SHIELD-OVERLAY-GENERIC-R20-v01-accepted";
        public const string OpenDawnResourcePath =
            "Art/VFX/ART-VFX-REVISION-FX-C01-OPEN-DAWN-R16-v01-accepted";
        public const string OlinTideResourcePath =
            "Art/VFX/ART-VFX-REVISION-FX-C01-OLIN-TIDE-R17-v01-accepted";
        public const string RescueBeamResourcePath =
            "Art/VFX/ART-VFX-REVISION-PP-RESCUE-BEAM-R21-v01-accepted";
        public const string HitReactionResourcePath =
            "Art/VFX/ART-VFX-REVISION-HIT-REACTION-R06-v02-accepted";
        public const string HostileHitFeedbackV2ResourcePath =
            "Art/VFX/ART-VFX-REVISION-HIT-FEEDBACK-HOSTILE-V2-R24-v01-accepted";
        public const string C03WhiteMarkResourcePath =
            "Art/VFX/ART-VFX-REVISION-FX-C03-WHITE-MARK-R26-v01-accepted";
        public const string C03MarkTransferResourcePath =
            "Art/VFX/ART-VFX-REVISION-FX-C03-MARK-TRANSFER-R27-v01-accepted";
        public const string C04AfterSpringReleaseResourcePath =
            "Art/VFX/ART-VFX-REVISION-FX-C04-AFTER-SPRING-RELEASE-R28-v01-accepted";
        public const int LightHitTravelMilliseconds = 220;
        public const int LightHealTravelMilliseconds = 260;
        public const int ArrowStandardTravelMilliseconds = 260;
        public const int SeedTravelMilliseconds = 300;
        public const float LightHitDurationSeconds = 0.48f;
        public const float LightHitReleaseProgress = 0.45833334f;
        public const float LightHitImpactProgress = 0.9166667f;
        public const float LightHealDurationSeconds = 0.52f;
        public const float LightHealReleaseProgress = 0.40f;
        public const float LightHealImpactProgress = 0.90f;
        public const float ArrowStandardDurationSeconds = 0.58f;
        public const float ArrowStandardReleaseProgress = 0.45f;
        public const float ArrowStandardImpactProgress = 0.89827585f;
        public const float SeedDurationSeconds = SeedTravelMilliseconds / 1000f /
            (0.90f - 0.45f);

        public static string ResourcePathFor(string profileId)
        {
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal)) return LightHitResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return LightHealResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return ArrowStandardResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal)) return SeedAttackResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal)) return SeedHealResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.HiddenSpringSeed,
                    StringComparison.Ordinal)) return HiddenSpringSeedResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowWhiteTrail,
                    StringComparison.Ordinal)) return ArrowWhiteTrailResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.FirePot,
                    StringComparison.Ordinal)) return FirePotResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.CrossbowStandard,
                    StringComparison.Ordinal)) return CrossbowStandardResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.CrossbowHeavy,
                    StringComparison.Ordinal)) return CrossbowHeavyResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.ClothHeal,
                    StringComparison.Ordinal)) return ClothHealResourcePath;
            return string.Empty;
        }

        public static float DurationSecondsFor(string profileId)
        {
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal)) return LightHitDurationSeconds;
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return LightHealDurationSeconds;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return ArrowStandardDurationSeconds;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.HiddenSpringSeed,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ArrowWhiteTrail,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowStandard,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowHeavy,
                    StringComparison.Ordinal)) return SeedDurationSeconds;
            if (string.Equals(profileId, BattleProjectileProfileIds.FirePot,
                    StringComparison.Ordinal)) return 0.66f;
            if (string.Equals(profileId, BattleProjectileProfileIds.ClothHeal,
                    StringComparison.Ordinal)) return 0.52f;
            return 0f;
        }

        public static float DisplayWidthFor(string profileId)
        {
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return BaseDisplayWidth * 2f;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return BaseDisplayWidth * 1.5f;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.HiddenSpringSeed,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ArrowWhiteTrail,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowStandard,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowHeavy,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ClothHeal,
                    StringComparison.Ordinal)) return BaseDisplayWidth * 1.5f;
            if (string.Equals(profileId, BattleProjectileProfileIds.FirePot,
                    StringComparison.Ordinal)) return BaseDisplayWidth * 1.5f;
            return BaseDisplayWidth * DefaultProjectileDisplayScale;
        }

        public static Rect FeedbackVisibleUvFor(string profileId)
        {
            if (string.Equals(profileId, BattleFeedbackProfileIds.HitFeedback,
                    StringComparison.Ordinal))
            {
                return new Rect(606f / 1536f, 370f / 1024f,
                    316f / 1536f, 310f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HitReaction,
                    StringComparison.Ordinal))
            {
                return new Rect(690f / 1536f, 400f / 1024f,
                    180f / 1536f, 224f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HostileHitFeedbackV2,
                    StringComparison.Ordinal))
            {
                return new Rect(215f / 1536f, 250f / 1024f,
                    1100f / 1536f, 494f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedback,
                    StringComparison.Ordinal))
            {
                return new Rect(506f / 1536f, 280f / 1024f,
                    480f / 1536f, 464f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRing,
                    StringComparison.Ordinal))
            {
                return new Rect(173f / 1536f, 393f / 1024f,
                    1191f / 1536f, 254f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRise,
                    StringComparison.Ordinal))
            {
                return new Rect(256f / 1214f, 77f / 1295f,
                    687f / 1214f, 1143f / 1295f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.C03WhiteMark,
                    StringComparison.Ordinal))
            {
                return new Rect(527f / 1536f, 205f / 1024f,
                    472f / 1536f, 554f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.C03MarkTransfer,
                    StringComparison.Ordinal))
            {
                return new Rect(444f / 1536f, 400f / 1024f,
                    750f / 1536f, 237f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.ShieldOverlayGeneric,
                    StringComparison.Ordinal))
            {
                return new Rect(182f / 1024f, 198f / 1536f,
                    661f / 1024f, 1121f / 1536f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.OpenDawn,
                    StringComparison.Ordinal))
            {
                return new Rect(38f / 1312f, 134f / 1199f,
                    1237f / 1312f, 930f / 1199f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.OlinTide,
                    StringComparison.Ordinal))
            {
                return new Rect(190f / 1536f, 332f / 1024f,
                    1157f / 1536f, 336f / 1024f);
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.RescueBeam,
                    StringComparison.Ordinal))
            {
                return new Rect(227f / 1774f, 343f / 887f,
                    1314f / 1774f, 181f / 887f);
            }
            return new Rect(0f, 0f, 1f, 1f);
        }

        public static Vector2 FeedbackVisiblePivotFor(string profileId)
        {
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedback,
                    StringComparison.Ordinal)) return new Vector2(0.5f, 0.68f);
            return new Vector2(0.5f, 0.5f);
        }

        public static float ReleaseProgressFor(string profileId)
        {
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal)) return LightHitReleaseProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return LightHealReleaseProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return ArrowStandardReleaseProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.HiddenSpringSeed,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ArrowWhiteTrail,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowStandard,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowHeavy,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.FirePot,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ClothHeal,
                    StringComparison.Ordinal)) return 0.45f;
            return -1f;
        }

        public static float ImpactProgressFor(string profileId)
        {
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal)) return LightHitImpactProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return LightHealImpactProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return ArrowStandardImpactProgress;
            if (string.Equals(profileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.HiddenSpringSeed,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ArrowWhiteTrail,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowStandard,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.CrossbowHeavy,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.FirePot,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.ClothHeal,
                    StringComparison.Ordinal)) return 0.90f;
            return -1f;
        }
    }

    /// <summary>
    /// Mutable view state copied from a combat snapshot. It deliberately contains no
    /// inventory or expedition objects, so presentation cannot mutate the run rules.
    /// </summary>
    internal sealed class BattleUnitPresentationState
    {
        public readonly string Id;
        public readonly string DefinitionId;
        public readonly bool IsPlayer;
        public readonly int Slot;
        public readonly Color BaseColor;
        public readonly int MaxHealth;
        public readonly int AttackDamage;
        public readonly int HealAmount;
        public readonly int Armor;
        public readonly CombatSpeedTier SpeedTier;
        public readonly int MaxShield;
        public readonly int RunLevel;
        public readonly Vector2 FormationFootPosition;
        public int CurrentHealth;
        public int CurrentShield;
        public Vector2 CurrentFootPosition;

        public BattleUnitPresentationState(
            string id,
            string definitionId,
            bool isPlayer,
            int slot,
            Color baseColor,
            int maxHealth,
            int attackDamage,
            int healAmount,
            int armor,
            CombatSpeedTier speedTier,
            int openingShield,
            Vector2 formationFootPosition,
            int runLevel = 1)
        {
            Id = id;
            DefinitionId = definitionId;
            IsPlayer = isPlayer;
            Slot = slot;
            BaseColor = baseColor;
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
            AttackDamage = attackDamage;
            HealAmount = healAmount;
            Armor = armor;
            SpeedTier = speedTier;
            MaxShield = Math.Max(0, openingShield);
            CurrentShield = MaxShield;
            RunLevel = Math.Max(1, Math.Min(RunUnitRules.MaximumRunLevel, runLevel));
            FormationFootPosition = formationFootPosition;
            CurrentFootPosition = formationFootPosition;
        }

        public bool IsAlive { get { return CurrentHealth > 0; } }
        public bool IsHealer { get { return HealAmount > 0; } }
        public float HealthPercent { get { return MaxHealth <= 0 ? 0f : (float)CurrentHealth / MaxHealth; } }
        public float ShieldPercent { get { return MaxShield <= 0 ? 0f : (float)CurrentShield / MaxShield; } }
    }

    /// <summary>
    /// Component boundary for the later SpriteRenderer/Animator implementation.
    /// A view receives projected state and stable cue IDs, never mutable run data.
    /// </summary>
    internal interface IBattleUnitView
    {
        string UnitId { get; }
        void Bind(BattleUnitPresentationState state);
        void SetFootPosition(Vector2 position);
        void PlayAnimation(string animationId);
        Vector2 GetAnchor(string anchorId);
        void Recycle();
    }

    internal interface IBattleEffectHandle
    {
        bool IsActive { get; }
        string EffectId { get; }
        BattleEventType Type { get; }
        int Amount { get; }
        Vector2 OriginPosition { get; }
        Vector2 TargetPosition { get; }
        float Progress { get; }
    }

    /// <summary>
    /// Component boundary for pooled projectiles, impacts, healing and floating text.
    /// </summary>
    internal interface IBattleEffectPool
    {
        IBattleEffectHandle Play(BattlePresentationCue cue, IBattleUnitView actor, IBattleUnitView target);
        void SetProgress(IBattleEffectHandle handle, float progress);
        void Recycle(IBattleEffectHandle handle);
        void RecycleAll();
    }

    /// <summary>
    /// First replaceable unit-view driver. The prototype IMGUI renderer reads this now;
    /// a later GameObject view can implement the same interface with an Animator.
    /// </summary>
    internal sealed class BattleUnitViewDriver : IBattleUnitView
    {
        public BattleUnitPresentationState State { get; private set; }
        public string CurrentAnimationId { get; private set; }
        public bool IsVisible { get; private set; }
        public float Opacity { get; private set; }
        public float RotationDegrees { get; private set; }
        public float Scale { get; private set; }

        public string UnitId { get { return State == null ? string.Empty : State.Id; } }

        public void Bind(BattleUnitPresentationState state)
        {
            State = state;
            CurrentAnimationId = BattlePresentationIds.Idle;
            IsVisible = state != null;
            Opacity = IsVisible ? 1f : 0f;
            RotationDegrees = 0f;
            Scale = 1f;
            if (state != null) SetFootPosition(state.FormationFootPosition);
        }

        public void SetFootPosition(Vector2 position)
        {
            if (State != null) State.CurrentFootPosition = position;
        }

        public void PlayAnimation(string animationId)
        {
            if (!string.IsNullOrEmpty(animationId)) CurrentAnimationId = animationId;
        }

        public Vector2 GetAnchor(string anchorId)
        {
            if (State == null) return Vector2.zero;
            var foot = State.CurrentFootPosition;
            if (anchorId == BattlePresentationIds.FeetAnchor) return foot;
            if (anchorId == BattlePresentationIds.HealthBarAnchor) return foot + new Vector2(0f, -150f);
            if (anchorId == BattlePresentationIds.WeaponAnchor)
            {
                return foot + new Vector2(State.IsPlayer ? 44f : -44f, -72f);
            }
            if (anchorId == BattlePresentationIds.SkillOriginAnchor) return foot + new Vector2(0f, -92f);
            if (anchorId == BattlePresentationIds.HitAnchor) return foot + new Vector2(0f, -72f);
            return foot + new Vector2(0f, -78f);
        }

        public void SetOpacity(float opacity)
        {
            Opacity = Mathf.Clamp01(opacity);
        }

        public void SetPose(float rotationDegrees, float scale)
        {
            RotationDegrees = rotationDegrees;
            Scale = Mathf.Max(0.01f, scale);
        }

        public void ApplyIdlePose(float timeSeconds)
        {
            if (State == null || !IsVisible) return;
            RestoreFormation();
        }

        public void ApplyActionPose(BattlePresentationCue cue, BattleUnitViewDriver target)
        {
            if (State == null || cue == null || target == null) return;
            if (FormalBattleAnimationContract.SupportsDefinition(State.DefinitionId))
            {
                RestoreFormation();
                return;
            }
            if (cue.Type == BattleEventType.Heal)
            {
                var pulse = Mathf.Sin(cue.Progress * Mathf.PI);
                SetFootPosition(State.FormationFootPosition + new Vector2(0f, -7f * pulse));
                SetPose(0f, 1f + pulse * 0.045f);
                return;
            }

            if (cue.Type != BattleEventType.Attack) return;
            var delta = target.State.FormationFootPosition - State.FormationFootPosition;
            var direction = delta.sqrMagnitude > 0.001f ? delta.normalized : Vector2.up;
            float travel;
            if (cue.Progress < 0.25f)
            {
                travel = Mathf.Lerp(0f, -0.04f, cue.Progress / 0.25f);
            }
            else if (cue.Progress < 0.50f)
            {
                travel = Mathf.Lerp(-0.04f, 0.18f, (cue.Progress - 0.25f) / 0.25f);
            }
            else
            {
                travel = Mathf.Lerp(0.18f, 0f, (cue.Progress - 0.50f) / 0.50f);
            }
            SetFootPosition(State.FormationFootPosition + delta * travel);
            var leanDirection = Mathf.Abs(direction.x) > 0.08f ? Mathf.Sign(direction.x) : (State.IsPlayer ? 1f : -1f);
            SetPose(leanDirection * Mathf.Sin(cue.Progress * Mathf.PI) * 3f, 1f);
        }

        public void ApplyTargetPose(BattlePresentationCue cue, BattleUnitViewDriver actor)
        {
            if (State == null || cue == null) return;
            if (FormalBattleAnimationContract.SupportsDefinition(State.DefinitionId))
            {
                RestoreFormation();
                return;
            }
            if (cue.Type == BattleEventType.Heal)
            {
                var pulse = Mathf.Sin(cue.Progress * Mathf.PI * 2f);
                SetPose(0f, 1f + Mathf.Max(0f, pulse) * 0.035f);
                return;
            }
            if (cue.Type != BattleEventType.Attack || actor == null || cue.Progress < cue.ImpactProgress) return;
            var hitProgress = Mathf.InverseLerp(cue.ImpactProgress, 1f, cue.Progress);
            var recoil = Mathf.Sin(hitProgress * Mathf.PI);
            var away = State.FormationFootPosition - actor.State.FormationFootPosition;
            var direction = away.sqrMagnitude > 0.001f ? away.normalized : Vector2.down;
            SetFootPosition(State.FormationFootPosition + direction * (8f * recoil));
            var leanDirection = Mathf.Abs(direction.x) > 0.08f ? Mathf.Sign(direction.x) : (State.IsPlayer ? -1f : 1f);
            SetPose(leanDirection * recoil * 2.5f, 1f - recoil * 0.018f);
        }

        public void RestoreFormation()
        {
            if (State != null) SetFootPosition(State.FormationFootPosition);
            SetPose(0f, 1f);
        }

        public void Recycle()
        {
            IsVisible = false;
            Opacity = 0f;
            RotationDegrees = 0f;
            Scale = 1f;
            CurrentAnimationId = BattlePresentationIds.Defeat;
        }
    }

    internal sealed class BattleEffectInstance : IBattleEffectHandle
    {
        private BattlePresentationCue cue;
        private IBattleUnitView actor;
        private IBattleUnitView target;

        public bool IsActive { get; private set; }
        public string EffectId { get { return cue == null ? string.Empty : cue.EffectId; } }
        public string ProjectileProfileId
        {
            get { return cue == null ? string.Empty : cue.ProjectileProfileId; }
        }
        public BattleEventType Type { get { return cue == null ? BattleEventType.Attack : cue.Type; } }
        public int Amount { get { return cue == null ? 0 : cue.Amount; } }
        public Vector2 OriginPosition { get; private set; }
        public Vector2 TargetPosition { get; private set; }
        public float Progress { get; private set; }
        public float ReleaseProgress { get { return cue == null ? -1f : cue.ReleaseProgress; } }
        public float ImpactProgress { get { return cue == null ? -1f : cue.ImpactProgress; } }

        internal void Activate(BattlePresentationCue battleCue, IBattleUnitView actorView, IBattleUnitView targetView)
        {
            cue = battleCue;
            actor = actorView;
            target = targetView;
            IsActive = true;
            SetProgress(0f);
        }

        internal void SetProgress(float progress)
        {
            Progress = Mathf.Clamp01(progress);
            if (cue == null) return;
            if (actor != null) OriginPosition = actor.GetAnchor(cue.OriginAnchorId);
            if (target != null) TargetPosition = target.GetAnchor(cue.TargetAnchorId);
        }

        internal void Deactivate()
        {
            IsActive = false;
            cue = null;
            actor = null;
            target = null;
            Progress = 0f;
        }
    }

    /// <summary>
    /// Small reusable pool for the sequential prototype battle. The contract supports
    /// concurrent effects later without allocating one object for every combat event.
    /// </summary>
    internal sealed class PooledBattleEffectPool : IBattleEffectPool
    {
        private readonly List<BattleEffectInstance> instances = new List<BattleEffectInstance>();

        public IReadOnlyList<BattleEffectInstance> Instances { get { return instances; } }

        public IBattleEffectHandle Play(
            BattlePresentationCue cue, IBattleUnitView actor, IBattleUnitView target)
        {
            BattleEffectInstance instance = null;
            foreach (var candidate in instances)
            {
                if (!candidate.IsActive)
                {
                    instance = candidate;
                    break;
                }
            }
            if (instance == null)
            {
                instance = new BattleEffectInstance();
                instances.Add(instance);
            }
            instance.Activate(cue, actor, target);
            return instance;
        }

        public void SetProgress(IBattleEffectHandle handle, float progress)
        {
            var instance = handle as BattleEffectInstance;
            if (instance != null && instance.IsActive) instance.SetProgress(progress);
        }

        public void Recycle(IBattleEffectHandle handle)
        {
            var instance = handle as BattleEffectInstance;
            if (instance != null) instance.Deactivate();
        }

        public void RecycleAll()
        {
            foreach (var instance in instances) instance.Deactivate();
        }
    }

    /// <summary>
    /// One deterministic combat event translated into presentation-only animation,
    /// effect and anchor IDs. Timings may change without changing the battle result.
    /// </summary>
    internal sealed class BattlePresentationCue
    {
        public readonly BattleEventType Type;
        public readonly string ActorId;
        public readonly string TargetId;
        public readonly int Amount;
        public readonly string ActorAnimationId;
        public readonly string TargetAnimationId;
        public readonly string EffectId;
        public readonly string ProjectileProfileId;
        public readonly string OriginAnchorId;
        public readonly string TargetAnchorId;
        public readonly float DurationSeconds;
        public readonly float ReleaseProgress;
        public readonly float ImpactProgress;
        public readonly bool IsSecondary;
        public readonly string SourceEffectId;
        public readonly bool DrivesActorAction;
        public float Progress;
        public int ElapsedMilliseconds
        {
            get { return Mathf.RoundToInt(Progress * DurationSeconds * 1000f); }
        }

        private BattlePresentationCue(
            BattleEvent battleEvent,
            string actorAnimationId,
            string targetAnimationId,
            string effectId,
            string projectileProfileId,
            string originAnchorId,
            string targetAnchorId,
            float durationSeconds,
            float releaseProgress,
            float impactProgress,
            bool drivesActorAction)
        {
            Type = battleEvent.Type;
            ActorId = battleEvent.ActorId;
            TargetId = battleEvent.TargetId;
            Amount = battleEvent.Amount;
            ActorAnimationId = actorAnimationId;
            TargetAnimationId = targetAnimationId;
            EffectId = effectId;
            ProjectileProfileId = projectileProfileId;
            OriginAnchorId = originAnchorId;
            TargetAnchorId = targetAnchorId;
            DurationSeconds = durationSeconds;
            ReleaseProgress = releaseProgress;
            ImpactProgress = impactProgress;
            IsSecondary = battleEvent.IsSecondary;
            SourceEffectId = battleEvent.SourceEffectId;
            DrivesActorAction = drivesActorAction;
        }

        public bool HasTravelEffect
        {
            get { return Type != BattleEventType.Defeat && !string.IsNullOrEmpty(ProjectileProfileId); }
        }

        private static string ProjectileProfileFor(
            BattleEventType type, string actorDefinitionId, bool isSecondary)
        {
            if (isSecondary) return string.Empty;
            if (type == BattleEventType.Heal)
            {
                if (string.Equals(actorDefinitionId, "enemy.n5.support", StringComparison.Ordinal))
                    return BattleProjectileProfileIds.ClothHeal;
                return string.Equals(actorDefinitionId, "unit.archer", StringComparison.Ordinal)
                    ? BattleProjectileProfileIds.SeedHeal
                    : BattleProjectileProfileIds.LightHeal;
            }
            if (type != BattleEventType.Attack) return string.Empty;
            if (string.Equals(actorDefinitionId, "unit.starpriest", StringComparison.Ordinal))
                return BattleProjectileProfileIds.FirePot;
            if (string.Equals(actorDefinitionId, "enemy.n3.ranged", StringComparison.Ordinal))
                return BattleProjectileProfileIds.CrossbowStandard;
            if (string.Equals(actorDefinitionId, "unit.thornGuard", StringComparison.Ordinal))
                return BattleProjectileProfileIds.ArrowStandard;
            if (string.Equals(actorDefinitionId, "unit.archer", StringComparison.Ordinal))
                return BattleProjectileProfileIds.SeedAttack;
            if (string.Equals(actorDefinitionId, "unit.healer", StringComparison.Ordinal))
                return BattleProjectileProfileIds.LightHit;
            return string.Empty;
        }

        public static BattlePresentationCue FromBattleEvent(
            BattleEvent battleEvent, string actorDefinitionId, string targetDefinitionId)
        {
            switch (battleEvent.Type)
            {
                case BattleEventType.Heal:
                {
                    var projectileProfileId = ProjectileProfileFor(
                        BattleEventType.Heal, actorDefinitionId, battleEvent.IsSecondary);
                    var usesProjectile = !string.IsNullOrEmpty(projectileProfileId);
                    return new BattlePresentationCue(
                        battleEvent,
                        battleEvent.IsSecondary ? BattlePresentationIds.Idle : BattlePresentationIds.Heal,
                        BattlePresentationIds.ReceiveHeal,
                        BattlePresentationIds.HealEffect,
                        projectileProfileId,
                        BattlePresentationIds.SkillOriginAnchor,
                        BattlePresentationIds.BodyAnchor,
                        usesProjectile
                            ? BattleProjectileProfiles.DurationSecondsFor(projectileProfileId)
                            : 0.44f,
                        battleEvent.IsSecondary
                            ? -1f
                            : usesProjectile
                                ? BattleProjectileProfiles.ReleaseProgressFor(projectileProfileId)
                                : 0.50f,
                        usesProjectile
                            ? BattleProjectileProfiles.ImpactProgressFor(projectileProfileId)
                            : 0.55f,
                        !battleEvent.IsSecondary);
                }
                case BattleEventType.Defeat:
                    var formalDefeat = FormalBattleAnimationContract.SupportsDefinition(targetDefinitionId);
                    return new BattlePresentationCue(
                        battleEvent,
                        BattlePresentationIds.Idle,
                        BattlePresentationIds.Defeat,
                        BattlePresentationIds.DefeatEffect,
                        string.Empty,
                        BattlePresentationIds.BodyAnchor,
                        BattlePresentationIds.FeetAnchor,
                        formalDefeat
                            ? (FormalBattleAnimationContract.GetClip(FormalBattleAnimationIds.Defeat).DurationMs +
                               FormalBattleAnimationContract.DefeatFinalHoldMs) / 1000f
                            : 0.28f,
                        -1f,
                        formalDefeat ? 0.65f : 0.55f,
                        false);
                default:
                    if (!battleEvent.IsSecondary &&
                        FormalBattleAnimationContract.SupportsDefinition(actorDefinitionId))
                    {
                        var clipId = string.Equals(battleEvent.SourceEffectId,
                            FormalBattleAnimationContract.GateStandSourceEffectId, StringComparison.Ordinal)
                            ? FormalBattleAnimationIds.VariantGateStand
                            : FormalBattleAnimationIds.BasicGateShieldStrike;
                        var clip = FormalBattleAnimationContract.GetClip(clipId);
                        var formalProjectileProfileId = ProjectileProfileFor(BattleEventType.Attack,
                            actorDefinitionId, battleEvent.IsSecondary);
                        var formalUsesProjectile = !string.IsNullOrEmpty(formalProjectileProfileId);
                        return new BattlePresentationCue(
                            battleEvent,
                            BattlePresentationIds.Attack,
                            BattlePresentationIds.Hit,
                            BattlePresentationIds.AttackEffect,
                            formalProjectileProfileId,
                            BattlePresentationIds.WeaponAnchor,
                            BattlePresentationIds.HitAnchor,
                            formalUsesProjectile
                                ? BattleProjectileProfiles.DurationSecondsFor(formalProjectileProfileId)
                                : clip.DurationMs / 1000f,
                            formalUsesProjectile
                                ? BattleProjectileProfiles.ReleaseProgressFor(formalProjectileProfileId)
                                : clip.ReleaseAtMs / (float)clip.DurationMs,
                            formalUsesProjectile
                                ? BattleProjectileProfiles.ImpactProgressFor(formalProjectileProfileId)
                                : clip.ImpactAtMs / (float)clip.DurationMs,
                            true);
                    }
                    var attackProjectileProfileId = ProjectileProfileFor(BattleEventType.Attack,
                        actorDefinitionId, battleEvent.IsSecondary);
                    var attackUsesProjectile = !string.IsNullOrEmpty(attackProjectileProfileId);
                    return new BattlePresentationCue(
                        battleEvent,
                        battleEvent.IsSecondary ? BattlePresentationIds.Idle : BattlePresentationIds.Attack,
                        BattlePresentationIds.Hit,
                        BattlePresentationIds.AttackEffect,
                        attackProjectileProfileId,
                        BattlePresentationIds.WeaponAnchor,
                        BattlePresentationIds.HitAnchor,
                        attackUsesProjectile
                            ? BattleProjectileProfiles.DurationSecondsFor(attackProjectileProfileId)
                            : 0.32f,
                        battleEvent.IsSecondary
                            ? -1f
                            : attackUsesProjectile
                                ? BattleProjectileProfiles.ReleaseProgressFor(attackProjectileProfileId)
                                : 0.40f,
                        attackUsesProjectile
                            ? BattleProjectileProfiles.ImpactProgressFor(attackProjectileProfileId)
                            : 0.50f,
                        !battleEvent.IsSecondary);
            }
        }
    }

    /// <summary>
    /// Presentation-side battle session. It mirrors health changes for rendering but
    /// never runs combat rules; all mutations are projections of BattleEvent data.
    /// </summary>
    internal sealed class BattlePresentationModel
    {
        private readonly List<BattleUnitPresentationState> playerUnits =
            new List<BattleUnitPresentationState>();
        private readonly List<BattleUnitPresentationState> enemyUnits =
            new List<BattleUnitPresentationState>();
        private readonly Dictionary<string, BattleUnitPresentationState> unitsById =
            new Dictionary<string, BattleUnitPresentationState>(StringComparer.Ordinal);
        private readonly Dictionary<string, BattleUnitViewDriver> viewsById =
            new Dictionary<string, BattleUnitViewDriver>(StringComparer.Ordinal);
        private readonly PooledBattleEffectPool effectPool = new PooledBattleEffectPool();
        private IBattleEffectHandle activeEffect;

        public IReadOnlyList<BattleUnitPresentationState> PlayerUnits { get { return playerUnits; } }
        public IReadOnlyList<BattleUnitPresentationState> EnemyUnits { get { return enemyUnits; } }
        public BattlePresentationCue ActiveCue { get; private set; }
        public IReadOnlyList<BattleEffectInstance> Effects { get { return effectPool.Instances; } }

        public void BeginBattle(
            IEnumerable<BattleUnitPresentationState> players,
            IEnumerable<BattleUnitPresentationState> enemies)
        {
            Reset();
            AddUnits(players, playerUnits);
            AddUnits(enemies, enemyUnits);
        }

        public BattleUnitPresentationState FindUnit(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            BattleUnitPresentationState unit;
            return unitsById.TryGetValue(id, out unit) ? unit : null;
        }

        public BattleUnitPresentationState FindUnit(bool isPlayer, int slot)
        {
            var units = isPlayer ? playerUnits : enemyUnits;
            foreach (var unit in units)
            {
                if (unit.Slot == slot) return unit;
            }
            return null;
        }

        public BattleUnitViewDriver FindView(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            BattleUnitViewDriver view;
            return viewsById.TryGetValue(id, out view) ? view : null;
        }

        public BattlePresentationCue BeginCue(BattleEvent battleEvent)
        {
            var actorState = FindUnit(battleEvent.ActorId);
            var targetState = FindUnit(battleEvent.TargetId);
            ActiveCue = BattlePresentationCue.FromBattleEvent(
                battleEvent,
                actorState == null ? null : actorState.DefinitionId,
                targetState == null ? null : targetState.DefinitionId);
            var actor = FindView(ActiveCue.ActorId);
            var target = FindView(ActiveCue.TargetId);
            if (actor != null)
            {
                actor.RestoreFormation();
                actor.PlayAnimation(ActiveCue.ActorAnimationId);
            }
            if (target != null)
            {
                target.RestoreFormation();
                target.PlayAnimation(ActiveCue.TargetAnimationId);
            }
            activeEffect = effectPool.Play(ActiveCue, actor, target);
            return ActiveCue;
        }

        public void SetCueProgress(float progress)
        {
            if (ActiveCue == null) return;
            ActiveCue.Progress = Mathf.Clamp01(progress);
            var actor = FindView(ActiveCue.ActorId);
            var target = FindView(ActiveCue.TargetId);
            if (actor != null && target != null && ActiveCue.Type != BattleEventType.Defeat &&
                ActiveCue.DrivesActorAction)
                actor.ApplyActionPose(ActiveCue, target);
            if (target != null && ActiveCue.Type != BattleEventType.Defeat)
                target.ApplyTargetPose(ActiveCue, actor);
            if (target != null && ActiveCue.Type == BattleEventType.Defeat)
            {
                if (FormalBattleAnimationContract.SupportsDefinition(target.State.DefinitionId))
                {
                    target.RestoreFormation();
                    target.SetOpacity(1f);
                }
                else
                {
                    target.SetOpacity(1f - ActiveCue.Progress);
                    target.SetFootPosition(target.State.FormationFootPosition +
                        new Vector2(0f, -34f * ActiveCue.Progress));
                }
            }
            effectPool.SetProgress(activeEffect, ActiveCue.Progress);
        }

        public void UpdateIdlePoses(float timeSeconds)
        {
            foreach (var pair in viewsById)
            {
                var view = pair.Value;
                if (!view.IsVisible) continue;
                if (ActiveCue != null &&
                    (ActiveCue.ActorId == view.UnitId || ActiveCue.TargetId == view.UnitId)) continue;
                view.ApplyIdlePose(timeSeconds);
            }
        }

        public void Apply(BattleEvent battleEvent)
        {
            var target = FindUnit(battleEvent.TargetId);
            if (target != null)
            {
                target.CurrentHealth = battleEvent.TargetHealthAfter;
                target.CurrentShield = battleEvent.TargetShieldAfter;
            }
        }

        public void SetUnitFootPosition(string id, Vector2 position)
        {
            var unit = FindUnit(id);
            if (unit != null) unit.CurrentFootPosition = position;
        }

        public void ResetUnitPositions()
        {
            foreach (var pair in unitsById)
            {
                pair.Value.CurrentFootPosition = pair.Value.FormationFootPosition;
            }
        }

        public void EndCue()
        {
            if (ActiveCue != null)
            {
                var actor = FindView(ActiveCue.ActorId);
                var target = FindView(ActiveCue.TargetId);
                if (actor != null)
                {
                    actor.RestoreFormation();
                    if (actor.IsVisible) actor.PlayAnimation(BattlePresentationIds.Idle);
                }
                if (target != null)
                {
                    target.RestoreFormation();
                    if (ActiveCue.Type == BattleEventType.Defeat && !target.State.IsAlive)
                    {
                        target.Recycle();
                    }
                    else if (target.IsVisible && target.State.IsAlive)
                    {
                        target.SetOpacity(1f);
                        target.PlayAnimation(BattlePresentationIds.Idle);
                    }
                }
            }
            effectPool.Recycle(activeEffect);
            activeEffect = null;
            ActiveCue = null;
        }

        public void Reset()
        {
            foreach (var pair in viewsById) pair.Value.Recycle();
            effectPool.RecycleAll();
            activeEffect = null;
            ActiveCue = null;
            playerUnits.Clear();
            enemyUnits.Clear();
            unitsById.Clear();
            viewsById.Clear();
        }

        private void AddUnits(
            IEnumerable<BattleUnitPresentationState> source,
            ICollection<BattleUnitPresentationState> destination)
        {
            if (source == null) return;
            foreach (var unit in source)
            {
                destination.Add(unit);
                unitsById.Add(unit.Id, unit);
                var view = new BattleUnitViewDriver();
                view.Bind(unit);
                viewsById.Add(unit.Id, view);
            }
        }
    }
}
