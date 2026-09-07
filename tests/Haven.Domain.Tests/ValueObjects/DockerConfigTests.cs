using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.ValueObjects;

[Category("Unit")]
public sealed class DockerConfigTests
{
    [Test]
    public void GetAcmeResolverName_NoAcmeArgs_ReturnsNull()
    {
        var config = new DockerConfig { CommandArgs = ["--api.dashboard=true"] };

        config.GetAcmeResolverName().ShouldBeNull();
    }

    [Test]
    public void GetAcmeResolverName_DefaultQuickSetupName_ReturnsLetsencrypt()
    {
        var config = new DockerConfig
        {
            CommandArgs = ["--certificatesresolvers.letsencrypt.acme.httpchallenge=true"]
        };

        config.GetAcmeResolverName().ShouldBe("letsencrypt");
    }

    [Test]
    public void GetAcmeResolverName_CustomResolverName_ReturnsConfiguredName()
    {
        var config = new DockerConfig
        {
            CommandArgs = ["--certificatesresolvers.myresolver.acme.email=admin@example.com"]
        };

        config.GetAcmeResolverName().ShouldBe("myresolver");
    }

    [Test]
    public void HasAcmeResolverConfigured_CustomResolverName_ReturnsTrue()
    {
        var config = new DockerConfig
        {
            CommandArgs = ["--certificatesresolvers.myresolver.acme.email=admin@example.com"]
        };

        config.HasAcmeResolverConfigured().ShouldBeTrue();
    }
}
