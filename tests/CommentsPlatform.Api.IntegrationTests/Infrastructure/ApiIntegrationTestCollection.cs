namespace CommentsPlatform.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiIntegrationTestCollection
    : ICollectionFixture<CommentsPlatformWebApplicationFactory>
{
    public const string Name = "API integration tests";
}
