using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SeekerAutoChess.Wallet
{
    public sealed class UnityBountyTowerTransport : IBountyTowerDataTransport
    {
        private const int MaximumResponseBytes = 128 * 1024;
        private readonly Uri baseUri;
        private readonly int timeoutSeconds;

        public UnityBountyTowerTransport(
            string baseUrl,
            bool allowInsecureLocalDevelopment,
            int timeoutSeconds = 10)
        {
            Uri parsed;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException("The bounty server base URL must be absolute.", "baseUrl");
            if (!IsAllowedEndpoint(parsed, allowInsecureLocalDevelopment))
                throw new ArgumentException(
                    "The bounty server must use HTTPS. HTTP is only allowed explicitly for a local development host.",
                    "baseUrl");
            if (timeoutSeconds < 1 || timeoutSeconds > 60)
                throw new ArgumentOutOfRangeException("timeoutSeconds");

            baseUri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<BountyServerRankingPage> GetRankingPageAsync(
            string accessToken,
            string mode,
            int offset,
            int limit,
            CancellationToken cancellationToken)
        {
            if (mode != "pve" && mode != "pvp")
                throw new ArgumentException("The bounty ranking mode must be pve or pvp.", "mode");
            if (offset < 0 || limit < 1 || limit > 100)
                throw new ArgumentOutOfRangeException("offset");
            var relativePath = "v1/bounty/rankings/" + mode +
                               "?offset=" + offset + "&limit=" + limit;
            var response = await SendJsonAsync<RankingPageResponse>(
                relativePath, accessToken, cancellationToken);
            return Map(response);
        }

        public async Task<BountyServerDailyState> GetDailyAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            var response = await SendJsonAsync<DailyResponse>(
                "v1/bounty/daily/me", accessToken, cancellationToken);
            var sourceTickets = response.tickets ?? new TicketResponse[0];
            var tickets = new BountyServerTicket[sourceTickets.Length];
            for (var index = 0; index < sourceTickets.Length; index++)
            {
                var source = sourceTickets[index] ?? new TicketResponse();
                tickets[index] = new BountyServerTicket
                {
                    TicketId = source.ticketId,
                    SourceCode = source.sourceCode,
                    AcquiredAt = source.acquiredAt
                };
            }

            return new BountyServerDailyState
            {
                SnapshotId = response.snapshotId,
                GeneratedAt = response.generatedAt,
                NextDrawAt = response.nextDrawAt,
                IsSynthetic = response.isSynthetic,
                PoolMicroSkr = response.poolMicroSkr,
                EstimatedSingleTicketWinRate = (decimal)response.estimatedSingleTicketWinRate,
                CanClaim = response.canClaim,
                Tickets = tickets,
                RequestId = response.requestId
            };
        }

        private async Task<T> SendJsonAsync<T>(
            string relativePath,
            string accessToken,
            CancellationToken cancellationToken) where T : class
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.", "accessToken");
            cancellationToken.ThrowIfCancellationRequested();
            var request = UnityWebRequest.Get(new Uri(baseUri, relativePath));
            try
            {
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                var operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += ignored => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort))
                    await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();

                var data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaximumResponseBytes)
                    throw new WalletServerException(request.responseCode, "CLIENT_RESPONSE_TOO_LARGE",
                        "The bounty server response exceeded the client limit.", string.Empty);
                var responseText = request.downloadHandler == null
                    ? string.Empty
                    : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                    throw CreateServerException(request.responseCode, responseText, request.error);

                var response = JsonUtility.FromJson<T>(responseText);
                if (response == null)
                    throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                        "The bounty server returned invalid JSON.", string.Empty);
                return response;
            }
            finally
            {
                request.Dispose();
            }
        }

        private static BountyServerRankingPage Map(RankingPageResponse response)
        {
            var sourceRows = response.rows ?? new RankEntryResponse[0];
            var rows = new BountyServerRankEntry[sourceRows.Length];
            for (var index = 0; index < sourceRows.Length; index++)
            {
                var source = sourceRows[index] ?? new RankEntryResponse();
                rows[index] = new BountyServerRankEntry
                {
                    Rank = source.rank,
                    PlayerName = source.playerName,
                    Score = source.score,
                    RewardMicroSkr = source.rewardMicroSkr,
                    IsCurrentPlayer = source.isCurrentPlayer
                };
            }
            var current = response.currentPlayer ?? new CurrentPlayerResponse();
            return new BountyServerRankingPage
            {
                SnapshotId = response.snapshotId,
                Mode = response.mode,
                GeneratedAt = response.generatedAt,
                NextRefreshAt = response.nextRefreshAt,
                IsFinal = response.isFinal,
                IsStale = response.isStale,
                IsSynthetic = response.isSynthetic,
                PoolMicroSkr = response.poolMicroSkr,
                Eligibility = response.eligibility,
                TotalRankedPlayers = response.totalRankedPlayers,
                Offset = response.offset,
                Limit = response.limit,
                CurrentPlayer = new BountyServerCurrentPlayer
                {
                    Rank = current.rank,
                    Score = current.score,
                    EstimatedRewardMicroSkr = current.estimatedRewardMicroSkr,
                    IsEligible = current.isEligible
                },
                Rows = rows,
                RequestId = response.requestId
            };
        }

        private static WalletServerException CreateServerException(
            long statusCode,
            string responseText,
            string fallbackMessage)
        {
            ErrorResponse error = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try { error = JsonUtility.FromJson<ErrorResponse>(responseText); }
                catch (ArgumentException) { }
            }
            return new WalletServerException(
                statusCode,
                error == null ? "TRANSPORT_FAILED" : error.code,
                error != null && !string.IsNullOrWhiteSpace(error.message)
                    ? error.message
                    : (string.IsNullOrWhiteSpace(fallbackMessage)
                        ? "The bounty server request failed."
                        : fallbackMessage),
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint, bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            IPAddress address;
            if (!IPAddress.TryParse(endpoint.Host, out address)) return false;
            if (IPAddress.IsLoopback(address)) return true;
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 &&
                   (bytes[0] == 10 ||
                    (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                    (bytes[0] == 192 && bytes[1] == 168));
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class RankingPageResponse
        {
            public string snapshotId;
            public string mode;
            public string generatedAt;
            public string nextRefreshAt;
            public bool isFinal;
            public bool isStale;
            public bool isSynthetic;
            public long poolMicroSkr;
            public string eligibility;
            public int totalRankedPlayers;
            public int offset;
            public int limit;
            public CurrentPlayerResponse currentPlayer;
            public RankEntryResponse[] rows;
            public string requestId;
        }

        [Serializable]
        private sealed class CurrentPlayerResponse
        {
            public int rank;
            public long score;
            public long estimatedRewardMicroSkr;
            public bool isEligible;
        }

        [Serializable]
        private sealed class RankEntryResponse
        {
            public int rank;
            public string playerName;
            public long score;
            public long rewardMicroSkr;
            public bool isCurrentPlayer;
        }

        [Serializable]
        private sealed class DailyResponse
        {
            public string snapshotId;
            public string generatedAt;
            public string nextDrawAt;
            public bool isSynthetic;
            public long poolMicroSkr;
            public double estimatedSingleTicketWinRate;
            public bool canClaim;
            public TicketResponse[] tickets;
            public string requestId;
        }

        [Serializable]
        private sealed class TicketResponse
        {
            public string ticketId;
            public string sourceCode;
            public string acquiredAt;
        }

        [Serializable]
        private sealed class ErrorResponse
        {
            public string code;
            public string message;
            public string requestId;
        }
#pragma warning restore 0649
    }
}
