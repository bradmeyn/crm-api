using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CrmApi.DTOs.Note;
using CrmApi.Models;
using CrmApi.Services;

[Authorize]
[ApiController]
[Route("api/clients/{clientId}/notes")]  


public class ClientNoteController : ControllerBase
{
    private readonly IClientNoteService _noteService;
    private readonly ILogger<ClientNoteController> _logger;

    private readonly ICurrentUserService _currentUserService;

    public ClientNoteController(
        IClientNoteService noteService,
        ICurrentUserService currentUserService,
        ILogger<ClientNoteController> logger)
    {
        _noteService = noteService;
        _currentUserService = currentUserService;
        _logger = logger;
    }
    [HttpGet]
    public async Task<IActionResult> GetNotes(Guid clientId)
    {
        var notes = await _noteService.GetNotesAsync(clientId);
        return Ok(notes);
    }

    [HttpGet("{noteId}")]
    public async Task<IActionResult> GetNoteById(Guid clientid, Guid noteId)
    {
        var note = await _noteService.GetNoteByIdAsync(clientid, noteId);
        return Ok(note);
    }

    [HttpPost]
    public async Task<IActionResult> CreateNote([FromBody] CreateNoteDto data)
    {
        var userId = _currentUserService.UserId;
        var now = DateTime.UtcNow;
        
        var note = new Note
        {
            Title = data.Title,
            Content = data.Content,
            Type = data.Type,
            ClientId = data.ClientId,
            CreatedById = userId,
            CreatedAt = now,
            UpdatedById = userId,
            UpdatedAt = now
        };

        var newNote = await _noteService.CreateNoteAsync(note);
        return Ok(newNote);
    }

    [HttpPost("{noteId}")]
    public async Task<IActionResult> UpdateNote([FromBody] UpdateNoteDto data)
    {
        
        var updatedNote = new Note
        {
            Id = data.id,
            Title = data.Title,
            Content = data.Content,
            Type = data.Type,
            ClientId = data.ClientId,
            UpdatedById = _currentUserService.UserId,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _noteService.UpdateNoteAsync(updatedNote);
        if (!result) return NotFound();
        return NoContent();
    }
}
