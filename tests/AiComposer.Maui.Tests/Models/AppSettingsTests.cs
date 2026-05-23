using AiComposer.Maui.Models;

namespace AiComposer.Maui.Tests.Models;

public sealed class AppSettingsTests
{
    [Fact]
    public void DefaultAiProvider_IsOpenAi()
    {
        var settings = new AppSettings();
        Assert.Equal("openai", settings.AiProvider);
    }

    [Fact]
    public void DefaultApiKey_IsEmpty()
    {
        var settings = new AppSettings();
        Assert.Equal(string.Empty, settings.ApiKey);
    }

    [Fact]
    public void DefaultAutoApprove_IsFalse()
    {
        var settings = new AppSettings();
        Assert.False(settings.AutoApprove);
    }

    [Fact]
    public void DefaultTimeoutMinutes_IsThirty()
    {
        var settings = new AppSettings();
        Assert.Equal(30, settings.TimeoutMinutes);
    }

    [Fact]
    public void DefaultMaxRetries_IsThree()
    {
        var settings = new AppSettings();
        Assert.Equal(3, settings.MaxRetries);
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
    public void ApiKey_CanBeSetAndRetrieved()
    {
        var settings = new AppSettings { ApiKey = "sk-test-key-123" };
        Assert.Equal("sk-test-key-123", settings.ApiKey);
    }

    [Fact]
    public void AutoApprove_CanBeSetToTrue()
    {
        var settings = new AppSettings { AutoApprove = true };
        Assert.True(settings.AutoApprove);
    }

    [Fact]
    public void TimeoutMinutes_CanBeSetToCustomValue()
    {
        var settings = new AppSettings { TimeoutMinutes = 60 };
        Assert.Equal(60, settings.TimeoutMinutes);
    }

    [Fact]
    public void MaxRetries_CanBeSetToZero()
    {
        var settings = new AppSettings { MaxRetries = 0 };
        Assert.Equal(0, settings.MaxRetries);
    }

    [Fact]
    public void MaxRetries_CanBeSetToCustomValue()
    {
        var settings = new AppSettings { MaxRetries = 5 };
        Assert.Equal(5, settings.MaxRetries);
    }

    [Fact]
    public void AiProvider_CanBeChanged()
    {
        var settings = new AppSettings { AiProvider = "anthropic" };
        Assert.Equal("anthropic", settings.AiProvider);
    }

    [Fact]
    public void AllNewProperties_CanBeSetTogether()
    {
        var settings = new AppSettings
        {
            ApiKey = "my-api-key",
            AutoApprove = true,
            TimeoutMinutes = 45,
            MaxRetries = 2,
            AiProvider = "azure-openai",
        };

        Assert.Equal("my-api-key", settings.ApiKey);
        Assert.True(settings.AutoApprove);
        Assert.Equal(45, settings.TimeoutMinutes);
        Assert.Equal(2, settings.MaxRetries);
        Assert.Equal("azure-openai", settings.AiProvider);
    }
}