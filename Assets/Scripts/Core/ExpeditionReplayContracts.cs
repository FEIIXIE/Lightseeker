using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class ExpeditionReplayActionRecord
    {
        public int Sequence;
        public string ActionId;
        public string Kind;
        public int ExpectedStateVersion;
        public string NodeId;
        public string PayloadCanonical;
        public string PayloadSha256;
    }

    [Serializable]
    public sealed class ExpeditionReplayEnvelopeRecord
    {
        public string ContractVersion;
        public string RunId;
        public string AccountId;
        public string CombatInputContractVersion;
        public string CombatInputHash;
        public string SeedCommitment;
        public ExpeditionReplayActionRecord[] Actions;
        public string TranscriptSha256;
    }

    public static class ExpeditionReplayClientContract
    {
        public const string Version = "chapter01.expedition-replay.v2";
        public const string PayloadVersion = "chapter01.replay-action.v1";
        public const string CombatInputVersion = "chapter01.combat-input.v1";
        public const int MaximumActionCount = 512;
        public const int MaximumPayloadUtf8Bytes = 65536;
        public const int MaximumTranscriptUtf8Bytes = 2097152;

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly HashSet<string> KnownActionKinds =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "route.select",
                "shop.refresh",
                "shop.buy",
                "merchant.buy",
                "roster.sell",
                "roster.move",
                "roster.merge",
                "item.bind",
                "item.discard",
                "node.choice",
                "upgrade.select",
                "battle.resolve"
            };

        private static readonly Dictionary<string, string[]> PayloadFields =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "route.select", new[] { "destinationNodeId" } },
                { "shop.refresh", new[] { "refreshId" } },
                { "shop.buy", new[] { "offerId", "runUnitId" } },
                { "merchant.buy", new[] { "offerId", "itemInstanceId" } },
                { "roster.sell", new[] { "runUnitId" } },
                { "roster.move", new[] { "runUnitId", "destinationSlotId" } },
                { "roster.merge", new[]
                    { "sourceUnitIdA", "sourceUnitIdB", "sourceUnitIdC", "resultUnitId" } },
                { "item.bind", new[] { "itemInstanceId", "runUnitId", "slotId" } },
                { "item.discard", new[] { "itemInstanceId" } },
                { "node.choice", new[] { "choiceId" } },
                { "upgrade.select", new[] { "upgradeId" } },
                { "battle.resolve", new[]
                    { "encounterId", "battleSeed", "battleInputSha256",
                        "battleEventSha256", "winner" } }
            };

        public static string BuildPayload(
            string kind,
            IDictionary<string, string> fields)
        {
            string[] expected;
            if (!PayloadFields.TryGetValue(kind, out expected) || fields == null ||
                fields.Count != expected.Length)
                throw new InvalidOperationException(
                    "The replay action payload schema is invalid.");
            var builder = new StringBuilder(PayloadVersion);
            for (var index = 0; index < expected.Length; index++)
            {
                string value;
                if (!fields.TryGetValue(expected[index], out value) || value == null)
                    throw new InvalidOperationException(
                        "The replay action payload is missing a required field.");
                builder.Append('|').Append(expected[index]).Append(':')
                    .Append(StrictUtf8.GetByteCount(value)).Append(':').Append(value);
            }
            var payload = builder.ToString();
            ValidatePayload(kind, payload);
            return payload;
        }

        public static ExpeditionReplayActionRecord CreateAction(
            int sequence,
            string actionId,
            string kind,
            int expectedStateVersion,
            string nodeId,
            string payloadCanonical)
        {
            ValidatePayload(kind, payloadCanonical);
            return new ExpeditionReplayActionRecord
            {
                Sequence = sequence,
                ActionId = actionId,
                Kind = kind,
                ExpectedStateVersion = expectedStateVersion,
                NodeId = nodeId,
                PayloadCanonical = payloadCanonical,
                PayloadSha256 = ComputePayloadHash(payloadCanonical)
            };
        }

        public static ExpeditionReplayEnvelopeRecord CreateEnvelope(
            string runId,
            string accountId,
            string combatInputHash,
            string seedCommitment,
            IList<ExpeditionReplayActionRecord> actions)
        {
            Guid parsedRunId;
            Guid parsedAccountId;
            if (!Guid.TryParse(runId, out parsedRunId) ||
                !Guid.TryParse(accountId, out parsedAccountId))
                throw new InvalidOperationException(
                    "An authoritative expedition run and account are required.");

            var copiedActions = actions == null
                ? null
                : new List<ExpeditionReplayActionRecord>(actions).ToArray();
            var envelope = new ExpeditionReplayEnvelopeRecord
            {
                ContractVersion = Version,
                // ASP.NET's HTTP JSON Guid binder accepts the canonical dashed form.
                // Transcript hashing still normalizes both values to "N" below, so the
                // wire-format correction does not change the replay digest contract.
                RunId = parsedRunId.ToString("D"),
                AccountId = parsedAccountId.ToString("D"),
                CombatInputContractVersion = CombatInputVersion,
                CombatInputHash = combatInputHash,
                SeedCommitment = seedCommitment,
                Actions = copiedActions,
                TranscriptSha256 = string.Empty
            };
            ValidateActions(envelope);
            envelope.TranscriptSha256 = ComputeTranscriptHash(envelope);
            return envelope;
        }

        public static string ComputePayloadHash(string payloadCanonical)
        {
            return ComputeSha256Hex(EncodeRequired(payloadCanonical));
        }

        public static string ComputeTranscriptHash(ExpeditionReplayEnvelopeRecord envelope)
        {
            if (envelope == null) throw new ArgumentNullException("envelope");
            using (var stream = new MemoryStream())
            {
                WriteText(stream, Version);
                WriteText(stream, NormalizeGuid(envelope.RunId));
                WriteText(stream, NormalizeGuid(envelope.AccountId));
                WriteText(stream, envelope.CombatInputContractVersion);
                WriteText(stream, envelope.CombatInputHash);
                WriteText(stream, envelope.SeedCommitment);
                var actions = envelope.Actions;
                if (actions == null)
                    throw new InvalidOperationException("The replay action list is required.");
                WriteInt32(stream, actions.Length);
                for (var index = 0; index < actions.Length; index++)
                {
                    var action = actions[index];
                    if (action == null)
                        throw new InvalidOperationException(
                            "The replay action list contains a null action.");
                    WriteInt32(stream, action.Sequence);
                    WriteText(stream, action.ActionId);
                    WriteText(stream, action.Kind);
                    WriteInt32(stream, action.ExpectedStateVersion);
                    WriteText(stream, action.NodeId);
                    WriteText(stream, action.PayloadCanonical);
                    WriteText(stream, action.PayloadSha256);
                }
                return ComputeSha256Hex(stream.ToArray());
            }
        }

        public static void Validate(ExpeditionReplayEnvelopeRecord envelope)
        {
            if (envelope == null || envelope.ContractVersion != Version ||
                envelope.CombatInputContractVersion != CombatInputVersion ||
                !IsSha256Hex(envelope.CombatInputHash) ||
                !IsSha256Hex(envelope.SeedCommitment))
                throw new InvalidOperationException("The expedition replay envelope is invalid.");
            NormalizeGuid(envelope.RunId);
            NormalizeGuid(envelope.AccountId);
            ValidateActions(envelope);
            var expected = ComputeTranscriptHash(envelope);
            if (!IsSha256Hex(envelope.TranscriptSha256) ||
                !string.Equals(expected, envelope.TranscriptSha256,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The expedition replay transcript hash does not match its actions.");
        }

        private static void ValidateActions(ExpeditionReplayEnvelopeRecord envelope)
        {
            if (!IsSha256Hex(envelope.CombatInputHash) ||
                !IsSha256Hex(envelope.SeedCommitment))
                throw new InvalidOperationException(
                    "The expedition replay run binding is invalid.");
            var actions = envelope.Actions;
            if (actions == null || actions.Length < 1 ||
                actions.Length > MaximumActionCount)
                throw new InvalidOperationException("The replay action count is invalid.");

            var actionIds = new HashSet<string>(StringComparer.Ordinal);
            var previousStateVersion = -1;
            var totalPayloadBytes = 0;
            for (var index = 0; index < actions.Length; index++)
            {
                var action = actions[index];
                int ignoredNodeOrdinal;
                if (action == null || action.Sequence != index ||
                    !IsIdentifier(action.ActionId, 64) ||
                    !actionIds.Add(action.ActionId) ||
                    !KnownActionKinds.Contains(action.Kind) ||
                    action.ExpectedStateVersion < previousStateVersion ||
                    action.ExpectedStateVersion < 0 ||
                    !TryGetNodeOrdinal(action.NodeId, out ignoredNodeOrdinal) ||
                    !IsSha256Hex(action.PayloadSha256))
                    throw new InvalidOperationException(
                        "The replay action ordering or metadata is invalid.");

                var payload = EncodeRequired(action.PayloadCanonical);
                checked { totalPayloadBytes += payload.Length; }
                if (payload.Length > MaximumPayloadUtf8Bytes ||
                    totalPayloadBytes > MaximumTranscriptUtf8Bytes ||
                    !string.Equals(ComputeSha256Hex(payload), action.PayloadSha256,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "A replay action payload is invalid or has been altered.");
                ValidatePayload(action.Kind, action.PayloadCanonical);
                previousStateVersion = action.ExpectedStateVersion;
            }

            if (!string.Equals(actions[actions.Length - 1].Kind,
                    "battle.resolve", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "A replay checkpoint must end with a battle resolution action.");
        }

        private static string NormalizeGuid(string value)
        {
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
                throw new InvalidOperationException("A replay run binding is not a valid GUID.");
            return parsed.ToString("N");
        }

        private static void ValidatePayload(string kind, string payloadCanonical)
        {
            string[] expected;
            if (!PayloadFields.TryGetValue(kind, out expected))
                throw new InvalidOperationException(
                    "The replay action kind has no payload schema.");
            var reader = new ReplayPayloadReader(payloadCanonical);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var index = 0; index < expected.Length; index++)
                values.Add(expected[index], reader.Read(expected[index]));
            reader.RequireEnd();

            foreach (var pair in values)
            {
                uint ignoredSeed;
                int ignoredNode;
                if (pair.Key.EndsWith("Sha256", StringComparison.Ordinal))
                {
                    if (!IsSha256Hex(pair.Value)) InvalidPayloadValue();
                }
                else if (pair.Key == "battleSeed")
                {
                    if (!uint.TryParse(pair.Value, out ignoredSeed) ||
                        ignoredSeed.ToString() != pair.Value) InvalidPayloadValue();
                }
                else if (pair.Key == "winner")
                {
                    if (pair.Value != "player" && pair.Value != "enemy")
                        InvalidPayloadValue();
                }
                else if (pair.Key == "destinationNodeId")
                {
                    if (!TryGetNodeOrdinal(pair.Value, out ignoredNode))
                        InvalidPayloadValue();
                }
                else if (pair.Key == "encounterId")
                {
                    if (!IsEncounterId(pair.Value)) InvalidPayloadValue();
                }
                else if (pair.Key == "choiceId")
                {
                    if (!IsChoiceIdentifier(pair.Value, 96)) InvalidPayloadValue();
                }
                else if (!IsIdentifier(pair.Value, 96))
                {
                    InvalidPayloadValue();
                }
            }

            if (kind == "roster.merge")
            {
                var sources = new HashSet<string>(StringComparer.Ordinal)
                {
                    values["sourceUnitIdA"], values["sourceUnitIdB"],
                    values["sourceUnitIdC"]
                };
                if (sources.Count != 3)
                    throw new InvalidOperationException(
                        "A replay roster merge must name three distinct source units.");
            }
        }

        private static void InvalidPayloadValue()
        {
            throw new InvalidOperationException(
                "A replay action payload value is invalid.");
        }

        private sealed class ReplayPayloadReader
        {
            private readonly string payload;
            private int offset;

            public ReplayPayloadReader(string payload)
            {
                this.payload = payload;
                if (payload == null ||
                    !payload.StartsWith(PayloadVersion, StringComparison.Ordinal))
                    InvalidCanonicalPayload();
                offset = PayloadVersion.Length;
            }

            public string Read(string expectedKey)
            {
                if (offset >= payload.Length || payload[offset] != '|')
                    InvalidCanonicalPayload();
                offset++;
                var keyEnd = payload.IndexOf(':', offset);
                if (keyEnd < 0 ||
                    payload.Substring(offset, keyEnd - offset) != expectedKey)
                    InvalidCanonicalPayload();
                offset = keyEnd + 1;
                var lengthEnd = payload.IndexOf(':', offset);
                if (lengthEnd < 0 || lengthEnd == offset)
                    InvalidCanonicalPayload();
                var lengthText = payload.Substring(offset, lengthEnd - offset);
                var byteLength = -1;
                if ((lengthText.Length > 1 && lengthText[0] == '0') ||
                    !int.TryParse(lengthText, out byteLength) || byteLength < 0)
                    InvalidCanonicalPayload();
                offset = lengthEnd + 1;
                var valueStart = offset;
                var consumedBytes = 0;
                while (offset < payload.Length && consumedBytes < byteLength)
                {
                    var characterLength = char.IsHighSurrogate(payload[offset]) ? 2 : 1;
                    if (offset + characterLength > payload.Length)
                        InvalidCanonicalPayload();
                    consumedBytes += StrictUtf8.GetByteCount(
                        payload.Substring(offset, characterLength));
                    offset += characterLength;
                }
                if (consumedBytes != byteLength) InvalidCanonicalPayload();
                return payload.Substring(valueStart, offset - valueStart);
            }

            public void RequireEnd()
            {
                if (offset != payload.Length) InvalidCanonicalPayload();
            }

            private static void InvalidCanonicalPayload()
            {
                throw new InvalidOperationException(
                    "The replay action payload is not canonical.");
            }
        }

        private static byte[] EncodeRequired(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf('\0') >= 0)
                throw new InvalidOperationException(
                    "A replay transcript field is empty or contains a null character.");
            try { return StrictUtf8.GetBytes(value); }
            catch (EncoderFallbackException)
            {
                throw new InvalidOperationException(
                    "A replay transcript field contains invalid Unicode.");
            }
        }

        private static void WriteText(Stream stream, string value)
        {
            var bytes = EncodeRequired(value);
            WriteInt32(stream, bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void WriteInt32(Stream stream, int value)
        {
            var bytes = new[]
            {
                (byte)(value >> 24),
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)value
            };
            stream.Write(bytes, 0, bytes.Length);
        }

        private static string ComputeSha256Hex(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                var builder = new StringBuilder(hash.Length * 2);
                for (var index = 0; index < hash.Length; index++)
                    builder.Append(hash[index].ToString("x2"));
                return builder.ToString();
            }
        }

        private static bool IsSha256Hex(string value)
        {
            if (value == null || value.Length != 64) return false;
            for (var index = 0; index < value.Length; index++)
                if (!((value[index] >= '0' && value[index] <= '9') ||
                      (value[index] >= 'a' && value[index] <= 'f'))) return false;
            return true;
        }

        private static bool IsIdentifier(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length > maximumLength) return false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z') ||
                      character == '-' || character == '_' || character == '.')) return false;
            }
            return true;
        }

        private static bool IsChoiceIdentifier(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length > maximumLength ||
                value[0] == ':' || value[value.Length - 1] == ':') return false;
            var previousSeparator = false;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                if (character == ':')
                {
                    if (previousSeparator) return false;
                    previousSeparator = true;
                    continue;
                }
                previousSeparator = false;
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z') ||
                      character == '-' || character == '_' || character == '.')) return false;
            }
            return true;
        }

        private static bool TryGetNodeOrdinal(string value, out int ordinal)
        {
            ordinal = -1;
            if (value == null || value.Length != 3 || value[0] != 'M' ||
                value[1] < '0' || value[1] > '9' ||
                value[2] < '0' || value[2] > '9') return false;
            ordinal = (value[1] - '0') * 10 + value[2] - '0';
            return ordinal <= 29;
        }

        private static bool IsEncounterId(string value)
        {
            if (value == null || value.Length > 96) return false;
            var parts = value.Split(':');
            int generation;
            uint seed;
            int ignoredNode;
            return parts.Length == 3 &&
                   TryGetNodeOrdinal(parts[0], out ignoredNode) &&
                   int.TryParse(parts[1], out generation) && generation > 0 &&
                   generation.ToString() == parts[1] &&
                   uint.TryParse(parts[2], out seed) && seed.ToString() == parts[2];
        }
    }
}
