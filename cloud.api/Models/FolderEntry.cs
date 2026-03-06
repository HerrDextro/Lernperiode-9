namespace cloud.api.Models
{
    public record FolderEntry(
    string Id,
    string Name,
    long Size,
    string VirtualPath,
    string OwnerId,
    bool IsPublic,
    string[] AllowedEntities,
    string? MimeType = null
    );
}

