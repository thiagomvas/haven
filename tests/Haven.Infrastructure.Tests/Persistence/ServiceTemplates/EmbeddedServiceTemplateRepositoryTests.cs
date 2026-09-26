using Haven.Infrastructure.Persistence.ServiceTemplates;

using Shouldly;

namespace Haven.Infrastructure.Tests.Persistence.ServiceTemplates;

[TestFixture]
[Category("Unit")]
public class EmbeddedServiceTemplateRepositoryTests
{
    private EmbeddedServiceTemplateRepository _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new EmbeddedServiceTemplateRepository();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnBuiltInTemplates()
    {
        var templates = await _sut.GetAllAsync(CancellationToken.None);

        templates.ShouldNotBeEmpty();
        templates.ShouldContain(t => t.Id == "postgres");
        templates.ShouldContain(t => t.Id == "redis");
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnMatchingTemplate()
    {
        var template = await _sut.GetByIdAsync("postgres", CancellationToken.None);

        template.ShouldNotBeNull();
        template.Name.ShouldBe("PostgreSQL");
        template.Inputs.ShouldNotBeEmpty();
    }

    [Test]
    public async Task GetByIdAsync_ShouldReturnNull_WhenTemplateDoesNotExist()
    {
        var template = await _sut.GetByIdAsync("does-not-exist", CancellationToken.None);

        template.ShouldBeNull();
    }
}
