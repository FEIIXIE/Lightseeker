using System;

namespace SeekerAutoChess.Core
{
    public static class MainnetInvitationLinkContract
    {
        public const string Scheme = "lightseeker";
        public const string Host = "invite";

        public static bool IsInvitationLink(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) && IsExpectedEndpoint(uri);
        }

        public static bool TryExtractCode(string value, out string invitationCode)
        {
            invitationCode = null;
            Uri uri;
            if (!Uri.TryCreate(value, UriKind.Absolute, out uri) || !IsExpectedEndpoint(uri))
                return false;

            try
            {
                var query = uri.Query;
                if (string.IsNullOrEmpty(query) || query[0] != '?') return false;
                string candidate = null;
                foreach (var pair in query.Substring(1).Split('&'))
                {
                    if (string.IsNullOrEmpty(pair)) return false;
                    var separator = pair.IndexOf('=');
                    var key = Uri.UnescapeDataString(
                        separator < 0 ? pair : pair.Substring(0, separator));
                    if (!string.Equals(key, "code", StringComparison.OrdinalIgnoreCase)) return false;
                    if (candidate != null) return false;
                    candidate = Uri.UnescapeDataString(
                        separator < 0 ? string.Empty : pair.Substring(separator + 1));
                }

                candidate = candidate == null ? null : candidate.Trim().ToUpperInvariant();
                if (!IsCanonicalCode(candidate)) return false;
                invitationCode = candidate;
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }

        private static bool IsExpectedEndpoint(Uri uri)
        {
            return string.Equals(uri.Scheme, Scheme, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(uri.Host, Host, StringComparison.OrdinalIgnoreCase) &&
                   uri.IsDefaultPort &&
                   (string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/") &&
                   string.IsNullOrEmpty(uri.UserInfo) &&
                   string.IsNullOrEmpty(uri.Fragment);
        }

        public static bool IsCanonicalCode(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 40 ||
                !value.StartsWith("LSI1-", StringComparison.Ordinal))
                return false;
            for (var index = 5; index < value.Length; index++)
            {
                if (index == 13 || index == 22 || index == 31)
                {
                    if (value[index] != '-') return false;
                    continue;
                }

                var character = value[index];
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'F')))
                    return false;
            }
            return true;
        }
    }
}
