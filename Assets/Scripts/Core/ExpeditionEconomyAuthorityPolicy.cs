namespace SeekerAutoChess.Core
{
    public enum ExpeditionEconomyAuthorityMode
    {
        LocalPrototype = 0,
        RemoteAuthoritativeRequired = 1
    }

    public static class ExpeditionEconomyAuthorityPolicy
    {
        public static ExpeditionEconomyAuthorityMode Resolve(
            bool formalInviteRuntimeConfigured)
        {
            return formalInviteRuntimeConfigured
                ? ExpeditionEconomyAuthorityMode.RemoteAuthoritativeRequired
                : ExpeditionEconomyAuthorityMode.LocalPrototype;
        }

        public static bool AllowsLocalRewardMutation(
            bool formalInviteRuntimeConfigured)
        {
            return Resolve(formalInviteRuntimeConfigured) ==
                   ExpeditionEconomyAuthorityMode.LocalPrototype;
        }

        public static bool AllowsClientSideItemMutation(bool formalRunBound)
        {
            return !formalRunBound;
        }

        public static void RequireLocalRewardMutationAllowed(
            bool formalInviteRuntimeConfigured)
        {
            if (!AllowsLocalRewardMutation(formalInviteRuntimeConfigured))
                throw new System.InvalidOperationException(
                    "The formal invite runtime requires server-authoritative expedition rewards.");
        }

        public static void RequireClientSideItemMutationAllowed(bool formalRunBound)
        {
            if (!AllowsClientSideItemMutation(formalRunBound))
                throw new System.InvalidOperationException(
                    "A formal expedition requires server-authoritative item instances.");
        }
    }
}
