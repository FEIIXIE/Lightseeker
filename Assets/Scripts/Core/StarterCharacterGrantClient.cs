using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class StarterCharacterGrantCharacter
    {
        public string CharacterInstanceId;
        public string CharacterId;
        public int Quality;
        public int AptitudeRoll;
        public int ExternalStar;
        public int SourceSlotIndex;
    }

    [Serializable]
    public sealed class StarterCharacterGrantSnapshot
    {
        public string GrantId;
        public string AccountId;
        public string SeasonId;
        public StarterCharacterGrantCharacter[] Characters = new StarterCharacterGrantCharacter[0];
        public bool IdempotentReplay;
        public string RequestId;
    }

    public interface IStarterCharacterGrantTransport
    {
        Task<StarterCharacterGrantSnapshot> ClaimAsync(
            string accessToken,
            string seasonId,
            string requestId,
            CancellationToken cancellationToken);
    }

    public static class StarterCharacterGrantCollectionRules
    {
        public static PlayerCollection Merge(
            CharacterCatalog catalog,
            PlayerCollection current,
            StarterCharacterGrantSnapshot grant,
            out string[] authoritativeDeckInstanceIds)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (grant == null || grant.Characters == null || grant.Characters.Length != 3)
                throw new InvalidOperationException("The starter character grant must contain exactly three characters.");

            Guid grantId;
            Guid accountId;
            if (!Guid.TryParse(grant.GrantId, out grantId) || grantId == Guid.Empty ||
                !Guid.TryParse(grant.AccountId, out accountId) || accountId == Guid.Empty ||
                string.IsNullOrWhiteSpace(grant.SeasonId))
                throw new InvalidOperationException("The starter character grant scope is invalid.");

            var orderedCharacters = new StarterCharacterGrantCharacter[grant.Characters.Length];
            Array.Copy(grant.Characters, orderedCharacters, grant.Characters.Length);
            Array.Sort(orderedCharacters, (left, right) =>
                (left == null ? int.MaxValue : left.SourceSlotIndex).CompareTo(
                    right == null ? int.MaxValue : right.SourceSlotIndex));
            var authoritativeInstanceIds = new HashSet<string>(StringComparer.Ordinal);
            var authoritativeDefinitionIds = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < orderedCharacters.Length; index++)
            {
                var character = orderedCharacters[index];
                Guid instanceId;
                if (character == null || character.SourceSlotIndex != index ||
                    !Guid.TryParse(character.CharacterInstanceId, out instanceId) ||
                    instanceId == Guid.Empty || string.IsNullOrWhiteSpace(character.CharacterId) ||
                    !catalog.Contains(character.CharacterId) || character.ExternalStar != 1 ||
                    character.Quality < 0 || character.Quality > 1 ||
                    character.AptitudeRoll < 0 ||
                    character.AptitudeRoll > CharacterRarityInstanceRules.RollScale ||
                    !authoritativeInstanceIds.Add(character.CharacterInstanceId) ||
                    !authoritativeDefinitionIds.Add(character.CharacterId))
                    throw new InvalidOperationException(
                        "The starter character grant contains an invalid character.");
            }

            var merged = new List<OwnedCharacterState>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (current != null)
            {
                foreach (var entry in current.Entries)
                {
                    if (entry == null || authoritativeInstanceIds.Contains(entry.CharacterInstanceId) ||
                        IsDevelopmentFixture(entry) || !seen.Add(entry.CharacterInstanceId)) continue;
                    merged.Add(entry);
                }
            }

            authoritativeDeckInstanceIds = new string[orderedCharacters.Length];
            var acquiredAt = PlayerCollection.NextAcquiredAtUtcTicks(
                merged, DateTime.UtcNow.Ticks);
            for (var index = 0; index < orderedCharacters.Length; index++)
            {
                var character = orderedCharacters[index];
                authoritativeDeckInstanceIds[index] = character.CharacterInstanceId;
                seen.Add(character.CharacterInstanceId);
                merged.Add(new OwnedCharacterState(
                    character.CharacterInstanceId,
                    character.CharacterId,
                    1,
                    character.ExternalStar,
                    MapQuality(character.Quality),
                    acquiredAt++,
                    character.AptitudeRoll));
            }
            return new PlayerCollection(catalog, merged);
        }

        private static bool IsDevelopmentFixture(OwnedCharacterState entry)
        {
            var legacyId = "character." + entry.DefinitionId;
            if (MatchesFixtureId(entry.CharacterInstanceId, legacyId)) return true;
            var formalId = PlayerCollection.FormalQualityInstanceId(
                entry.DefinitionId, entry.Quality);
            return MatchesFixtureId(entry.CharacterInstanceId, formalId);
        }

        private static bool MatchesFixtureId(string instanceId, string fixtureId)
        {
            return string.Equals(instanceId, fixtureId, StringComparison.Ordinal) ||
                   instanceId.StartsWith(fixtureId + ".copy.", StringComparison.Ordinal);
        }

        private static CharacterQuality MapQuality(int serverQuality)
        {
            switch (serverQuality)
            {
                case 0: return CharacterQuality.Common;
                case 1: return CharacterQuality.Uncommon;
                default:
                    throw new InvalidOperationException("The starter character grant quality is invalid.");
            }
        }
    }
}
