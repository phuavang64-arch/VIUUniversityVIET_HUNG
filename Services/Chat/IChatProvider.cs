namespace VIUUniversity.Services.Chat
{
    /// <summary>
    /// A conversation-capable language model. Implementations must NOT execute SQL or
    /// any other side effect; they only translate a list of turns into text.
    /// </summary>
    public interface IChatProvider
    {
        string Name { get; }

        /// <summary>Returns null when the provider is unreachable or errored.</summary>
        Task<string?> CompleteAsync(IReadOnlyList<ChatTurn> turns, CancellationToken ct);
    }
}