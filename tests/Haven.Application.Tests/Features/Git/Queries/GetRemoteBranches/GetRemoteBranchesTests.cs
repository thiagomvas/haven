using FluentValidation.TestHelper;

using Haven.Application.Common;
using Haven.Application.Common.Interfaces.Deployment;
using Haven.Application.Common.Interfaces.Repositories;
using Haven.Application.Features.Git.Queries.GetRemoteBranches;
using Haven.Domain;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using NSubstitute;

using Shouldly;

namespace Haven.Application.Tests.Features.Git.Queries.GetRemoteBranches;

[Category("Unit")]
public sealed class GetRemoteBranchesTests
{
    private GetRemoteBranchesValidator _validator;
    private IGitCredentialsRepository _gitCredentialsRepository;
    private IGitService _gitService;
    private GetRemoteBranchesHandler _handler;

    [SetUp]
    public void Setup()
    {
        _validator = new GetRemoteBranchesValidator();
        _gitCredentialsRepository = Substitute.For<IGitCredentialsRepository>();
        _gitService = Substitute.For<IGitService>();
        _handler = new GetRemoteBranchesHandler(_gitService, _gitCredentialsRepository);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenRepositoryUrlIsEmpty()
    {
        var query = new GetRemoteBranchesQuery { RepositoryUrl = string.Empty };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.RepositoryUrl);
    }

    [Test]
    public void Validator_ShouldHaveError_WhenRepositoryUrlIsNotValidUri()
    {
        var query = new GetRemoteBranchesQuery { RepositoryUrl = "not-a-valid-uri" };

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.RepositoryUrl);
    }

    [Test]
    public void Validator_ShouldNotHaveError_WhenRepositoryUrlIsValid()
    {
        var query = new GetRemoteBranchesQuery { RepositoryUrl = "https://github.com/example/repo.git" };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.RepositoryUrl);
    }

    [Test]
    public void Validator_ShouldNotHaveError_WhenGitCredentialIdIsNotProvided()
    {
        var query = new GetRemoteBranchesQuery { RepositoryUrl = "https://github.com/example/repo.git" };

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public async Task Handler_ShouldReturnSuccess_WhenNoCredentialsProvided()
    {
        var query = new GetRemoteBranchesQuery { RepositoryUrl = "https://github.com/example/repo.git" };

        _gitService.GetRemoteBranchesAsync(query.RepositoryUrl, null, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success([]));

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Test]
    public async Task Handler_ShouldReturnNotFound_WhenCredentialIdDoesNotExist()
    {
        var credentialId = Guid.NewGuid();
        var query = new GetRemoteBranchesQuery
        {
            RepositoryUrl = "https://github.com/example/repo.git",
            GitCredentialId = credentialId
        };

        _gitCredentialsRepository.FindByIdAsync(credentialId, Arg.Any<CancellationToken>())
            .Returns((GitCredentials?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.IsFailure.ShouldBeTrue();
    }

    [Test]
    public async Task Handler_WithValidCredentialId_ShouldFetchCredentialsFromRepository()
    {
        var credentialId = Guid.NewGuid();
        var credentials = GitCredentials.CreateFromToken(GitProviderType.Generic, null, "my-token", null, "My Creds");
        var query = new GetRemoteBranchesQuery
        {
            RepositoryUrl = "https://github.com/example/repo.git",
            GitCredentialId = credentialId
        };

        _gitCredentialsRepository.FindByIdAsync(credentialId, Arg.Any<CancellationToken>())
            .Returns(credentials);

        _gitService.GetRemoteBranchesAsync(query.RepositoryUrl, credentials, Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<string>>.Success(Array.Empty<string>()));

        var result = await _handler.Handle(query, CancellationToken.None);

        await _gitCredentialsRepository.Received(1).FindByIdAsync(credentialId, Arg.Any<CancellationToken>());
        result.IsSuccess.ShouldBeTrue();
    }
}