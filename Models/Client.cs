

namespace CrmApi.Models
{
    public class Client
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string Title { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string PreferredName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; } = DateOnly.MinValue;

        // Address
        public string? Street { get; set; }
        public string? Suburb { get; set; }
        public string? State { get; set; }
        public string? PostCode { get; set; }
        public string? Country { get; set; } = "Australia";


         // Client Management
        public ClientStatus Status { get; set; } = ClientStatus.Prospect;
        public DateTime? ClientSince { get; set; }
        public DateTime? LastContactDate { get; set; }
        public DateTime? NextReviewDate { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public Guid CreatedById { get; set; }
        public User CreatedBy { get; set; } = null!;
        public Guid UpdatedById { get; set; }
        public User UpdatedBy { get; set; } = null!;

        // Associations
        public Guid BusinessId { get; set; }
        public Business Business { get; set; } = null!;
        public Guid? PrimaryAdvisorId { get; set; }
        public User? PrimaryAdvisor { get; set; }
    }

    public enum ClientStatus
    {
        Prospect,      // Not yet a client
        Active,        // Current client
        Inactive,      // No longer actively managed
        Archived       // Historical record
    }
}