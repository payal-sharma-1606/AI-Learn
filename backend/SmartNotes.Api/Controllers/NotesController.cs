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

    public NotesController(AppDbContext context, IAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
    {
        return await _context.Notes.OrderByDescending(n => n.UpdatedAt).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Note>> GetNote(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound();
        }
        return note;
    }

    [HttpPost]
    public async Task<ActionResult<Note>> CreateNote(Note note)
    {
        var now = DateTime.UtcNow;
        note.CreatedAt = now;
        note.UpdatedAt = now;
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(int id, Note note)
    {
        if (id != note.Id)
        {
            return BadRequest();
        }

        var existing = await _context.Notes.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Title = note.Title;
        existing.Content = note.Content;
        existing.Tags = note.Tags;
        existing.Summary = note.Summary;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id}/summarize")]
    public async Task<ActionResult<Note>> SummarizeNote(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        note.Summary = await _aiService.SummarizeAsync(note.Content);
        note.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return note;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(int id)
    {
        var note = await _context.Notes.FindAsync(id);
        if (note == null)
        {
            return NotFound();
        }

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
