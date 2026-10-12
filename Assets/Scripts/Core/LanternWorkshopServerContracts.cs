using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public static class GuardTideCovenantRules
    {
        public const string ContractId = "chapter01.lantern.guard_tide.six_stage.v1.0";
        public const int MaximumStage = 6;
        private static readonly int[] Costs = { 10, 20, 30, 40, 60, 80 };
        private static readonly string[][] Options =
        {
            new[] { "root.health", "root.armor", "root.healing" },
            new[] { "trigger.start", "trigger.danger" },
            new[] { "scope.front", "scope.team", "scope.column" },
            new[] { "payoff.offense", "payoff.recovery", "payoff.reshield" },
            new[] { "route.wall", "route.break", "route.formation" },
            new[] { "capstone.sustain", "capstone.burst" }
        };

        public static int CostForStage(int stage)
        {
            if (stage < 1 || stage > MaximumStage) throw new ArgumentOutOfRangeException("stage");
            return Costs[stage - 1];
        }

        public static IReadOnlyList<string> OptionsForStage(int stage)
        {
            if (stage < 1 || stage > MaximumStage) throw new ArgumentOutOfRangeException("stage");
            return Options[stage - 1];
        }

        public static bool IsOptionForStage(int stage, string optionId)
        {
            if (stage < 1 || stage > MaximumStage || string.IsNullOrWhiteSpace(optionId))
                return false;
            return Array.IndexOf(Options[stage - 1], optionId) >= 0;
        }
    }

    public sealed class LanternWorkshopLanternState
    {
        public string LanternInstanceId { get; set; }
        public string SeasonId { get; set; }
        public int ContractStage { get; set; }
        public string[] OptionIds { get; set; }
        public int StateVersion { get; set; }
        public string StateHash { get; set; }
    }

    public sealed class LanternWorkshopSnapshot
    {
        public string AccountId { get; set; }
        public long LampGlowBalance { get; set; }
        public string CovenantContractId { get; set; }
        public int[] StageCosts { get; set; }
        public LanternWorkshopLanternState[] Lanterns { get; set; }
        public string RequestId { get; set; }
    }

    public sealed class LanternCovenantAdvanceResult
    {
        public string ReceiptId { get; set; }
        public string AccountId { get; set; }
        public string LanternInstanceId { get; set; }
        public int ResultingStage { get; set; }
        public string SelectedOptionId { get; set; }
        public int CostLampGlow { get; set; }
        public long RemainingLampGlow { get; set; }
        public string[] ResultingOptionIds { get; set; }
        public int ResultingStateVersion { get; set; }
        public string ResultingStateHash { get; set; }
        public string IdempotencyKey { get; set; }
        public string CreatedAt { get; set; }
        public bool WasAlreadyApplied { get; set; }
        public string RequestId { get; set; }
    }

    public interface ILanternWorkshopServerTransport
    {
        Task<LanternWorkshopSnapshot> GetSnapshotAsync(
            string accessToken, CancellationToken cancellationToken);
        Task<LanternCovenantAdvanceResult> AdvanceAsync(
            string accessToken, string lanternInstanceId, string optionId,
            int expectedStateVersion, string idempotencyKey,
            CancellationToken cancellationToken);
    }

    public sealed class LanternWorkshopCoordinator
    {
        private readonly ILanternWorkshopServerTransport transport;

        public LanternWorkshopCoordinator(ILanternWorkshopServerTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException("transport");
        }

        public async Task<LanternWorkshopSnapshot> LoadAsync(
            string accessToken, string expectedAccountId,
            CancellationToken cancellationToken)
        {
            var snapshot = await transport.GetSnapshotAsync(accessToken, cancellationToken);
            ValidateSnapshot(snapshot, expectedAccountId);
            return snapshot;
        }

        public async Task<LanternCovenantAdvanceResult> AdvanceAsync(
            string accessToken, string expectedAccountId, string lanternInstanceId,
            string optionId, int expectedStateVersion, string idempotencyKey,
            CancellationToken cancellationToken)
        {
            Guid parsed;
            if (!Guid.TryParse(lanternInstanceId, out parsed) ||
                expectedStateVersion < 1 || string.IsNullOrWhiteSpace(idempotencyKey))
                throw new ArgumentException("The lantern covenant advance request is invalid.");
            if (string.IsNullOrWhiteSpace(optionId))
                throw new ArgumentException("A covenant option is required.", "optionId");
            var result = await transport.AdvanceAsync(accessToken, lanternInstanceId,
                optionId, expectedStateVersion, idempotencyKey, cancellationToken);
            ValidateAdvance(result, expectedAccountId, lanternInstanceId, optionId);
            return result;
        }

        private static void ValidateSnapshot(LanternWorkshopSnapshot snapshot,
            string expectedAccountId)
        {
            Guid expected;
            Guid actual;
            if (snapshot == null || !Guid.TryParse(expectedAccountId, out expected) ||
                !Guid.TryParse(snapshot.AccountId, out actual) || expected != actual ||
                snapshot.LampGlowBalance < 0 ||
                !string.Equals(snapshot.CovenantContractId,
                    GuardTideCovenantRules.ContractId, StringComparison.Ordinal) ||
                snapshot.StageCosts == null || snapshot.StageCosts.Length != 6)
                throw new InvalidOperationException("The lantern workshop snapshot is invalid.");
            for (var stage = 1; stage <= 6; stage++)
                if (snapshot.StageCosts[stage - 1] != GuardTideCovenantRules.CostForStage(stage))
                    throw new InvalidOperationException("The lantern workshop cost contract changed.");
            foreach (var lantern in snapshot.Lanterns ?? new LanternWorkshopLanternState[0])
                ValidateLantern(lantern);
        }

        private static void ValidateLantern(LanternWorkshopLanternState lantern)
        {
            Guid id;
            if (lantern == null || !Guid.TryParse(lantern.LanternInstanceId, out id) ||
                string.IsNullOrWhiteSpace(lantern.SeasonId) || lantern.ContractStage < 0 ||
                lantern.ContractStage > 6 || lantern.StateVersion < 1 ||
                !IsSha256(lantern.StateHash) ||
                (lantern.OptionIds ?? new string[0]).Length != lantern.ContractStage)
                throw new InvalidOperationException("The lantern covenant state is invalid.");
            for (var index = 0; index < lantern.ContractStage; index++)
                if (!GuardTideCovenantRules.IsOptionForStage(index + 1, lantern.OptionIds[index]))
                    throw new InvalidOperationException("The lantern covenant path is invalid.");
        }

        private static void ValidateAdvance(LanternCovenantAdvanceResult result,
            string expectedAccountId, string lanternInstanceId, string optionId)
        {
            Guid account;
            Guid expectedAccount;
            Guid lantern;
            Guid expectedLantern;
            Guid receipt;
            if (result == null || !Guid.TryParse(result.ReceiptId, out receipt) ||
                !Guid.TryParse(result.AccountId, out account) ||
                !Guid.TryParse(expectedAccountId, out expectedAccount) || account != expectedAccount ||
                !Guid.TryParse(result.LanternInstanceId, out lantern) ||
                !Guid.TryParse(lanternInstanceId, out expectedLantern) || lantern != expectedLantern ||
                !string.Equals(result.SelectedOptionId, optionId, StringComparison.Ordinal) ||
                result.ResultingStage < 1 || result.ResultingStage > 6 ||
                result.CostLampGlow != GuardTideCovenantRules.CostForStage(result.ResultingStage) ||
                result.RemainingLampGlow < 0 || !IsSha256(result.ResultingStateHash) ||
                (result.ResultingOptionIds ?? new string[0]).Length != result.ResultingStage)
                throw new InvalidOperationException("The lantern covenant advance response is invalid.");
        }

        private static bool IsSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            foreach (var character in value)
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'A' && character <= 'F') &&
                    !(character >= 'a' && character <= 'f')) return false;
            return true;
        }
    }
}
