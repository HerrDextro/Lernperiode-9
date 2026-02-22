namespace cloud.api
{
    public record FileEntryDto(
    string Id,
    string Name,
    long Size,
    string Path,
    bool IsPublic,
    string OwnerId
    );
}
