
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Api.DTOs.Client;
using CrmApi.Models;
using System;


[Authorize]
[ApiController]
[Route("api/clients")]

public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<ClientController> _logger;

     protected Guid BusinessId
    {
        get
        {
            var businessIdClaim = User?.FindFirst("businessId")?.Value;
            if (string.IsNullOrEmpty(businessIdClaim) || !Guid.TryParse(businessIdClaim, out var id))
            {
                throw new UnauthorizedAccessException("Business ID not found in claims");
            }
            return id;
        }
    }

    public ClientController(
        IClientService clientService,
        ILogger<ClientController> logger
    )
    {
        _clientService = clientService;
        _logger = logger;
    }

 [HttpGet]
    public async Task<IActionResult> GetClients()
    {

        var clients = await _clientService.GetClientsAsync(BusinessId);
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClientById(Guid id)
    {
        var client = await _clientService.GetClientByIdAsync(BusinessId, id);
        if (client == null) return NotFound();
        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] CreateClientDto dto) // ✅ Use DTO, not entity
    {
        var client = new Client
        {
            Title = dto.Title,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PreferredName = dto.PreferredName,
            Email = dto.Email,
            Phone = dto.Phone ?? string.Empty,
            Street = dto.Address?.Street,
            Suburb = dto.Address?.Suburb,
            State = dto.Address?.State,
            PostCode = dto.Address?.PostCode,
            BusinessId = BusinessId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createdClient = await _clientService.CreateClientAsync(client);
        return CreatedAtAction(nameof(GetClientById), new { id = createdClient.Id }, createdClient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateClientDto dto) // ✅ Use DTO
    {
        var client = new Client
        {
            Id = id,
            Title = string.Empty,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            PreferredName = dto.PreferredName,
            Email = dto.Email,
            Phone = dto.Phone ?? string.Empty,
            Street = dto.Address?.Street,
            Suburb = dto.Address?.Suburb,
            State = dto.Address?.State,
            PostCode = dto.Address?.PostCode,
            BusinessId = dto.BusinessId,
            UpdatedAt = DateTime.UtcNow
        };

        var updated = await _clientService.UpdateClientAsync(client);
        if (!updated) return NotFound();

        // Return the updated client
        var updatedClient = await _clientService.GetClientByIdAsync(BusinessId, id);
        return Ok(updatedClient);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var deleted = await _clientService.DeleteClientAsync(BusinessId, id);
        if (!deleted) return NotFound();
        return NoContent();
    }    
}