namespace @RepositoryOwner.@RepositoryName.Configuration
{
    public sealed record @RepositoryNameConfiguration
    {
        public required DiscordConfiguration Discord { get; init; }
        public LoggerConfiguration Logger { get; init; } = new();
    }
}
