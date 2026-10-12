using System;
using System.Collections.Generic;
using System.Globalization;

namespace SeekerAutoChess.Core
{
    public enum AuthoritativeWarehouseItemKind
    {
        Equipment = 0,
        Lantern = 1
    }

    public sealed class AuthoritativeWarehouseItem
    {
        internal AuthoritativeWarehouseItem(
            AuthoritativeWarehouseItemKind kind,
            string instanceId,
            string templateId,
            string rarityId,
            string acquiredAt,
            int stateVersion,
            string stateHash)
        {
            Kind = kind;
            InstanceId = instanceId;
            TemplateId = templateId;
            RarityId = rarityId;
            AcquiredAt = acquiredAt;
            StateVersion = stateVersion;
            StateHash = stateHash;
        }

        public AuthoritativeWarehouseItemKind Kind { get; private set; }
        public string InstanceId { get; private set; }
        public string TemplateId { get; private set; }
        public string RarityId { get; private set; }
        public string AcquiredAt { get; private set; }
        public int StateVersion { get; private set; }
        public string StateHash { get; private set; }
    }

    /// <summary>
    /// Account-bound, server-derived warehouse list. It deliberately exposes no
    /// local mutation methods. Online economic writes must use dedicated server
    /// commands; an offline projection remains display-only.
    /// </summary>
    public sealed class AuthoritativeWarehouseProjection
    {
        private readonly AuthoritativeWarehouseItem[] items;

        private AuthoritativeWarehouseProjection(
            string accountId,
            AuthoritativeWarehouseItem[] items,
            int pendingQueueItems,
            int failedQueueItems,
            bool isOnline)
        {
            AccountId = accountId;
            this.items = items;
            PendingQueueItems = pendingQueueItems;
            FailedQueueItems = failedQueueItems;
            IsOnline = isOnline;
        }

        public string AccountId { get; private set; }
        public IReadOnlyList<AuthoritativeWarehouseItem> Items { get { return items; } }
        public int PendingQueueItems { get; private set; }
        public int FailedQueueItems { get; private set; }
        public bool IsOnline { get; private set; }
        public bool AllowsLocalEconomicWrites { get { return false; } }

        public static AuthoritativeWarehouseProjection Create(
            AuthoritativeRewardInventorySnapshot snapshot,
            string authenticatedAccountId,
            bool isOnline)
        {
            Guid snapshotAccount;
            Guid authenticatedAccount;
            if (snapshot == null ||
                !Guid.TryParse(snapshot.AccountId, out snapshotAccount) ||
                !Guid.TryParse(authenticatedAccountId, out authenticatedAccount) ||
                snapshotAccount != authenticatedAccount)
                throw new InvalidOperationException(
                    "The authoritative warehouse inventory does not belong to the authenticated account.");

            var projected = new List<AuthoritativeWarehouseItem>();
            foreach (var equipment in snapshot.Equipment ??
                     new AuthoritativeEquipmentInstance[0])
                projected.Add(CreateItem(
                    AuthoritativeWarehouseItemKind.Equipment,
                    equipment.InstanceId, equipment.TemplateId, equipment.RarityId,
                    equipment.AcquiredAt, equipment.StateVersion, equipment.StateHash));
            foreach (var lantern in snapshot.Lanterns ??
                     new AuthoritativeLanternInstance[0])
                projected.Add(CreateItem(
                    AuthoritativeWarehouseItemKind.Lantern,
                    lantern.InstanceId, lantern.TemplateId, lantern.RarityId,
                    lantern.AcquiredAt, lantern.StateVersion, lantern.StateHash));

            projected.Sort(CompareByAcquiredAtThenId);
            return new AuthoritativeWarehouseProjection(
                snapshotAccount.ToString("N"), projected.ToArray(),
                snapshot.PendingQueueItems, snapshot.FailedQueueItems, isOnline);
        }

        private static AuthoritativeWarehouseItem CreateItem(
            AuthoritativeWarehouseItemKind kind,
            string instanceId,
            string templateId,
            string rarityId,
            string acquiredAt,
            int stateVersion,
            string stateHash)
        {
            Guid parsedInstance;
            DateTimeOffset parsedAcquiredAt;
            if (!Guid.TryParse(instanceId, out parsedInstance) ||
                string.IsNullOrWhiteSpace(templateId) ||
                string.IsNullOrWhiteSpace(rarityId) ||
                !DateTimeOffset.TryParse(acquiredAt, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out parsedAcquiredAt) ||
                parsedAcquiredAt.Offset != TimeSpan.Zero || stateVersion < 1 ||
                !IsSha256(stateHash))
                throw new InvalidOperationException(
                    "The authoritative warehouse item is invalid.");
            return new AuthoritativeWarehouseItem(
                kind, parsedInstance.ToString("N"), templateId, rarityId,
                parsedAcquiredAt.ToString("O", CultureInfo.InvariantCulture),
                stateVersion, stateHash.ToUpperInvariant());
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

        private static int CompareByAcquiredAtThenId(
            AuthoritativeWarehouseItem left,
            AuthoritativeWarehouseItem right)
        {
            var acquired = string.CompareOrdinal(left.AcquiredAt, right.AcquiredAt);
            return acquired != 0
                ? acquired
                : string.CompareOrdinal(left.InstanceId, right.InstanceId);
        }
    }
}
