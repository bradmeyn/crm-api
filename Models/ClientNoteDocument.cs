

using CrmApi.Models;

public class ClientNoteDocument: BaseEntity
{
    public required string FileName { get; set; } 
    public required string BlobName { get; set; } 
    public required string ContentType { get; set; }
    public long FileSize { get; set; }
    public Guid NoteId {get; set;}
    public Note Note {get; set;} = null!;

}