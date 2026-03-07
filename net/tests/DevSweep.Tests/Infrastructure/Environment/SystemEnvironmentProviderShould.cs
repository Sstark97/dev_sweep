using AwesomeAssertions;
using DevSweep.Domain.Enums;
using DevSweep.Infrastructure.Environment;

namespace DevSweep.Tests.Infrastructure.Environment;

internal sealed class SystemEnvironmentProviderShould
{
    private readonly SystemEnvironmentProvider provider = new();

    [Test]
    public void DetectCurrentOperatingSystem()
    {
        var operatingSystem = provider.CurrentOperatingSystem;

        Enum.IsDefined(operatingSystem).Should().BeTrue();
    }

    [Test]
    public void ProvideHomePathThatExists()
    {
        var homePath = provider.HomePath.ToString();

        homePath.Should().NotBeEmpty();
        Directory.Exists(homePath).Should().BeTrue();
    }

    [Test]
    public void ProvideJetBrainsBasePathWithJetBrainsSegment()
    {
        var path = provider.JetBrainsBasePath().ToString();

        path.Should().NotBeEmpty();
        path.Should().Contain("JetBrains");
    }

    [Test]
    public void ProvideDockerConfigPathWithDockerSegment()
    {
        var path = provider.DockerConfigPath().ToString();

        path.Should().NotBeEmpty();
        path.Should().Contain(".docker");
    }

    [Test]
    public void ProvideMavenRepositoryPathWithM2Segment()
    {
        var path = provider.MavenRepositoryPath().ToString();

        path.Should().NotBeEmpty();
        path.Should().Contain(".m2");
    }

    [Test]
    public void ProvideGradleCachePathWithGradleSegment()
    {
        var path = provider.GradleCachePath().ToString();

        path.Should().NotBeEmpty();
        path.Should().Contain(".gradle");
    }

    [Test]
    public void ProvideNodeModulesGlobalPath()
    {
        var path = provider.NodeModulesGlobalPath().ToString();

        path.Should().NotBeEmpty();
    }

    [Test]
    public void ProvidePythonCachePath()
    {
        var path = provider.PythonCachePath().ToString();

        path.Should().NotBeEmpty();
    }

    [Test]
    public void ProvideSdkmanPathWithSdkmanSegment()
    {
        var path = provider.SdkmanPath().ToString();

        path.Should().NotBeEmpty();
        path.Should().Contain(".sdkman");
    }

    [Test]
    public void ProvideHomebrewCachePath()
    {
        var path = provider.HomebrewCachePath().ToString();

        path.Should().NotBeEmpty();
    }

    [Test]
    public void ProvideSystemTempPath()
    {
        var path = provider.SystemTempPath().ToString();

        path.Should().NotBeEmpty();
    }

    [Test]
    public void ProvideSystemLogsPath()
    {
        var path = provider.SystemLogsPath().ToString();

        path.Should().NotBeEmpty();
    }

    [Test]
    public void ProvideSystemCachePath()
    {
        var path = provider.SystemCachePath().ToString();

        path.Should().NotBeEmpty();
    }
}
