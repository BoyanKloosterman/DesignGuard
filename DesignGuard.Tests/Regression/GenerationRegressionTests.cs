using System.IO;
using DesignGuard.Models;
using DesignGuard.Settings;
using DesignGuard.Tests.Support;
using Xunit;

namespace DesignGuard.Tests.Regression;

/// <summary>Regressie: dreiging/eis-generatie met dezelfde regels als de app.</summary>
public sealed class GenerationRegressionTests : IDisposable
{
    private readonly string _settingsDir;

    public GenerationRegressionTests()
    {
        _settingsDir = Path.Combine(Path.GetTempPath(), "dg-genreg-" + Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_settingsDir))
                Directory.Delete(_settingsDir, true);
        }
        catch
        {
            // test cleanup
        }
    }

    [Fact]
    public void Internetblootstelling_webapp_geeft_minstens_één_dreiging()
    {
        var us = new UserSettingsService(_settingsDir);
        var kp = RegressionServiceFactory.CreateKnowledgePack(us);
        var svc = RegressionServiceFactory.CreateThreatGeneration(kp);

        var p = new ProjectModel
        {
            Name = "Regressie",
            InternetExposed = true,
            SystemType = SystemType.WebApp,
            Components =
            {
                new ComponentModel { Id = 1, Name = "Frontend", Tag = "frontend" }
            }
        };

        var threats = svc.Generate(p);
        Assert.NotEmpty(threats);
        Assert.All(threats, t => Assert.False(string.IsNullOrWhiteSpace(t.Title)));
    }

    [Fact]
    public void Zelfde_ontwerp_geeft_stabiele_rulefingerprints_voor_dreigingen()
    {
        var us = new UserSettingsService(_settingsDir);
        var kp = RegressionServiceFactory.CreateKnowledgePack(us);
        var svc = RegressionServiceFactory.CreateThreatGeneration(kp);

        var p = new ProjectModel
        {
            Name = "Stabiel",
            InternetExposed = true,
            HasAuthentication = true,
            Components =
            {
                new ComponentModel { Id = 1, Name = "API", Tag = "api" }
            }
        };

        var a = svc.Generate(p).Select(t => t.RuleFingerprint).OrderBy(x => x).ToList();
        var b = svc.Generate(p).Select(t => t.RuleFingerprint).OrderBy(x => x).ToList();
        Assert.Equal(a, b);
    }

    [Fact]
    public void Authenticatie_vlag_geeft_minstens_één_eis()
    {
        var us = new UserSettingsService(_settingsDir);
        var kp = RegressionServiceFactory.CreateKnowledgePack(us);
        var svc = RegressionServiceFactory.CreateRequirementGeneration(kp);

        var p = new ProjectModel
        {
            Name = "Auth",
            HasAuthentication = true,
            Components =
            {
                new ComponentModel { Id = 1, Name = "Svc", Tag = "api" }
            }
        };

        var reqs = svc.Generate(p);
        Assert.NotEmpty(reqs);
    }
}
