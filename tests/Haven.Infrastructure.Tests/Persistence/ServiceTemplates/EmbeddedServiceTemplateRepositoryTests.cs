using Haven.Application.Common.Templating;
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

    [TestCase("postgres")]
    [TestCase("redis")]
    [TestCase("rabbitmq")]
    [TestCase("mariadb")]
    [TestCase("mysql")]
    [TestCase("loki")]
    [TestCase("grafana")]
    public async Task GetByIdAsync_Outputs_OnlyReferenceKnownEnvKeysAndDeclaredPort(string templateId)
    {
        var template = await _sut.GetByIdAsync(templateId, CancellationToken.None);

        template.ShouldNotBeNull();
        template.Outputs.ShouldNotBeEmpty();

        var knownEnvKeys = template.Container.Env.Keys.ToHashSet();

        foreach (var output in template.Outputs)
        {
            foreach (var envKey in TemplateExpressionResolver.FindKeys(output.Value, "env"))
                knownEnvKeys.ShouldContain(envKey, $"Output '{output.Key}' references unknown env key '{envKey}'.");

            if (TemplateExpressionResolver.FindKeys(output.Value, "container").Contains("port"))
                template.Container.Port.ShouldNotBeNull($"Output '{output.Key}' references container.port, but the template does not declare one.");
        }
    }
}
