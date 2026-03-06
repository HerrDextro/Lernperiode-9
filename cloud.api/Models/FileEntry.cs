namespace cloud.api.Models
{
    public record FileEntry(
        string Id,
        string Name,
        long Size,
        string VirtualPath,
        bool IsPublic,
        string OwnerId,
        List<Dtos.ACLEntryDto> AllowedIdentities,
        string? MimeType = null
    );
}
