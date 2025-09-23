using CrmApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/clients")]

public class ClientController : ControllerBase
{
    private readonly IClientService _clientService;
    private readonly ILogger<ClientController> _logger;

    protected Guid? BusinessId =>
    Guid.TryParse(User?.FindFirst("businessId")?.Value, out var id) ? id : null;

    public ClientController(
        IClientService clientService,
        UserManager<User> userManager,
        ILogger<ClientController> logger
    )
    {
        _clientService = clientService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetClients()
    {
        var businessId = BusinessId;
        if (businessId == null) return Unauthorized();

        var clients = await _clientService.GetClientsAsync(businessId.Value);
        return Ok(clients);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetClientById(Guid id)
    {
        var businessId = BusinessId;
        if (businessId == null) return Unauthorized();

        var client = await _clientService.GetClientByIdAsync(businessId.Value, id);
        if (client == null) return NotFound();

        return Ok(client);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] Client client)
    {
        var businessId = BusinessId;
        if (businessId == null) return Unauthorized();  
        client.BusinessId = businessId.Value;
        var createdClient = await _clientService.CreateClientAsync(client);
        return CreatedAtAction(nameof(GetClientById), new { id = createdClient.Id }, createdClient);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] Client client)
    {
        var businessId = BusinessId;
        if (businessId == null) return Unauthorized();
        if (id != client.Id) return BadRequest("Client ID mismatch");
        client.BusinessId = businessId.Value;
        var updatedClient = await _clientService.UpdateClientAsync(client);
        if (updatedClient == null) return NotFound();
        return Ok(updatedClient);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteClient(Guid id)
    {
        var businessId = BusinessId;
        if (businessId == null) return Unauthorized();

        var deleted = await _clientService.DeleteClientAsync(businessId.Value, id);
        if (!deleted) return NotFound();

        return NoContent();
    }   
}