using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Challenge.IntegrationTests;

public sealed class ApiDocumentationTests
{
    [Fact]
    public async Task Scalar_InDevelopment_ReturnsApiReferencePage()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/scalar/v1");
        var content = await response.Content.ReadAsStringAsync();

        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("Scalar", content, StringComparison.OrdinalIgnoreCase);
    }
}
