namespace SmartNotes.Api.Models;

/// <summary>
/// AI-suggested tags for a note. Deliberately not persisted by the suggest endpoint: the user
/// reviews the suggestions and saves the ones they want through the normal update endpoint.
/// </summary>
public class TagSuggestionResponse
{
    public List<string> Tags { get; set; } = [];
}
