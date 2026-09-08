namespace SmartNotes.Api.Interfaces;

/// <summary>
/// The vector index that holds note chunks. Implementations throw
/// <see cref="Exceptions.AiServiceException"/> when the index cannot be reached or updated.
/// </summary>
public interface ISearchIndexService
{
    /// <summary>
    /// Creates the index if it is missing, or updates it to match the current schema.
    /// Safe to call repeatedly.
    /// </summary>
    Task EnsureIndexAsync(CancellationToken cancellationToken = default);
}
