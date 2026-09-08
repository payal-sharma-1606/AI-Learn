namespace SmartNotes.Api.Models;

/// <summary>
/// One indexed slice of a note, and the schema of the Azure AI Search index.
/// </summary>
/// <remarks>
/// Notes are indexed in pieces rather than whole because an embedding of a long note averages
/// out its distinct topics, which blurs retrieval. A chunk carries one idea, so its vector
/// points somewhere meaningful.
/// </remarks>
public class NoteChunk
{
    /// <summary>
    /// Document key, "{NoteId}-{ChunkIndex}". Deterministic so that re-indexing a note
    /// overwrites its chunks instead of duplicating them. Azure AI Search keys must be strings.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Filterable, so all chunks of one note can be found and replaced together.</summary>
    public int NoteId { get; set; }

    /// <summary>Position of this chunk within the note, 0-based.</summary>
    public int ChunkIndex { get; set; }

    /// <summary>Carried on every chunk so a search hit can be displayed without a database round trip.</summary>
    public string Title { get; set; } = string.Empty;

    public string ChunkText { get; set; } = string.Empty;

    /// <summary>The embedding of <see cref="ChunkText"/>. This is what similarity search compares.</summary>
    public float[] Embedding { get; set; } = [];
}
