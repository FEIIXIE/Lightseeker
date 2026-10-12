namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Stores replay state scoped to one authenticated account and authoritative run.
    /// Implementations must not fall back to an unscoped local save.
    /// </summary>
    public interface IExpeditionReplayStateStore
    {
        ExpeditionReplayRecorderState Load(string accountId, string runId);
        void Save(ExpeditionReplayRecorderState state);
        void Clear(string accountId, string runId);
    }
}
