using AiComposer.Maui.Models;

namespace AiComposer.Maui.Tests.Models;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultAiProvider_IsDeepSeek()
    {
        var settings = new AppSettings();
        Assert.Equal("deepseek", settings.AiProvider);
    }

    [Fact]
    public void DefaultWorkspacePath_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal(string.Empty, settings.WorkspacePath);
    }

    [Fact]
    public void DefaultOutputPath_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal(string.Empty, settings.OutputPath);
    }

    [Fact]
    public void DefaultAiModel_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal(string.Empty, settings.AiModel);
    }

    [Fact]
    public void AiProvider_CanBeChanged()
    {
        var settings = new AppSettings { AiProvider = "copilot" };
        Assert.Equal("copilot", settings.AiProvider);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var settings = new AppSettings
        {
            AiProvider = "copilot",
            AiModel = "gpt-5",
            WorkspacePath = "/ws",
            OutputPath = "/out",
            CliExecutablePath = "/cli",
            LiveOutput = false,
        };

        Assert.Equal("copilot", settings.AiProvider);
        Assert.Equal("gpt-5", settings.AiModel);
        Assert.Equal("/ws", settings.WorkspacePath);
        Assert.Equal("/out", settings.OutputPath);
        Assert.Equal("/cli", settings.CliExecutablePath);
        Assert.False(settings.LiveOutput);
    }
}