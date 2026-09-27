using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.Exceptions;

using Shouldly;

namespace Haven.Domain.Tests.Entities;

[TestFixture]
[Category("Unit")]
public sealed class ServiceComputedPropertyTests
{
    [Test]
    public void Create_SetsFieldsAndTrims()
    {
        var serviceId = Guid.NewGuid();

        var property = ServiceComputedProperty.Create(
            serviceId,
            " connection_string ",
            " Connection String ",
            " postgresql://${{ env.USER }}@${{ runtime.host }} ",
            isSecret: true);

        property.ServiceId.ShouldBe(serviceId);
        property.Key.ShouldBe("connection_string");
        property.Label.ShouldBe("Connection String");
        property.Template.ShouldBe("postgresql://${{ env.USER }}@${{ runtime.host }}");
        property.IsSecret.ShouldBeTrue();
    }

    [TestCase("")]
    [TestCase("  ")]
    [TestCase("Connection-String")]
    [TestCase("CONNECTION_STRING")]
    public void Create_InvalidKey_Throws(string key)
    {
        Should.Throw<ValidationException>(() =>
            ServiceComputedProperty.Create(Guid.NewGuid(), key, "Label", "template", false));
    }

    [Test]
    public void Create_EmptyLabel_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceComputedProperty.Create(Guid.NewGuid(), "key", " ", "template", false));
    }

    [Test]
    public void Create_EmptyTemplate_Throws()
    {
        Should.Throw<ValidationException>(() =>
            ServiceComputedProperty.Create(Guid.NewGuid(), "key", "Label", " ", false));
    }

    [Test]
    public void Service_AddComputedProperty_AddsToCollection()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);

        var property = service.AddComputedProperty("connection_string", "Connection String", "postgresql://...", true);

        service.ComputedProperties.ShouldContain(property);
    }

    [Test]
    public void Service_AddComputedProperty_DuplicateKey_Throws()
    {
        var service = Service.Create(Guid.NewGuid(), "test-service", ServiceType.DockerImage, ExposureMode.None);
        service.AddComputedProperty("connection_string", "Connection String", "postgresql://...", true);

        Should.Throw<ValidationException>(() =>
            service.AddComputedProperty("connection_string", "Other Label", "other", false));
    }
}
