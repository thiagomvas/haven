using FluentValidation.TestHelper;
using Haven.Application.Features.Services.Commands.CreateService;
using Haven.Domain;
using Haven.Domain.ValueObjects;
using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.CreateService;

[Category("Unit")]
public sealed class CreateServiceValidatorDockerfileTests
{
    private CreateServiceValidator _sut;

    [SetUp]
    public void Setup() => _sut = new CreateServiceValidator();

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenRepositoryIsMissing()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.Repository = null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Repository);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenRepositoryIsEmpty()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.Repository = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Repository);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenBranchIsMissing()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.Branch = null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Branch);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenBranchIsEmpty()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.Branch = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Branch);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldNotHaveError_WhenRepositoryAndBranchProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig!.Repository);
        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig!.Branch);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldNotHaveError_WhenOptionalFilePathProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.FilePath = "docker/Dockerfile.prod";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldNotHaveError_WhenGitCredentialIdProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig!.GitCredentialId = Guid.NewGuid();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldHaveError_WhenContentIsNull()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Raw);
        command.DockerfileConfig!.Content = null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Content);
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldHaveError_WhenContentIsEmpty()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Raw);
        command.DockerfileConfig!.Content = string.Empty;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig!.Content);
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldNotHaveError_WhenContentProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Raw);

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig!.Content);
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldNotHaveError_WhenRepositoryAlsoProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Raw);
        command.DockerfileConfig!.Repository = "https://github.com/example/repo.git";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldNotHaveError_WhenBranchAlsoProvided()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Raw);
        command.DockerfileConfig!.Branch = "main";

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_Dockerfile_ShouldHaveError_WhenDockerfileConfigIsNull()
    {
        var command = CreateDockerfileCommand(DockerfileSource.Git);
        command.DockerfileConfig = null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig);
    }

    private static CreateServiceCommand CreateDockerfileCommand(DockerfileSource source) => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        Name = "my-service",
        Type = ServiceType.Dockerfile,
        ExposureMode = ExposureMode.Internal,
        DockerfileConfig = source == DockerfileSource.Git
            ? new DockerfileConfig
            {
                Source = DockerfileSource.Git,
                Repository = "https://github.com/example/repo.git",
                Branch = "main"
            }
            : new DockerfileConfig
            {
                Source = DockerfileSource.Raw,
                Content = "FROM ubuntu:22.04\nRUN echo hello"
            }
    };
}
