using CrmApi.Models;
using CrmApi.Data;
using Microsoft.EntityFrameworkCore;

// Define the IClientNoteService interface 
public interface IClientNoteService
{
    Task<List<Note>> GetNotesAsync(Guid clientId);
    Task<Note?> GetNoteByIdAsync(Guid clientId, Guid noteId);
    Task<Note> CreateNoteAsync(Note note);
    Task<bool> UpdateNoteAsync(Note note);
    Task<bool> DeleteNoteAsync(Guid clientId, Guid noteId);
}


public class ClientNoteService : IClientNoteService
{
    private readonly ApplicationDbContext _context;

    public ClientNoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Note>> GetNotesAsync(Guid clientId)
    {
        return await _context.Notes
            .Where(n => n.ClientId == clientId)
            .ToListAsync();
    }

    public async Task<Note?> GetNoteByIdAsync(Guid clientId, Guid noteId)
    {
        return await _context.Notes
            .FirstOrDefaultAsync(n => n.ClientId == clientId && n.Id == noteId);
    }

    public async Task<Note> CreateNoteAsync(Note note)
    {
        _context.Notes.Add(note);
        await _context.SaveChangesAsync();
        return note;
    }

    public async Task<bool> UpdateNoteAsync(Note note)
    {
        var existingNote = await _context.Notes
            .FirstOrDefaultAsync(n => n.ClientId == note.ClientId && n.Id == note.Id);

        if (existingNote == null) return false;

        // Copy updatable fields from incoming note DTO/model into the existing entity
        existingNote.Content = note.Content ?? existingNote.Content;
        existingNote.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteNoteAsync(Guid clientId, Guid noteId)
    {
        var existingNote = await _context.Notes
            .FirstOrDefaultAsync(n => n.ClientId == clientId && n.Id == noteId);

        if (existingNote == null) return false;

        _context.Notes.Remove(existingNote);
        await _context.SaveChangesAsync();
        return true;
    }
    
}