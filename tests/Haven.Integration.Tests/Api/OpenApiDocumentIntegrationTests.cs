using System.Net;

using Haven.Integration.Tests.Common;

using Shouldly;

namespace Haven.Integration.Tests.Api;

[TestFixture]
[Category("Integration")]
public class OpenApiDocumentIntegrationTests
{
    private IntegrationTestFixture _fixture = null!;

    [SetUp]
    public async Task SetUp()
    {
        _fixture = new IntegrationTestFixture();
        await _fixture.InitializeAsync();
    }

    [TearDown]
    public void TearDown() => _fixture?.Dispose();

    // Scalar's client requests the document at /openapi/v1.json. Generation also fails if any
    // endpoint/DTO can't be described.
    [Test]
    public async Task ScalarOpenApiDocument_IsServedAndGenerates()
    {
        var response = await _fixture.Client.GetAsync("/openapi/v1.json");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK, body);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
    }
}
