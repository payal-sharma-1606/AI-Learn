using Microsoft.EntityFrameworkCore;
using SmartNotes.Api.Models;

namespace SmartNotes.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Note> Notes => Set<Note>();
}
