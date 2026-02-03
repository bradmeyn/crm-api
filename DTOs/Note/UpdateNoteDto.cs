using System.Text.Json.Serialization;

namespace CrmApi.DTOs.Note;

public class UpdateNoteDto
{

    [JsonPropertyName("id")]
    public Guid id {get; set;}
    [JsonPropertyName("clientId")]
    public Guid ClientId {get; set;}

    [JsonPropertyName("type")]
    public string Type {get; set;} = string.Empty;
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}