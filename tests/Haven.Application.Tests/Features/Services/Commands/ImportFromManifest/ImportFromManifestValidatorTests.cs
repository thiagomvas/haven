using FluentValidation.TestHelper;

using Haven.Application.Features.Services.Commands.ImportFromManifest;

namespace Haven.Application.Tests.Features.Services.Commands.ImportFromManifest;

[Category("Unit")]
public sealed class ImportFromManifestValidatorTests
{
    private ImportFromManifestValidator _sut;

    [SetUp]
    public void Setup() => _sut = new ImportFromManifestValidator();

    [Test]
    public void Validate_ShouldNotHaveError_WhenRawManifestIsNull()
    {
        var result = _sut.TestValidate(CreateCommand(null));

        result.ShouldNotHaveValidationErrorFor(x => x.RawManifest);
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Validate_ShouldHaveError_WhenRawManifestIsEmptyOrWhitespace(string manifest)
    {
        var result = _sut.TestValidate(CreateCommand(manifest));

        result.ShouldHaveValidationErrorFor(x => x.RawManifest);
    }

    [Test]
    public void Validate_ShouldNotHaveError_WhenRawManifestHasContent()
    {
        var result = _sut.TestValidate(CreateCommand("name: web"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static ImportFromManifestCommand CreateCommand(string? rawManifest) => new()
    {
        EnvironmentId = Guid.NewGuid(),
        RawManifest = rawManifest,
    };
}