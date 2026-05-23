using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;
using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Tests.ViewModels;

/// <summary>Unit tests for <see cref="SettingsViewModel"/>.</summary>
public sealed class SettingsViewModelTests
{
    // ---------------------------------------------------------------------------
    // Fakes
    // ---------------------------------------------------------------------------

    private sealed class FakeSettingsService : ISettingsService
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public AppSettings? Saved { get; private set; }

        public Task<AppSettings> LoadAsync(CancellationToken ct = default) =>
            Task.FromResult(Settings);

        public Task SaveAsync(AppSettings settings, CancellationToken ct = default)
        {
            Saved = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWorkspaceService : IWorkspaceService
    {
        public string? PathToReturn { get; set; }
        public bool WasCalled { get; private set; }

        public string? CurrentWorkspacePath => PathToReturn;

        public Task<string?> OpenWorkspaceAsync()
        {
            WasCalled = true;
            return Task.FromResult(PathToReturn);
        }

        public void SetWorkspacePath(string path) => PathToReturn = path;
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static (SettingsViewModel vm, FakeSettingsService svc, FakeWorkspaceService ws)
        Create(AppSettings? initialSettings = null)
    {
        var svc = new FakeSettingsService();
        if (initialSettings is not null)
            svc.Settings = initialSettings;

        var ws = new FakeWorkspaceService();
        // Constructor calls LoadSettingsAsync internally; the task completes synchronously
        // in our fake because it returns Task.FromResult.
        var vm = new SettingsViewModel(svc, ws);
        // Drain the constructor-triggered load.
        Task.Run(() => vm.LoadSettingsCommand.ExecuteAsync(null)).GetAwaiter().GetResult();
        return (vm, svc, ws);
    }

    // ---------------------------------------------------------------------------
    // Providers list
    // ---------------------------------------------------------------------------

    [Fact]
    public void Providers_ContainsFourExpectedEntries()
    {
        var (vm, _, _) = Create();

        Assert.Equal(4, vm.Providers.Count);
        Assert.Contains("OpenAI", vm.Providers);
        Assert.Contains("Azure OpenAI", vm.Providers);
        Assert.Contains("Anthropic", vm.Providers);
        Assert.Contains("Local", vm.Providers);
    }

    [Fact]
    public void Providers_OrderIsOpenAiFirst()
    {
        var (vm, _, _) = Create();
        Assert.Equal("OpenAI", vm.Providers[0]);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — field mapping
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadSettings_MapsWorkspacePath()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/ws" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("/ws", vm.WorkspacePath);
    }

    [Fact]
    public async Task LoadSettings_MapsOutputPath()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { OutputPath = "/out" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("/out", vm.OutputPath);
    }

    [Fact]
    public async Task LoadSettings_MapsApiKey()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { ApiKey = "sk-abc" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("sk-abc", vm.ApiKey);
    }

    [Fact]
    public async Task LoadSettings_MapsAutoApprove()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { AutoApprove = true } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.True(vm.AutoApprove);
    }

    [Fact]
    public async Task LoadSettings_MapsModel()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { AiModel = "gpt-5" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("gpt-5", vm.Model);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — provider label conversion
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("openai", "OpenAI")]
    [InlineData("azure-openai", "Azure OpenAI")]
    [InlineData("anthropic", "Anthropic")]
    [InlineData("local", "Local")]
    [InlineData("unknown-provider", "OpenAI")] // fallback
    [InlineData("", "OpenAI")]                 // empty string fallback
    [InlineData("OPENAI", "OpenAI")]           // case-insensitive
    [InlineData("OpenAI", "OpenAI")]           // mixed case
    public async Task LoadSettings_ConvertsAiProviderToLabel(string rawProvider, string expectedLabel)
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { AiProvider = rawProvider } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal(expectedLabel, vm.SelectedProvider);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — clamping
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData(0, 1)]    // zero clamped to 1
    [InlineData(-1, 1)]   // negative clamped to 1
    [InlineData(1, 1)]    // boundary — exactly 1 kept
    [InlineData(60, 60)]  // normal value unchanged
    public async Task LoadSettings_ClampsTimeoutMinutes(int storedValue, int expectedValue)
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { TimeoutMinutes = storedValue } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal(expectedValue, vm.TimeoutMinutes);
    }

    [Theory]
    [InlineData(-1, 0)]  // negative clamped to 0
    [InlineData(0, 0)]   // zero is allowed
    [InlineData(3, 3)]   // normal value unchanged
    public async Task LoadSettings_ClampsMaxRetries(int storedValue, int expectedValue)
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { MaxRetries = storedValue } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal(expectedValue, vm.MaxRetries);
    }

    [Fact]
    public async Task LoadSettings_ResetsSavedConfirmation()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        // Simulate a previous save confirmation being visible.
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasSavedConfirmation);

        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.False(vm.HasSavedConfirmation);
    }

    // ---------------------------------------------------------------------------
    // SaveAsync — provider value conversion
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("OpenAI", "openai")]
    [InlineData("Azure OpenAI", "azure-openai")]
    [InlineData("Anthropic", "anthropic")]
    [InlineData("Local", "local")]
    [InlineData("SomethingUnknown", "openai")] // fallback
    public async Task Save_ConvertsSelectedProviderToRawValue(string label, string expectedValue)
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.SelectedProvider = label;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(svc.Saved);
        Assert.Equal(expectedValue, svc.Saved!.AiProvider);
    }

    [Fact]
    public async Task Save_PersistsWorkspacePath()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.WorkspacePath = "/new/workspace";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("/new/workspace", svc.Saved!.WorkspacePath);
    }

    [Fact]
    public async Task Save_PersistsOutputPath()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.OutputPath = "/new/output";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("/new/output", svc.Saved!.OutputPath);
    }

    [Fact]
    public async Task Save_PersistsApiKey()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.ApiKey = "sk-secret";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("sk-secret", svc.Saved!.ApiKey);
    }

    [Fact]
    public async Task Save_PersistsAutoApprove()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.AutoApprove = true;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(svc.Saved!.AutoApprove);
    }

    [Fact]
    public async Task Save_PersistsModel()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.Model = "claude-opus-4";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("claude-opus-4", svc.Saved!.AiModel);
    }

    [Theory]
    [InlineData(0, 1)]    // clamped to minimum
    [InlineData(-5, 1)]   // negative clamped to 1
    [InlineData(30, 30)]  // normal value
    public async Task Save_ClampsTimeoutMinutesToMinimumOne(int vmValue, int expectedSaved)
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.TimeoutMinutes = vmValue;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(expectedSaved, svc.Saved!.TimeoutMinutes);
    }

    [Theory]
    [InlineData(-1, 0)]  // negative clamped to 0
    [InlineData(0, 0)]   // zero allowed
    [InlineData(3, 3)]   // normal
    public async Task Save_ClampsMaxRetriesToZero(int vmValue, int expectedSaved)
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());
        vm.MaxRetries = vmValue;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal(expectedSaved, svc.Saved!.MaxRetries);
    }

    [Fact]
    public async Task Save_SetsHasSavedConfirmationToTrue()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.HasSavedConfirmation);
    }

    [Fact]
    public async Task Save_SetsSaveConfirmationMessageContainingTime()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.SaveConfirmationMessage));
        Assert.Contains("Settings saved at", vm.SaveConfirmationMessage);
    }

    [Fact]
    public async Task Save_ResetIsSavingToFalseAfterCompletion()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsSaving);
    }

    [Fact]
    public async Task Save_ResetIsSavingToFalseEvenIfServiceThrows()
    {
        var svc = new ThrowingSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        // AsyncRelayCommand captures exceptions rather than re-throwing them,
        // so we simply await the command and then verify the finally-block ran.
        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsSaving);
    }

    // ---------------------------------------------------------------------------
    // BrowseWorkspaceAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task BrowseWorkspace_SetsWorkspacePath_WhenServiceReturnsPath()
    {
        var svc = new FakeSettingsService();
        var ws = new FakeWorkspaceService { PathToReturn = "/chosen/workspace" };
        var vm = new SettingsViewModel(svc, ws);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/chosen/workspace", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_DoesNotChangeWorkspacePath_WhenServiceReturnsNull()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/original" } };
        var ws = new FakeWorkspaceService { PathToReturn = null };
        var vm = new SettingsViewModel(svc, ws);
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/original", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_DoesNotChangeWorkspacePath_WhenServiceReturnsEmptyString()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/original" } };
        var ws = new FakeWorkspaceService { PathToReturn = string.Empty };
        var vm = new SettingsViewModel(svc, ws);
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/original", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_CallsOpenWorkspaceOnService()
    {
        var svc = new FakeSettingsService();
        var ws = new FakeWorkspaceService { PathToReturn = "/path" };
        var vm = new SettingsViewModel(svc, ws);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.True(ws.WasCalled);
    }

    // ---------------------------------------------------------------------------
    // ResetAsync
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Reset_ReloadsSettingsFromService()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        // Dirty the VM
        vm.WorkspacePath = "dirty-path";
        vm.ApiKey = "dirty-key";

        // Reload via Reset
        await vm.ResetCommand.ExecuteAsync(null);

        // After reset, the VM should reflect what the service has (empty strings in this case)
        Assert.Equal(string.Empty, vm.WorkspacePath);
        Assert.Equal(string.Empty, vm.ApiKey);
    }

    [Fact]
    public async Task Reset_ClearsSavedConfirmation()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService());

        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasSavedConfirmation);

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.False(vm.HasSavedConfirmation);
    }

    // ---------------------------------------------------------------------------
    // Helper fakes
    // ---------------------------------------------------------------------------

    private sealed class ThrowingSettingsService : ISettingsService
    {
        public Task<AppSettings> LoadAsync(CancellationToken ct = default) =>
            Task.FromResult(new AppSettings());

        public Task SaveAsync(AppSettings settings, CancellationToken ct = default) =>
            throw new InvalidOperationException("Simulated save failure");
    }
}