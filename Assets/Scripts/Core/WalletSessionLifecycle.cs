using System;

namespace SeekerAutoChess.Core
{
    public enum WalletServerSessionState
    {
        NotConfigured,
        SignedOut,
        Restoring,
        SigningIn,
        Authenticated,
        Refreshing,
        SigningOut,
        Expired,
        Failed
    }

    public static class WalletSessionRefreshPolicy
    {
        public static bool ShouldRefresh(
            WalletAuthenticationSession session,
            DateTimeOffset now,
            TimeSpan refreshLeadTime)
        {
            if (refreshLeadTime < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException("refreshLeadTime");
            if (!HasUsableRefreshToken(session, now)) return false;

            DateTimeOffset accessExpiry;
            return !TryParseExpiry(session.AccessExpiresAt, out accessExpiry) ||
                   accessExpiry <= now + refreshLeadTime;
        }

        public static bool IsAccessExpired(
            WalletAuthenticationSession session,
            DateTimeOffset now)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken)) return true;
            DateTimeOffset accessExpiry;
            return !TryParseExpiry(session.AccessExpiresAt, out accessExpiry) || accessExpiry <= now;
        }

        public static bool HasUsableRefreshToken(
            WalletAuthenticationSession session,
            DateTimeOffset now)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.RefreshToken)) return false;
            DateTimeOffset refreshExpiry;
            return TryParseExpiry(session.RefreshExpiresAt, out refreshExpiry) && refreshExpiry > now;
        }

        private static bool TryParseExpiry(string value, out DateTimeOffset result)
        {
            return DateTimeOffset.TryParse(value, out result);
        }
    }
}
