namespace cloud.api
{
    public sealed class LoginDto
    {
        public required string Username { get; init; }
        public required string Password { get; init; }
    }
}