using Haven.Application.Mappers;
using Haven.Domain.Aggregates;
using Haven.Domain.Entities;
using Haven.Domain.Enums;
using Haven.Domain.ValueObjects;

using Shouldly;

namespace Haven.Application.Tests.Mappers;

[Category("Unit")]
public sealed class CustomActionManifestMapperTests
{
    private static readonly Guid ServiceId = Guid.NewGuid();

    private static CustomAction NewExecAction() => CustomAction.Create(
        ServiceId, "Run migrations", "migrate", "Applies pending migrations", "database",
        new ExecActionConfig(["dotnet", "ef", "database", "update"], "/app", "root", ShellType.Bash),
        ["deploy", "admin"], ActionRisk.RequireConfirmation, TimeSpan.FromSeconds(90),
        [
            new CustomActionInput("target", "Target", "Migration target", true, "latest"),
            new CustomActionInput("verbose", "Verbose")
        ]);

    private static CustomAction NewHttpAction() => CustomAction.Create(
        ServiceId, "Ping", "ping", "Pings the service", "globe",
        new HttpActionConfig(HttpMethod.Post, "http://svc/health",
            new Dictionary<string, string> { ["X-Key"] = "abc", ["Accept"] = "json" }, "{\"a\":1}", [200, 204]),
        [], ActionRisk.Safe, TimeSpan.FromMinutes(2));

    [Test]
    public void ToManifest_ExecAction_MapsAllFields()
    {
        var action = NewExecAction();

        var manifest = action.ToManifest();

        manifest.Id.ShouldBe(action.Id);
        manifest.ActionName.ShouldBe("Run migrations");
        manifest.Alias.ShouldBe("migrate");
        manifest.ActionDescription.ShouldBe("Applies pending migrations");
        manifest.Icon.ShouldBe("database");
        manifest.RequiredPermissions.ShouldBe(["deploy", "admin"]);
        manifest.Risk.ShouldBe(ActionRisk.RequireConfirmation);
        manifest.Timeout.ShouldBe(TimeSpan.FromSeconds(90));
        manifest.Config.Type.ShouldBe("exec");
        manifest.Config.Command.ShouldBe(["dotnet", "ef", "database", "update"]);
        manifest.Config.WorkingDir.ShouldBe("/app");
        manifest.Config.User.ShouldBe("root");
        manifest.Config.Shell.ShouldBe(ShellType.Bash);
        manifest.Inputs.Count.ShouldBe(2);
        manifest.Inputs[0].Name.ShouldBe("target");
        manifest.Inputs[0].Label.ShouldBe("Target");
        manifest.Inputs[0].Description.ShouldBe("Migration target");
        manifest.Inputs[0].Required.ShouldBeTrue();
        manifest.Inputs[0].DefaultValue.ShouldBe("latest");
    }

    [Test]
    public void ToManifest_HttpAction_MapsAllFields()
    {
        var manifest = NewHttpAction().ToManifest();

        manifest.Config.Type.ShouldBe("http");
        manifest.Config.Method.ShouldBe("POST");
        manifest.Config.Url.ShouldBe("http://svc/health");
        manifest.Config.Headers.ShouldNotBeNull();
        manifest.Config.Headers["X-Key"].ShouldBe("abc");
        manifest.Config.Headers["Accept"].ShouldBe("json");
        manifest.Config.Body.ShouldBe("{\"a\":1}");
        manifest.Config.SuccessStatusCodes.ShouldBe([200, 204]);
    }

    [Test]
    public void ToManifest_DoesNotExposeToken()
    {
        typeof(Haven.Application.Features.Services.CustomActionManifest).GetProperty("Token").ShouldBeNull();
    }

    [Test]
    public void RoundTrip_ExecAction_PreservesEverythingExceptToken()
    {
        var original = NewExecAction();

        var restored = original.ToManifest().ToEntity(ServiceId);

        restored.Id.ShouldBe(original.Id);
        restored.ServiceId.ShouldBe(ServiceId);
        restored.ActionName.ShouldBe(original.ActionName);
        restored.Alias.ShouldBe(original.Alias);
        restored.ActionDescription.ShouldBe(original.ActionDescription);
        restored.Icon.ShouldBe(original.Icon);
        restored.RequiredPermissions.ShouldBe(original.RequiredPermissions);
        restored.Risk.ShouldBe(original.Risk);
        restored.Timeout.ShouldBe(original.Timeout);
        restored.Inputs.ShouldBe(original.Inputs);
        var config = restored.Config.ShouldBeOfType<ExecActionConfig>();
        var expected = (ExecActionConfig)original.Config;
        config.Command.ShouldBe(expected.Command);
        config.WorkingDir.ShouldBe(expected.WorkingDir);
        config.User.ShouldBe(expected.User);
        config.Shell.ShouldBe(expected.Shell);
        restored.Token.ShouldStartWith("hca_");
        restored.Token.ShouldNotBe(original.Token);
    }

    [Test]
    public void RoundTrip_HttpAction_PreservesEverything()
    {
        var original = NewHttpAction();

        var restored = original.ToManifest().ToEntity(ServiceId);

        var config = restored.Config.ShouldBeOfType<HttpActionConfig>();
        var expected = (HttpActionConfig)original.Config;
        config.Method.ShouldBe(expected.Method);
        config.Url.ShouldBe(expected.Url);
        config.Headers.ShouldBe(expected.Headers);
        config.Body.ShouldBe(expected.Body);
        config.SuccessStatusCodes.ShouldBe(expected.SuccessStatusCodes);
        restored.Inputs.ShouldBeEmpty();
        restored.RequiredPermissions.ShouldBeEmpty();
    }

    [Test]
    public void ToEntity_EmptyId_GeneratesNewId()
    {
        var manifest = NewExecAction().ToManifest();
        manifest.Id = Guid.Empty;

        manifest.ToEntity(ServiceId).Id.ShouldNotBe(Guid.Empty);
    }

    [Test]
    public void ToEntity_UnknownConfigType_Throws()
    {
        var manifest = NewExecAction().ToManifest();
        manifest.Config.Type = "bogus";

        Should.Throw<InvalidOperationException>(() => manifest.ToEntity(ServiceId));
    }

    [Test]
    public void ServiceRoundTrip_PreservesCustomActions()
    {
        var service = Service.Create(Guid.NewGuid(), "api", ServiceType.DockerImage, ExposureMode.None,
            sourceConfig: new DockerConfig { Image = "nginx" });
        service.CustomActions = [NewExecAction(), NewHttpAction()];
        var environment = Haven.Domain.Aggregates.Environment.Create(Guid.NewGuid(), "dev");

        var manifest = service.ToManifest();
        var restored = manifest.ToEntity(environment);

        manifest.CustomActions.Count.ShouldBe(2);
        restored.CustomActions.Select(a => a.ActionName).ShouldBe(["Run migrations", "Ping"]);
        restored.CustomActions.ShouldAllBe(a => a.ServiceId == restored.Id);
    }

    [Test]
    public void ServiceToEntity_WithoutCustomActions_YieldsEmpty()
    {
        var service = Service.Create(Guid.NewGuid(), "api", ServiceType.DockerImage, ExposureMode.None,
            sourceConfig: new DockerConfig { Image = "nginx" });
        var environment = Haven.Domain.Aggregates.Environment.Create(Guid.NewGuid(), "dev");

        service.ToManifest().ToEntity(environment).CustomActions.ShouldBeEmpty();
    }
}