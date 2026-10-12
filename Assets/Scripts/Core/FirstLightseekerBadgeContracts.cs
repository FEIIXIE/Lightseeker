using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class FirstLightseekerBadgeGrantSnapshot
    {
        public string GrantId;
        public string AccountId;
        public string BadgeId;
        public string RulesVersion;
        public string FirstAchievedSeasonId;
        public string AchievedAtUtc;
        public string Status;
        public bool IdempotentReplay;
        public string RequestId;
    }

    public interface IFirstLightseekerBadgeGrantTransport
    {
        Task<FirstLightseekerBadgeGrantSnapshot> ClaimAsync(
            string accessToken,
            CancellationToken cancellationToken);
    }
}
