
namespace CrmApi.Models.Client.Enums
{
    public enum ClientStatus
    {
        Prospect,      // Not yet a client
        Active,        // Current client
        Inactive,      // No longer actively managed
        Archived       // Historical record
    }
}