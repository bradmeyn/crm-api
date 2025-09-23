using CrmApi.Models;
using CrmApi.Data;
using Microsoft.EntityFrameworkCore;

public interface IClientService
{
    Task<List<Client>> GetClientsAsync(Guid businessId);
    Task<Client?> GetClientByIdAsync(Guid businessId, Guid clientId);
    Task<Client> CreateClientAsync(Client client);
    Task<bool> UpdateClientAsync(Client client);
    Task<bool> DeleteClientAsync(Guid businessId, Guid clientId);
}

public class ClientService : IClientService
{
    private readonly ApplicationDbContext _context;

    public ClientService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Client>> GetClientsAsync(Guid businessId)
    {
        return await _context.Clients
            .Where(c => c.BusinessId == businessId)
            .ToListAsync();
    }

    public async Task<Client?> GetClientByIdAsync(Guid businessId, Guid clientId)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == clientId);
    }

    public async Task<Client> CreateClientAsync(Client client)
    {
        _context.Clients.Add(client);
        await _context.SaveChangesAsync();
        return client;
    }

    public async Task<bool> UpdateClientAsync(Client client)
    {
        var existingClient = await _context.Clients
            .FirstOrDefaultAsync(c => c.BusinessId == client.BusinessId && c.Id == client.Id);

        if (existingClient == null) return false;


        _context.Clients.Update(existingClient);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClientAsync(Guid businessId, Guid clientId)
    {
        var client = await _context.Clients
            .FirstOrDefaultAsync(c => c.BusinessId == businessId && c.Id == clientId);

        if (client == null) return false;

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
        return true;
    }
}