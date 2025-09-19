using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Api.Models;
using Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Api.Data;

namespace Api;

[ApiController]
[Route("api/clients")]
[Authorize]
public class ClientController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ClientController> _logger;

    public ClientController(ApplicationDbContext context, ILogger<ClientController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientListDto>>> GetClients()
    {
        var clients = await _context.Clients
            .Select(c => new ClientListDto
            {
                Id = c.Id,
                Salutation = c.Salutation,
                FirstName = c.FirstName,
                LastName = c.LastName,
                PreferredName = c.PreferredName,
                Email = c.Email,
                Phone = c.Phone,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientResponseDto>> GetClient(Guid id)
    {
        var client = await _context.Clients
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            return NotFound();
        }

        var responseDto = new ClientResponseDto
        {
            Id = client.Id,
            Salutation = client.Salutation,
            FirstName = client.FirstName,
            LastName = client.LastName,
            PreferredName = client.PreferredName,
            Email = client.Email,
            Phone = client.Phone,
            Address = client.Address != null ? new AddressDto
            {
                Street = client.Address.Street,
                Suburb = client.Address.Suburb,
                State = client.Address.State,
                PostCode = client.Address.PostCode,
            } : null,
            IsActive = client.IsActive,
            BusinessId = client.BusinessId,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };

        return Ok(responseDto);
    }

    [HttpPost]
    public async Task<ActionResult<ClientResponseDto>> CreateClient(CreateClientDto createDto)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            Salutation = createDto.Salutation,
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            PreferredName = createDto.PreferredName,
            Email = createDto.Email,
            Phone = createDto.Phone,
            IsActive = true,
            BusinessId = createDto.BusinessId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Handle address if provided
        if (createDto.Address != null)
        {
            client.Address = new Address
            {
                Id = Guid.NewGuid(),
                Street = createDto.Address.Street,
                Suburb = createDto.Address.Suburb,
                State = createDto.Address.State,
                PostCode = createDto.Address.PostCode,
                ClientId = client.Id
            };
        }

        _context.Clients.Add(client);
        await _context.SaveChangesAsync();

        // Return the created client
        var responseDto = new ClientResponseDto
        {
            Id = client.Id,
            Salutation = client.Salutation,
            FirstName = client.FirstName,
            LastName = client.LastName,
            PreferredName = client.PreferredName,
            Email = client.Email,
            Phone = client.Phone,
            Address = client.Address != null ? new AddressDto
            {
                Street = client.Address.Street,
                Suburb = client.Address.Suburb,
                State = client.Address.State,
                PostCode = client.Address.PostCode,

            } : null,
            IsActive = client.IsActive,
            BusinessId = client.BusinessId,
            CreatedAt = client.CreatedAt,
            UpdatedAt = client.UpdatedAt
        };

        return CreatedAtAction(
            nameof(GetClient),
            new { id = client.Id },
            responseDto
        );
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, UpdateClientDto updateDto)
    {
        var client = await _context.Clients
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            return NotFound();
        }

        // Update client properties
        client.Salutation = updateDto.Salutation;
        client.FirstName = updateDto.FirstName;
        client.LastName = updateDto.LastName;
        client.PreferredName = updateDto.PreferredName;
        client.Email = updateDto.Email;
        client.Phone = updateDto.Phone;
        client.IsActive = updateDto.IsActive;
        client.BusinessId = updateDto.BusinessId;
        client.UpdatedAt = DateTime.UtcNow;

        // Handle address update
        if (updateDto.Address != null)
        {
            if (client.Address == null)
            {
                // Create new address
                client.Address = new Address
                {
                    Id = Guid.NewGuid(),
                    ClientId = client.Id
                };
            }

            // Update address properties
            client.Address.Street = updateDto.Address.Street;
            client.Address.Suburb = updateDto.Address.Suburb;
            client.Address.State = updateDto.Address.State;
            client.Address.PostCode = updateDto.Address.PostCode;

        }


        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ClientExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var client = await _context.Clients
            .Include(c => c.Address)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (client == null)
        {
            return NotFound();
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ClientExists(Guid id)
    {
        return _context.Clients.Any(e => e.Id == id);
    }
}