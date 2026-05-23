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

    private sealed class FakeSecureStorageService : ISecureStorageService
    {
        public string StoredApiKey { get; set; } = string.Empty;
        public bool RemoveCalled { get; private set; }

        public Task<string> GetApiKeyAsync() => Task.FromResult(StoredApiKey);

        public Task SetApiKeyAsync(string apiKey)
        {
            StoredApiKey = apiKey;
            return Task.CompletedTask;
        }

        public Task RemoveApiKeyAsync()
        {
            RemoveCalled = true;
            StoredApiKey = string.Empty;
            return Task.CompletedTask;
        }
    }

    // ---------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------

    private static (SettingsViewModel vm, FakeSettingsService svc, FakeWorkspaceService ws, FakeSecureStorageService sec)
        Create(AppSettings? initialSettings = null, string storedApiKey = "")
    {
        var svc = new FakeSettingsService();
        if (initialSettings is not null)
            svc.Settings = initialSettings;

        var ws = new FakeWorkspaceService();
        var sec = new FakeSecureStorageService { StoredApiKey = storedApiKey };
        var vm = new SettingsViewModel(svc, ws, sec);
        Task.Run(() => vm.LoadSettingsCommand.ExecuteAsync(null)).GetAwaiter().GetResult();
        return (vm, svc, ws, sec);
    }

    // ---------------------------------------------------------------------------
    // Providers list
    // ---------------------------------------------------------------------------

    [Fact]
    public void Providers_ContainsThreeExpectedEntries()
    {
        var (vm, _, _, _) = Create();

        Assert.Equal(3, vm.Providers.Count);
        Assert.Contains("DeepSeek", vm.Providers);
        Assert.Contains("Copilot", vm.Providers);
        Assert.Contains("Local", vm.Providers);
    }

    [Fact]
    public void Providers_OrderIsDeepSeekFirst()
    {
        var (vm, _, _, _) = Create();
        Assert.Equal("DeepSeek", vm.Providers[0]);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — field mapping
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadSettings_MapsWorkspacePath()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/ws" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("/ws", vm.WorkspacePath);
    }

    [Fact]
    public async Task LoadSettings_MapsOutputPath()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { OutputPath = "/out" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("/out", vm.OutputPath);
    }

    [Fact]
    public async Task LoadSettings_MapsApiKeyFromSecureStorage()
    {
        var sec = new FakeSecureStorageService { StoredApiKey = "sk-abc" };
        var vm = new SettingsViewModel(new FakeSettingsService(), new FakeWorkspaceService(), sec);
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("sk-abc", vm.ApiKey);
    }

    [Fact]
    public async Task LoadSettings_MapsModel()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { AiModel = "gpt-5" } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal("gpt-5", vm.Model);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — provider label conversion
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("deepseek", "DeepSeek")]
    [InlineData("copilot", "Copilot")]
    [InlineData("local", "Local")]
    [InlineData("", "Local")]
    [InlineData("unknown-provider", "DeepSeek")]  // fallback
    [InlineData("DEEPSEEK", "DeepSeek")]          // case-insensitive
    public async Task LoadSettings_ConvertsAiProviderToLabel(string rawProvider, string expectedLabel)
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { AiProvider = rawProvider } };
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.Equal(expectedLabel, vm.SelectedProvider);
    }

    // ---------------------------------------------------------------------------
    // LoadSettingsAsync — confirmation reset
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task LoadSettings_ResetsSavedConfirmation()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.True(vm.HasSavedConfirmation);

        await vm.LoadSettingsCommand.ExecuteAsync(null);

        Assert.False(vm.HasSavedConfirmation);
    }

    // ---------------------------------------------------------------------------
    // SaveAsync — provider value conversion
    // ---------------------------------------------------------------------------

    [Theory]
    [InlineData("DeepSeek", "deepseek")]
    [InlineData("Copilot", "copilot")]
    [InlineData("Local", "")]
    [InlineData("SomethingUnknown", "deepseek")] // fallback
    public async Task Save_ConvertsSelectedProviderToRawValue(string label, string expectedValue)
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        vm.SelectedProvider = label;

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.NotNull(svc.Saved);
        Assert.Equal(expectedValue, svc.Saved!.AiProvider);
    }

    [Fact]
    public async Task Save_PersistsWorkspacePath()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        vm.WorkspacePath = "/new/workspace";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("/new/workspace", svc.Saved!.WorkspacePath);
    }

    [Fact]
    public async Task Save_PersistsOutputPath()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        vm.OutputPath = "/new/output";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("/new/output", svc.Saved!.OutputPath);
    }

    [Fact]
    public async Task Save_PersistsApiKeyViaSecureStorage()
    {
        var sec = new FakeSecureStorageService();
        var vm = new SettingsViewModel(new FakeSettingsService(), new FakeWorkspaceService(), sec);
        vm.ApiKey = "sk-secret";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("sk-secret", sec.StoredApiKey);
    }

    [Fact]
    public async Task Save_PersistsModel()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());
        vm.Model = "claude-opus-4";

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.Equal("claude-opus-4", svc.Saved!.AiModel);
    }

    [Fact]
    public async Task Save_SetsHasSavedConfirmationToTrue()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.True(vm.HasSavedConfirmation);
    }

    [Fact]
    public async Task Save_SetsSaveConfirmationMessageContainingTime()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(string.IsNullOrWhiteSpace(vm.SaveConfirmationMessage));
        Assert.Contains("Settings saved at", vm.SaveConfirmationMessage);
    }

    [Fact]
    public async Task Save_ResetIsSavingToFalseAfterCompletion()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsSaving);
    }

    [Fact]
    public async Task Save_ResetIsSavingToFalseEvenIfServiceThrows()
    {
        var svc = new ThrowingSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());

        await vm.SaveCommand.ExecuteAsync(null);

        Assert.False(vm.IsSaving);
    }

    // ---------------------------------------------------------------------------
    // BrowseWorkspaceAsync — no longer persists immediately
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task BrowseWorkspace_SetsWorkspacePath_WhenServiceReturnsPath()
    {
        var svc = new FakeSettingsService();
        var ws = new FakeWorkspaceService { PathToReturn = "/chosen/workspace" };
        var vm = new SettingsViewModel(svc, ws, new FakeSecureStorageService());

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/chosen/workspace", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_DoesNotChangeWorkspacePath_WhenServiceReturnsNull()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/original" } };
        var ws = new FakeWorkspaceService { PathToReturn = null };
        var vm = new SettingsViewModel(svc, ws, new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/original", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_DoesNotChangeWorkspacePath_WhenServiceReturnsEmptyString()
    {
        var svc = new FakeSettingsService { Settings = new AppSettings { WorkspacePath = "/original" } };
        var ws = new FakeWorkspaceService { PathToReturn = string.Empty };
        var vm = new SettingsViewModel(svc, ws, new FakeSecureStorageService());
        await vm.LoadSettingsCommand.ExecuteAsync(null);

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.Equal("/original", vm.WorkspacePath);
    }

    [Fact]
    public async Task BrowseWorkspace_CallsOpenWorkspaceOnService()
    {
        var svc = new FakeSettingsService();
        var ws = new FakeWorkspaceService { PathToReturn = "/path" };
        var vm = new SettingsViewModel(svc, ws, new FakeSecureStorageService());

        await vm.BrowseWorkspaceCommand.ExecuteAsync(null);

        Assert.True(ws.WasCalled);
    }

    // ---------------------------------------------------------------------------
    // ResetAsync — restores defaults
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task Reset_RestoresDefaultValues()
    {
        var (vm, svc, _, sec) = Create(new AppSettings { WorkspacePath = "/ws", AiModel = "gpt-5" }, "sk-key");

        // Dirty the VM
        vm.WorkspacePath = "dirty-path";
        vm.ApiKey = "dirty-key";

        await vm.ResetCommand.ExecuteAsync(null);

        // After reset, the VM should reflect defaults
        Assert.Equal(string.Empty, vm.WorkspacePath);
        Assert.Equal(string.Empty, vm.ApiKey);
        Assert.Equal("DeepSeek", vm.SelectedProvider);
    }

    [Fact]
    public async Task Reset_RemovesApiKeyFromSecureStorage()
    {
        var (vm, _, _, sec) = Create(storedApiKey: "sk-key");

        await vm.ResetCommand.ExecuteAsync(null);

        Assert.True(sec.RemoveCalled);
        Assert.Equal(string.Empty, sec.StoredApiKey);
    }

    [Fact]
    public async Task Reset_ClearsSavedConfirmation()
    {
        var svc = new FakeSettingsService();
        var vm = new SettingsViewModel(svc, new FakeWorkspaceService(), new FakeSecureStorageService());

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