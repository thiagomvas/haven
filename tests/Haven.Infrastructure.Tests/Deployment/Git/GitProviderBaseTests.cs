using Haven.Application.Common.Models;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;
using Haven.Infrastructure.Deployment.Git;

using LibGit2Sharp;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Haven.Infrastructure.Tests.Deployment.Git;

[Category("Unit")]
public sealed class GitProviderBaseTests
{
    private ILogger<GitProviderBase> _logger = null!;

    [SetUp]
    public void Setup()
    {
        _logger = Substitute.For<ILogger<GitProviderBase>>();
    }

    private static GitCredentials TokenCredentials() =>
        GitCredentials.CreateFromToken(GitProviderType.Generic, null, "pat-value", null, "Test Creds");

    private static GitCredentials OAuthCredentials() =>
        GitCredentials.CreateFromOAuth(GitProviderType.GitHub, null, "oauth-access-token", "refresh", null, "Test Creds");

    [Test]
    public void CreateCloneOptions_WithTokenAuth_SetsCredentialsProvider()
    {
        var credentials = TokenCredentials();
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreateCloneOptions(credentials);
        var resolved = (UsernamePasswordCredentials)options.FetchOptions.CredentialsProvider!("url", null, SupportedCredentialTypes.UsernamePassword);

        resolved.Password.ShouldBe("pat-value");
    }

    [Test]
    public void CreateCloneOptions_WithOAuthAuth_SetsCredentialsProvider()
    {
        var credentials = OAuthCredentials();
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreateCloneOptions(credentials);
        var resolved = (UsernamePasswordCredentials)options.FetchOptions.CredentialsProvider!("url", null, SupportedCredentialTypes.UsernamePassword);

        resolved.Password.ShouldBe("oauth-access-token");
    }

    [Test]
    public void CreateCloneOptions_WithSshAuth_DoesNotSetCredentialsProvider()
    {
        var credentials = GitCredentials.Create(GitProviderType.Generic, null, GitAuthMethod.Ssh,
            EncryptedValue.From("ssh-key"), null, null, "Test Creds");
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreateCloneOptions(credentials);

        options.FetchOptions.CredentialsProvider.ShouldBeNull();
    }

    [Test]
    public void CreatePullOptions_WithOAuthAuth_SetsCredentialsProvider()
    {
        var credentials = OAuthCredentials();
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreatePullOptions(credentials);
        var resolved = (UsernamePasswordCredentials)options.FetchOptions.CredentialsProvider!("url", null, SupportedCredentialTypes.UsernamePassword);

        resolved.Password.ShouldBe("oauth-access-token");
    }

    [Test]
    public void CreateProxyOptions_WithOAuthAuth_SetsCredentialsProvider()
    {
        var credentials = OAuthCredentials();
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreateProxyOptions(credentials);
        var resolved = (UsernamePasswordCredentials)options.CredentialsProvider!("url", null, SupportedCredentialTypes.UsernamePassword);

        resolved.Password.ShouldBe("oauth-access-token");
    }

    [Test]
    public void CreatePushOptions_WithOAuthAuth_SetsCredentialsProvider()
    {
        var credentials = OAuthCredentials();
        var sut = new TestableGitProviderBase(credentials, _logger);

        var options = sut.CallCreatePushOptions();
        var resolved = (UsernamePasswordCredentials)options.CredentialsProvider!("url", null, SupportedCredentialTypes.UsernamePassword);

        resolved.Password.ShouldBe("oauth-access-token");
    }

    [Test]
    public void CreatePushOptions_WithNoCredentials_DoesNotSetCredentialsProvider()
    {
        var sut = new TestableGitProviderBase(null, _logger);

        var options = sut.CallCreatePushOptions();

        options.CredentialsProvider.ShouldBeNull();
    }

    private sealed class TestableGitProviderBase(GitCredentials? credentials, ILogger<GitProviderBase> logger)
        : GitProviderBase(credentials, logger)
    {
        public override GitProviderType Type => GitProviderType.Generic;

        public override Task CloneRepositoryAsync(string repositoryUrl, string destinationPath, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public override Task PullAsync(string repositoryUrl, string branch, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public override Task<IReadOnlyList<string>> GetBranchesAsync(string repositoryUrl, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<string>>([]);

        public override Task<IReadOnlyList<GitRepositorySummary>> GetAccessibleRepositoriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GitRepositorySummary>>([]);

        public CloneOptions CallCreateCloneOptions(GitCredentials? creds) => CreateCloneOptions(creds);
        public PullOptions CallCreatePullOptions(GitCredentials? creds) => CreatePullOptions(creds);
        public ProxyOptions CallCreateProxyOptions(GitCredentials? creds) => CreateProxyOptions(creds);
        public PushOptions CallCreatePushOptions() => CreatePushOptions();
    }
}