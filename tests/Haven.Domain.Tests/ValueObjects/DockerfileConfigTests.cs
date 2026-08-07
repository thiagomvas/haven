using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Domain.Tests.ValueObjects;

[Category("Unit")]
public sealed class DockerfileConfigTests
{
    [Test]
    public void DockerfileConfig_WithGitSource_SetsPropertiesCorrectly()
    {
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main"
        };

        config.Source.ShouldBe(DockerfileSource.Git);
        config.Repository.ShouldBe("https://github.com/example/repo.git");
        config.Branch.ShouldBe("main");
        config.Content.ShouldBeNull();
    }

    [Test]
    public void DockerfileConfig_WithRawSource_SetsPropertiesCorrectly()
    {
        var content = "FROM ubuntu:22.04\nRUN echo hello";
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Raw,
            Content = content
        };

        config.Source.ShouldBe(DockerfileSource.Raw);
        config.Content.ShouldBe(content);
        config.Repository.ShouldBeNull();
        config.Branch.ShouldBeNull();
    }

    [Test]
    public void DockerfileConfig_WithGitSource_CanIncludeCustomFilePath()
    {
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main",
            FilePath = "docker/Dockerfile.prod"
        };

        config.FilePath.ShouldBe("docker/Dockerfile.prod");
    }

    [Test]
    public void DockerfileConfig_WithGitSource_CanIncludeGitCredentialId()
    {
        var credentialId = Guid.NewGuid();
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main",
            GitCredentialId = credentialId
        };

        config.GitCredentialId.ShouldBe(credentialId);
    }

    [Test]
    public void DockerfileConfig_DefaultFilePath_IsNull()
    {
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main"
        };

        config.FilePath.ShouldBeNull();
    }

    [Test]
    public void DockerfileConfig_DefaultGitCredentialId_IsNull()
    {
        var config = new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main"
        };

        config.GitCredentialId.ShouldBeNull();
    }
}