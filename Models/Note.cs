namespace CrmApi.Models;

public class Note: BaseEntity
{
    public string Type {get; set;} = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    
    // Associations
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public User UpdatedBy { get; set; } = null!;
}
