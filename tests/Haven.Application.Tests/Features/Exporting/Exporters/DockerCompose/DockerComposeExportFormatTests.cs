using Haven.Application.Features.Exporting;
using Haven.Application.Features.Exporting.Exporters.DockerCompose;

using Shouldly;

namespace Haven.Application.Tests.Features.Exporting.Exporters.DockerCompose;

[Category("Unit")]
public sealed class DockerComposeExportFormatTests
{
    [Test]
    public void ExportService_SerializesRuntimeSettingsWithComposeKeyNames()
    {
        var model = new ServiceExportModel
        {
            Name = "web",
            Image = "nginx",
            Ports = ["8080:80"],
            Command = ["--flag"],
            Restart = "unless-stopped",
            MemoryLimit = 1024,
            Cpus = 0.5m,
            SecretKeys = ["TOKEN"]
        };

        var yaml = new DockerComposeExportFormat().ExportService(model);

        TestContext.Out.WriteLine(yaml);
        yaml.ShouldContain("mem_limit: 1024");
        yaml.ShouldContain("restart: unless-stopped");
        yaml.ShouldContain("cpus: 0.5");
        yaml.ShouldContain("TOKEN: ${TOKEN}");
        yaml.ShouldMatch("""(?m)^\s+- ['"]8080:80['"]$""");
    }
}
