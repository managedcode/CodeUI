using System.Net;
using Microsoft.Extensions.DependencyInjection;
// TODO: Consider migrating to Aspire.Hosting.Testing for distributed application testing
// Example: using Aspire.Hosting.Testing;

namespace CodeUI.AspireTests;

public class WebApplicationTests : IClassFixture<CodeUI.Tests.TestWebApplicationFactory<CodeUI.Web.Program>>
{
    private readonly CodeUI.Tests.TestWebApplicationFactory<CodeUI.Web.Program> _factory;

    public WebApplicationTests(CodeUI.Tests.TestWebApplicationFactory<CodeUI.Web.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetWebResourceRootReturnsOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWebResourceReturnsExpectedContent()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("CodeUI", content);
    }

    [Fact]
    public async Task GetCounterPageReturnsOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/counter");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetWeatherPageReturnsOkStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/weather");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}