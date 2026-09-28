using System;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionPhase
    {
        Preparation = 0,
        Battle = 1,
        Upgrade = 2,
        Route = 3,
        Complete = 4,
        Node = 5
    }

    /// <summary>
    /// The legal high-level transitions of one expedition.  Keeping this outside
    /// Unity lets the client and the future ASP.NET server share the same rules.
    /// </summary>
    public sealed class ExpeditionFlow
    {
        public ExpeditionPhase Phase { get; private set; } = ExpeditionPhase.Preparation;

        public bool IsBattling => Phase == ExpeditionPhase.Battle;
        public bool CanPrepare => Phase == ExpeditionPhase.Preparation;
        public bool CanChooseUpgrade => Phase == ExpeditionPhase.Upgrade;
        public bool CanChooseRoute => Phase == ExpeditionPhase.Route;
        public bool IsComplete => Phase == ExpeditionPhase.Complete;
        public bool CanResolveNode => Phase == ExpeditionPhase.Node;

        public bool TryBeginBattle()
        {
            if (!CanPrepare) return false;
            Phase = ExpeditionPhase.Battle;
            return true;
        }

        public void BeginAtRoute()
        {
            Phase = ExpeditionPhase.Route;
        }

        public bool TryFinishBattle(bool playerWon, bool runEnded)
        {
            if (!IsBattling) return false;
            Phase = runEnded
                ? ExpeditionPhase.Complete
                : playerWon ? ExpeditionPhase.Upgrade : ExpeditionPhase.Preparation;
            return true;
        }

        public bool TryChooseUpgrade()
        {
            if (!CanChooseUpgrade) return false;
            Phase = ExpeditionPhase.Route;
            return true;
        }

        public bool TryChooseRoute()
        {
            if (!CanChooseRoute) return false;
            Phase = ExpeditionPhase.Preparation;
            return true;
        }

        public bool TryEnterNode()
        {
            if (!CanChooseRoute) return false;
            Phase = ExpeditionPhase.Node;
            return true;
        }

        public bool TryFinishNode()
        {
            if (!CanResolveNode) return false;
            Phase = ExpeditionPhase.Route;
            return true;
        }

        public bool TryAbandon()
        {
            if (IsComplete) return false;
            Phase = ExpeditionPhase.Complete;
            return true;
        }

        public bool TryRestore(int serializedPhase)
        {
            if (!Enum.IsDefined(typeof(ExpeditionPhase), serializedPhase)) return false;
            var restored = (ExpeditionPhase)serializedPhase;
            // A battle is never restored from a local mid-animation snapshot.  The
            // current prototype saves only stable phases; future online runs recover
            // authoritative battle state from the server.
            if (restored == ExpeditionPhase.Battle) return false;
            Phase = restored;
            return true;
        }

        public void Reset()
        {
            Phase = ExpeditionPhase.Preparation;
        }

        public static bool IsUnrecoverablePreparation(
            ExpeditionPhase phase,
            int coins,
            int occupiedUnits,
            int minimumRecruitCost)
        {
            return phase == ExpeditionPhase.Preparation &&
                   occupiedUnits == 0 &&
                   coins < minimumRecruitCost;
        }
    }
}
