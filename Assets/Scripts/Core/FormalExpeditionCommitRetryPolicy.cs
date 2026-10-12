using System;

namespace SeekerAutoChess.Core
{
    public enum FormalExpeditionCommitPhase
    {
        Checkpoint = 0,
        Terminal = 1,
        Recovery = 2
    }

    /// <summary>
    /// Preserves the transport facts that determine whether a formal expedition
    /// commit can safely be retried or recovered. The player-facing loop must
    /// never infer these facts from a localized exception message.
    /// </summary>
    public sealed class FormalExpeditionCommitFailure : Exception
    {
        public FormalExpeditionCommitFailure(FormalExpeditionCommitPhase phase,
            long httpStatus, string errorCode, string requestId, Exception innerException)
            : base(BuildMessage(phase, httpStatus, errorCode, requestId, innerException),
                innerException)
        {
            Phase = phase;
            HttpStatus = httpStatus;
            ErrorCode = errorCode ?? string.Empty;
            RequestId = requestId ?? string.Empty;
        }

        public FormalExpeditionCommitPhase Phase { get; private set; }
        public long HttpStatus { get; private set; }
        public string ErrorCode { get; private set; }
        public string RequestId { get; private set; }

        private static string BuildMessage(FormalExpeditionCommitPhase phase,
            long httpStatus, string errorCode, string requestId, Exception innerException)
        {
            return "Formal expedition commit failed phase=" + phase +
                " httpStatus=" + httpStatus +
                " errorCode=" + (errorCode ?? string.Empty) +
                " requestId=" + (requestId ?? string.Empty) +
                " detail=" + (innerException == null ? string.Empty : innerException.Message);
        }
    }

    public static class FormalExpeditionCommitRetryPolicy
    {
        public const int MaximumAttempts = 3;
        public const float MaximumElapsedSeconds = 30f;

        public static bool IsRetryable(FormalExpeditionCommitFailure failure)
        {
            if (failure == null) return false;
            if (failure.HttpStatus == 408 || failure.HttpStatus == 429) return true;
            if (failure.HttpStatus >= 500 && failure.HttpStatus <= 599) return true;
            if (failure.HttpStatus != 0) return false;
            return string.Equals(failure.ErrorCode, "TRANSPORT_FAILED",
                       StringComparison.Ordinal) ||
                   string.Equals(failure.ErrorCode, "TRANSPORT_TIMEOUT",
                       StringComparison.Ordinal);
        }

        public static bool IsOutcomeUncertain(FormalExpeditionCommitFailure failure)
        {
            if (!IsRetryable(failure)) return false;
            // A rate limit is an explicit rejection, not an unknown commit result.
            return failure.HttpStatus != 429;
        }

        public static bool CanRetry(FormalExpeditionCommitFailure failure,
            int completedAttempts, float elapsedSeconds)
        {
            return IsRetryable(failure) && completedAttempts < MaximumAttempts &&
                   elapsedSeconds < MaximumElapsedSeconds;
        }

        public static float RetryDelaySeconds(int completedAttempts)
        {
            return Math.Min(8f, 1.5f + Math.Max(0, completedAttempts));
        }
    }
}
