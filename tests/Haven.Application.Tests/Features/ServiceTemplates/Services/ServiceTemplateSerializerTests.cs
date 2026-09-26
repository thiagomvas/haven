using System.Text;

using Haven.Application.Features.ServiceTemplates.Contracts;
using Haven.Application.Features.ServiceTemplates.Services;

using Shouldly;

using Version = Haven.Domain.ValueObjects.Version;

namespace Haven.Application.Tests.Features.ServiceTemplates.Services;

[TestFixture]
[Category("Unit")]
public class ServiceTemplateSerializerTests
{
    private ServiceTemplateSerializer _sut = null!;

    [SetUp]
    public void Setup()
    {
        _sut = new ServiceTemplateSerializer();
    }
    
    [Test]
    public async Task Deserialize_ShouldDeserializeTemplateMetadata()
    {
        var yaml = ServiceTemplateYamlExamples.CompletePostgres;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
        
        var template = await _sut.DeserializeAsync(stream);
        
        template.Id.ShouldBe("postgres");
        template.Name.ShouldBe("PostgreSQL", StringCompareShould.IgnoreCase);
        template.Icon.ShouldBe("postgres.svg", StringCompareShould.IgnoreCase);
        template.Category.ShouldBe("database", StringCompareShould.IgnoreCase);
        template.Version.ToString().ShouldBe("1.0.0");
    }
    
    [Test]
    public async Task Deserialize_ShouldDeserializeTemplateInputs()
    {
        var yaml = ServiceTemplateYamlExamples.CompletePostgres;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(yaml));
        
        var template = await _sut.DeserializeAsync(stream);
        
        template.Inputs.ShouldNotBeNull();
        template.Inputs.Count.ShouldBe(4);
        
        var versionInput = template.Inputs.FirstOrDefault(i => i.Key == "postgres_version");
        versionInput.ShouldNotBeNull();
        versionInput.Type.ShouldBe(TemplateInputFieldType.Select);
        versionInput.Options.ShouldBe(new[] { "9.6", "10", "11", "12", "13", "14", "15" });
        versionInput.Label.ShouldBe("Version");
        versionInput.Immutable.ShouldBeTrue();
        
        var userInput = template.Inputs.FirstOrDefault(i => i.Key == "postgres_user");
        userInput.ShouldNotBeNull();
        userInput.Type.ShouldBe(TemplateInputFieldType.Text);
        userInput.Label.ShouldBe("User");
        userInput.DefaultValue.ShouldBe("postgres");
        
        var passwordInput = template.Inputs.FirstOrDefault(i => i.Key == "postgres_password");
        passwordInput.ShouldNotBeNull();
        passwordInput.Type.ShouldBe(TemplateInputFieldType.Secret);
        passwordInput.Label.ShouldBe("Password");
        passwordInput.Immutable.ShouldBeTrue();
        
        var databaseInput = template.Inputs.FirstOrDefault(i => i.Key == "postgres_database");
        databaseInput.ShouldNotBeNull();
        databaseInput.Type.ShouldBe(TemplateInputFieldType.Text);
        databaseInput.Label.ShouldBe("Database");
        databaseInput.DefaultValue.ShouldBe("postgres");
    }

    [Test]
    public async Task Serialize_ShouldCreateValidYaml()
    {
        var template = new ServiceTemplate
        {
            Id = "postgres",
            Version = Version.Parse("1.0.0"),
            Name = "PostgreSQL",
            Icon = "postgres.svg",
            Category = "database",
            Inputs =
            [

                new TemplateInputField
                {
                    Key = "postgres_version",
                    Type = TemplateInputFieldType.Select,
                    Options = new[] { "9.6", "10", "11", "12", "13", "14", "15" },
                    Label = "Version",
                    Immutable = true
                },

                new TemplateInputField
                {
                    Key = "postgres_user",
                    Type = TemplateInputFieldType.Text,
                    Label = "User",
                    DefaultValue = "postgres"
                },

                new TemplateInputField
                {
                    Key = "postgres_password",
                    Type = TemplateInputFieldType.Secret,
                    Label = "Password",
                    Immutable = true
                },

                new TemplateInputField
                {
                    Key = "postgres_database",
                    Type = TemplateInputFieldType.Text,
                    Label = "Database",
                    DefaultValue = "postgres"
                }
            ]
        };

        var yaml = await _sut.SerializeAsync(template);
        
        var deserializedTemplate = await _sut.DeserializeAsync(new MemoryStream(Encoding.UTF8.GetBytes(yaml)));
        deserializedTemplate.ShouldNotBeNull();
        deserializedTemplate.Id.ShouldBe(template.Id);
        deserializedTemplate.Version.ShouldBe(template.Version);
        deserializedTemplate.Name.ShouldBe(template.Name);
        deserializedTemplate.Icon.ShouldBe(template.Icon);
        deserializedTemplate.Category.ShouldBe(template.Category);

        foreach (var deserializedInput in deserializedTemplate.Inputs)
        {
            var original = template.Inputs.FirstOrDefault(i => i.Key == deserializedInput.Key);
            original.ShouldNotBeNull();
            deserializedInput.Type.ShouldBe(original.Type);
            deserializedInput.Label.ShouldBe(original.Label);
            deserializedInput.DefaultValue.ShouldBe(original.DefaultValue);
            deserializedInput.Immutable.ShouldBe(original.Immutable);
            deserializedInput.Options.ShouldBeEquivalentTo(original.Options);
        }
    }
}