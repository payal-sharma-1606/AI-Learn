using System.ComponentModel.DataAnnotations;

namespace SmartNotes.Api.Models;

/// <summary>
/// The client-supplied fields of a note. Deliberately excludes Id, Summary, CreatedAt and
/// UpdatedAt so callers cannot set server-owned state, and so an edit cannot clear a summary.
/// </summary>
public class NoteInput
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string Tags { get; set; } = string.Empty;
}
