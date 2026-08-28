using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartNotes.Api.Data;
using SmartNotes.Api.Models;
using SmartNotes.Api.Services;

namespace SmartNotes.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAiService _aiService;
    private readonly ILogger<NotesController> _logger;

    public NotesController(AppDbContext context, IAiService aiService, ILogger<NotesController> logger)
    {
        _context = context;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Note>>> GetNotes(CancellationToken cancellationToken)
    {
        return await _context.Notes
            .OrderByDescending(n => n.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Note>> GetNote(int id, CancellationToken cancellationToken)
    {
        var note = await _context.Notes.FindAsync([id], cancellationToken);
        if (note == null)
        {
            return NotFound();
        }
        return note;
    }

    [HttpPost]
    public async Task<ActionResult<Note>> CreateNote(NoteInput input, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var note = new Note
        {
            Title = input.Title,
            Content = input.Content,
            Tags = input.Tags,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(int id, NoteInput input, CancellationToken cancellationToken)
    {
        var existing = await _context.Notes.FindAsync([id], cancellationToken);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Title = input.Title;
        existing.Content = input.Content;
        existing.Tags = input.Tags;
        existing.UpdatedAt = DateTime.UtcNow;
        // Summary is intentionally untouched: it is owned by the summarize endpoint.

        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/summarize")]
    public async Task<ActionResult<Note>> SummarizeNote(int id, CancellationToken cancellationToken)
    {
        var note = await _context.Notes.FindAsync([id], cancellationToken);
        if (note == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(note.Content))
        {
            return Problem(
                title: "Nothing to summarize",
                detail: "This note has no content to summarize.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        try
        {
            note.Summary = await _aiService.SummarizeAsync(note.Content, cancellationToken);
        }
        catch (AiServiceException ex)
        {
            _logger.LogError(ex, "Could not summarize note {NoteId}.", id);
            return Problem(
                title: "Summary unavailable",
                detail: ex.Message,
                statusCode: ex.IsTransient
                    ? StatusCodes.Status503ServiceUnavailable
                    : StatusCodes.Status502BadGateway);
        }

        note.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return note;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id, CancellationToken cancellationToken)
    {
        var note = await _context.Notes.FindAsync([id], cancellationToken);
        if (note == null)
        {
            return NotFound();
        }

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
