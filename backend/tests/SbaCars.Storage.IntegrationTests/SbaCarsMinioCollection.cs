namespace SbaCars.Storage.IntegrationTests;

/// <summary>
/// Shares one <see cref="SbaCars.TestKit.SbaCarsMinioFixture"/> container across every test class
/// in this assembly.
/// </summary>
[CollectionDefinition(Name)]
public sealed class SbaCarsMinioCollection : ICollectionFixture<SbaCars.TestKit.SbaCarsMinioFixture>
{
    public const string Name = "SbaCars Minio";
}
