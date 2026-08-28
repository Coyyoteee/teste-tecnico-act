using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Challenge.UnitTests.TestDoubles;

internal sealed class TestHostEnvironment(string contentRootPath) : IHostEnvironment
{
    public string EnvironmentName { get; set; } = Environments.Development;
    public string ApplicationName { get; set; } = "Challenge.UnitTests";
    public string ContentRootPath { get; set; } = contentRootPath;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
