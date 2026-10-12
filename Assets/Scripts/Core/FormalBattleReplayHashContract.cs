using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    public static class FormalBattleReplayHashContract
    {
        public const string InputContractVersion = "chapter01.battle-input-sha256.v1";
        public const string EventContractVersion = "chapter01.battle-events-sha256.v1";

        private static readonly FieldInfo[] BattleUnitFields = CreateOrderedBattleUnitFields();

        public static string ComputeInputSha256(BattleInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var builder = new StringBuilder(InputContractVersion);
            Append(builder, "seed", input.Seed.ToString(CultureInfo.InvariantCulture));
            AppendUnits(builder, "player", input.PlayerUnits);
            AppendUnits(builder, "enemy", input.EnemyUnits);
            var effects = input.PlayerRoguelikeEffects;
            if (effects == null)
                throw new InvalidOperationException("Formal battle effects cannot be null.");
            Append(builder, "effectCount", effects.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var effect in effects)
            {
                if (effect == null)
                    throw new InvalidOperationException("Formal battle effects cannot contain null.");
                Append(builder, "effectId", effect.effectId);
                Append(builder, "rarityId", effect.rarityId.ToString(CultureInfo.InvariantCulture));
                Append(builder, "stackCount", effect.stackCount.ToString(CultureInfo.InvariantCulture));
                Append(builder, "sourceChoiceSetId", effect.sourceChoiceSetId);
                Append(builder, "acquiredAtRunStateVersion",
                    effect.acquiredAtRunStateVersion.ToString(CultureInfo.InvariantCulture));
            }
            Append(builder, "runtimeGeneration",
                input.PlayerRoguelikeRuntimeGeneration.ToString(CultureInfo.InvariantCulture));
            Append(builder, "keystoneId", input.PlayerKeystoneId);
            return Sha256Hex(builder.ToString());
        }

        public static string ComputeEventSha256(IReadOnlyList<BattleEvent> events)
        {
            if (events == null) throw new ArgumentNullException(nameof(events));
            var builder = new StringBuilder(EventContractVersion);
            Append(builder, "count", events.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var battleEvent in events)
            {
                if (battleEvent == null)
                    throw new InvalidOperationException("Formal battle events cannot contain null.");
                Append(builder, "type", ((int)battleEvent.Type).ToString(CultureInfo.InvariantCulture));
                Append(builder, "actorId", battleEvent.ActorId);
                Append(builder, "targetId", battleEvent.TargetId);
                Append(builder, "amount", battleEvent.Amount.ToString(CultureInfo.InvariantCulture));
                Append(builder, "targetHealthAfter", battleEvent.TargetHealthAfter.ToString(CultureInfo.InvariantCulture));
                Append(builder, "shieldAbsorbed", battleEvent.ShieldAbsorbed.ToString(CultureInfo.InvariantCulture));
                Append(builder, "targetShieldAfter", battleEvent.TargetShieldAfter.ToString(CultureInfo.InvariantCulture));
                Append(builder, "actionTime", battleEvent.ActionTime.ToString(CultureInfo.InvariantCulture));
                Append(builder, "childIndex", battleEvent.ChildIndex.ToString(CultureInfo.InvariantCulture));
                Append(builder, "secondary", battleEvent.IsSecondary ? "1" : "0");
                Append(builder, "sourceEffectId", battleEvent.SourceEffectId);
            }
            return Sha256Hex(builder.ToString());
        }

        private static void AppendUnits(StringBuilder builder, string side,
            IList<BattleUnitState> units)
        {
            if (units == null)
                throw new InvalidOperationException("Formal battle units cannot be null.");
            Append(builder, side + "Count", units.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var unit in units)
            {
                if (unit == null)
                    throw new InvalidOperationException("Formal battle units cannot contain null.");
                foreach (var field in BattleUnitFields)
                    Append(builder, side + "." + field.Name, ValueText(field.GetValue(unit)));
            }
        }

        private static FieldInfo[] CreateOrderedBattleUnitFields()
        {
            var fields = typeof(BattleUnitState).GetFields(BindingFlags.Instance |
                                                           BindingFlags.Public);
            Array.Sort(fields, (left, right) => string.CompareOrdinal(left.Name, right.Name));
            return fields;
        }

        private static string ValueText(object value)
        {
            if (value == null) return string.Empty;
            if (value is bool) return (bool)value ? "1" : "0";
            var type = value.GetType();
            if (type.IsEnum)
                return Convert.ToInt32(value, CultureInfo.InvariantCulture)
                    .ToString(CultureInfo.InvariantCulture);
            var formattable = value as IFormattable;
            return formattable == null
                ? value.ToString()
                : formattable.ToString(null, CultureInfo.InvariantCulture);
        }

        private static void Append(StringBuilder builder, string key, string value)
        {
            value = value ?? string.Empty;
            builder.Append('|').Append(key).Append(':')
                .Append(value.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(value);
        }

        private static string Sha256Hex(string canonical)
        {
            byte[] digest;
            using (var algorithm = SHA256.Create())
                digest = algorithm.ComputeHash(Encoding.UTF8.GetBytes(canonical));
            var builder = new StringBuilder(digest.Length * 2);
            foreach (var value in digest) builder.Append(value.ToString("x2"));
            return builder.ToString();
        }
    }
}
