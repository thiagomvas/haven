using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.UpdateService;
using Haven.Domain;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Application.Tests.Features.Services.Commands.UpdateService;

[Category("Unit")]
public sealed class UpdateServiceValidatorDockerfileTests
{
    private UpdateServiceValidator _sut;

    [SetUp]
    public void Setup() => _sut = new UpdateServiceValidator();

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenRepositoryIsEmpty()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = string.Empty,
            Branch = "main"
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Repository);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldHaveError_WhenBranchIsEmpty()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = string.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Branch);
    }

    [Test]
    public void Validate_DockerfileGitSource_ShouldNotHaveError_WhenRepositoryAndBranchProvided()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Git,
            Repository = "https://github.com/example/repo.git",
            Branch = "main"
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Repository);
        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Branch);
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldHaveError_WhenContentIsEmpty()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Raw,
            Content = string.Empty
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Content);
    }

    [Test]
    public void Validate_DockerfileRawSource_ShouldNotHaveError_WhenContentProvided()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Raw,
            Content = "FROM ubuntu:22.04"
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.DockerfileConfig.Value!.Content);
    }

    [Test]
    public void Validate_DockerfileConfig_NotProvided_ShouldNotHaveError()
    {
        var command = CreateMinimalCommand();

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Validate_DockerfileTypeWithoutConfig_ShouldHaveError()
    {
        var command = CreateMinimalCommand();
        command.Type = (Optional<ServiceType>)ServiceType.Dockerfile;
        command.DockerfileConfig = (Optional<DockerfileConfig?>)(DockerfileConfig?)null;

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Type);
    }

    [Test]
    public void Validate_Dockerfile_ShouldHaveError_WhenCommandArgContainsEmptyString()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Raw,
            Content = "FROM ubuntu:22.04",
            CommandArgs = ["--foo=bar", ""]
        };

        var result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor("DockerfileConfig.Value.CommandArgs[1]");
    }

    [Test]
    public void Validate_Dockerfile_ShouldNotHaveError_WhenCommandArgsAreValid()
    {
        var command = CreateMinimalCommand();
        command.DockerfileConfig = (Optional<DockerfileConfig?>)new DockerfileConfig
        {
            Source = DockerfileSource.Raw,
            Content = "FROM ubuntu:22.04",
            CommandArgs = ["--foo=bar"]
        };

        var result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static UpdateServiceCommand CreateMinimalCommand() => new()
    {
        ProjectId = Guid.NewGuid(),
        EnvironmentId = Guid.NewGuid(),
        ServiceId = Guid.NewGuid()
    };
}