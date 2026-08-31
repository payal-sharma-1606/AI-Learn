namespace SmartNotes.Api.Interfaces;

/// <summary>
/// AI capabilities applied to note content. Implementations are expected to throw
/// <see cref="Exceptions.AiServiceException"/> when a result cannot be produced.
/// </summary>
public interface IAiService
{
    /// <summary>
    /// Produces a short prose summary of <paramref name="content"/>.
    /// </summary>
    Task<string> SummarizeAsync(string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Suggests 3-5 short topic tags for <paramref name="content"/>. Suggestions are returned
    /// to the caller, never persisted: the user reviews them before they are saved.
    /// </summary>
    Task<List<string>> SuggestTagsAsync(string content, CancellationToken cancellationToken = default);
}
