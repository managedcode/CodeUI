using Microsoft.Extensions.DependencyInjection;

namespace CodeUI.Tests;

public class WebApplicationFactoryTests : IClassFixture<TestWebApplicationFactory<CodeUI.Web.Program>>
{
    private readonly TestWebApplicationFactory<CodeUI.Web.Program> _factory;

    public WebApplicationFactoryTests(TestWebApplicationFactory<CodeUI.Web.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_EndpointsReturnSuccessAndCorrectContentType()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        Assert.Equal("text/html; charset=utf-8", 
            response.Content.Headers.ContentType?.ToString());
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Counter")]
    [InlineData("/Weather")]
    public async Task Get_EndpointsReturnSuccess(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
    }
}