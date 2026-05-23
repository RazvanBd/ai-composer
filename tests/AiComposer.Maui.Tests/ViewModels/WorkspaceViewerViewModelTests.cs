using System.Collections.Generic;
using AiComposer.Maui.Models;
using AiComposer.Maui.Services.Abstractions;
using AiComposer.Maui.ViewModels;

namespace AiComposer.Maui.Tests.ViewModels;

/// <summary>Unit tests for the new <see cref="WorkspaceViewerViewModel"/> additions:
/// <see cref="IQueryAttributable"/> implementation, <c>OnSelectedFileChanged</c> partial,
/// and <c>LoadAndSelectFromQueryAsync</c>.</summary>
public sealed class WorkspaceViewerViewModelTests
{
    // ---------------------------------------------------------------------------
    // Fakes
    // ---------------------------------------------------------------------------

    private sealed class FakeOutputService : IOutputService
    {
        public IReadOnlyList<GeneratedFileItem> Files { get; set; } = [];
        public string ContentToReturn { get; set; } = string.Empty;
        public string? LastReadPath { get; private set; }

        public Task<IReadOnlyList<GeneratedFileItem>> ListGeneratedFilesAsync(
            string ticketId, CancellationToken ct = default) =>
            Task.FromResult(Files);

        public Task<string> ReadFileContentAsync(string fullPath, CancellationToken ct = default)
        {
            LastReadPath = fullPath;
            return Task.FromResult(ContentToReturn);
        }
    }

    // ---------------------------------------------------------------------------
    // ApplyQueryAttributes — IQueryAttributable (new in this PR)
    // ---------------------------------------------------------------------------

    [Fact]
    public void ApplyQueryAttributes_SetsTicketId()
    {
        var svc = new FakeOutputService();
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object> { ["ticketId"] = "T-101" });

        Assert.Equal("T-101", vm.TicketId);
    }

    [Fact]
    public void ApplyQueryAttributes_SetsRequestedRelativePath()
    {
        var svc = new FakeOutputService();
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object>
            {
                ["ticketId"] = "T-101",
                ["relativePath"] = "src/Foo.cs",
            });

        Assert.Equal("src/Foo.cs", vm.RequestedRelativePath);
    }

    [Fact]
    public void ApplyQueryAttributes_DecodesUrlEncodedRelativePath()
    {
        var svc = new FakeOutputService();
        var vm = new WorkspaceViewerViewModel(svc);

        // Simulate URL-encoded path such as "src%2FFoo.cs"
        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object>
            {
                ["ticketId"] = "T-101",
                ["relativePath"] = "src%2FFoo.cs",
            });

        Assert.Equal("src/Foo.cs", vm.RequestedRelativePath);
    }

    [Fact]
    public void ApplyQueryAttributes_WithEmptyTicketId_DoesNotStartFileLoad()
    {
        var svc = new FakeOutputService
        {
            Files = [new GeneratedFileItem { RelativePath = "a.cs", FullPath = "/a.cs" }]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        // Provide empty ticketId — should NOT trigger LoadAndSelectFromQueryAsync.
        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object> { ["ticketId"] = string.Empty });

        // Give any async task time to complete.
        Task.Delay(50).GetAwaiter().GetResult();

        // Files should remain unloaded.
        Assert.Empty(vm.GeneratedFiles);
    }

    [Fact]
    public void ApplyQueryAttributes_WithNoTicketIdKey_DoesNotStartFileLoad()
    {
        var svc = new FakeOutputService
        {
            Files = [new GeneratedFileItem { RelativePath = "a.cs", FullPath = "/a.cs" }]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(new Dictionary<string, object>());

        Task.Delay(50).GetAwaiter().GetResult();

        Assert.Empty(vm.GeneratedFiles);
    }

    [Fact]
    public async Task ApplyQueryAttributes_WithTicketId_LoadsFiles()
    {
        var svc = new FakeOutputService
        {
            Files =
            [
                new GeneratedFileItem { RelativePath = "a.cs", FullPath = "/a.cs" },
                new GeneratedFileItem { RelativePath = "b.cs", FullPath = "/b.cs" },
            ]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object> { ["ticketId"] = "T-101" });

        // Wait for the fire-and-forget async load to complete.
        await Task.Delay(100);

        Assert.Equal(2, vm.GeneratedFiles.Count);
    }

    [Fact]
    public async Task ApplyQueryAttributes_WithMatchingRelativePath_SelectsMatchedFile()
    {
        var target = new GeneratedFileItem { RelativePath = "src/Foo.cs", FullPath = "/src/Foo.cs" };
        var svc = new FakeOutputService
        {
            Files =
            [
                new GeneratedFileItem { RelativePath = "src/Bar.cs", FullPath = "/src/Bar.cs" },
                target,
            ]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object>
            {
                ["ticketId"] = "T-101",
                ["relativePath"] = "src/Foo.cs",
            });

        await Task.Delay(100);

        Assert.NotNull(vm.SelectedFile);
        Assert.Equal("src/Foo.cs", vm.SelectedFile!.RelativePath);
    }

    [Fact]
    public async Task ApplyQueryAttributes_RelativePathMatchIsCaseInsensitive()
    {
        var target = new GeneratedFileItem { RelativePath = "src/Foo.cs", FullPath = "/src/Foo.cs" };
        var svc = new FakeOutputService { Files = [target] };
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object>
            {
                ["ticketId"] = "T-101",
                ["relativePath"] = "SRC/FOO.CS",   // different case
            });

        await Task.Delay(100);

        Assert.NotNull(vm.SelectedFile);
        Assert.Equal("src/Foo.cs", vm.SelectedFile!.RelativePath);
    }

    [Fact]
    public async Task ApplyQueryAttributes_WithNonMatchingRelativePath_DoesNotSelectFile()
    {
        var svc = new FakeOutputService
        {
            Files = [new GeneratedFileItem { RelativePath = "src/Foo.cs", FullPath = "/src/Foo.cs" }]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object>
            {
                ["ticketId"] = "T-101",
                ["relativePath"] = "does/not/exist.cs",
            });

        await Task.Delay(100);

        Assert.Null(vm.SelectedFile);
    }

    [Fact]
    public async Task ApplyQueryAttributes_WithTicketIdOnly_LoadsFilesAndDoesNotSelectAny()
    {
        var svc = new FakeOutputService
        {
            Files = [new GeneratedFileItem { RelativePath = "a.cs", FullPath = "/a.cs" }]
        };
        var vm = new WorkspaceViewerViewModel(svc);

        // Only ticketId, no relativePath
        ((IQueryAttributable)vm).ApplyQueryAttributes(
            new Dictionary<string, object> { ["ticketId"] = "T-202" });

        await Task.Delay(100);

        Assert.Equal(1, vm.GeneratedFiles.Count);
        Assert.Null(vm.SelectedFile);
    }

    // ---------------------------------------------------------------------------
    // RequestedRelativePath — new property default value
    // ---------------------------------------------------------------------------

    [Fact]
    public void RequestedRelativePath_DefaultsToEmpty()
    {
        var vm = new WorkspaceViewerViewModel(new FakeOutputService());
        Assert.Equal(string.Empty, vm.RequestedRelativePath);
    }

    [Fact]
    public void RequestedRelativePath_CanBeSetDirectly()
    {
        var vm = new WorkspaceViewerViewModel(new FakeOutputService());
        vm.RequestedRelativePath = "some/path.cs";
        Assert.Equal("some/path.cs", vm.RequestedRelativePath);
    }

    // ---------------------------------------------------------------------------
    // OnSelectedFileChanged partial — triggers SelectFileAsync (new in this PR)
    // ---------------------------------------------------------------------------

    [Fact]
    public async Task OnSelectedFileChanged_WhenFileSetToNonNull_LoadsFileContent()
    {
        var file = new GeneratedFileItem { RelativePath = "a.cs", FullPath = "/full/a.cs" };
        var svc = new FakeOutputService { ContentToReturn = "// hello" };
        var vm = new WorkspaceViewerViewModel(svc);

        vm.SelectedFile = file;

        // Allow the fire-and-forget SelectFileAsync to complete.
        await Task.Delay(100);

        Assert.Equal("// hello", vm.FileContent);
        Assert.Equal("/full/a.cs", svc.LastReadPath);
    }

    [Fact]
    public async Task OnSelectedFileChanged_WhenFileSetToNull_DoesNotReadContent()
    {
        var svc = new FakeOutputService { ContentToReturn = "irrelevant" };
        var vm = new WorkspaceViewerViewModel(svc);

        // Set to null — partial method guard `if (value is not null)` should prevent read.
        vm.SelectedFile = null;

        await Task.Delay(50);

        // ContentToReturn should not have been fetched.
        Assert.Null(svc.LastReadPath);
    }

    // ---------------------------------------------------------------------------
    // Boundary / regression cases
    // ---------------------------------------------------------------------------

    [Fact]
    public void ApplyQueryAttributes_NullTicketIdValue_SetsEmptyTicketId()
    {
        var vm = new WorkspaceViewerViewModel(new FakeOutputService());
        var query = new Dictionary<string, object> { ["ticketId"] = (object)null! };

        ((IQueryAttributable)vm).ApplyQueryAttributes(query);

        Assert.Equal(string.Empty, vm.TicketId);
    }

    [Fact]
    public void ApplyQueryAttributes_NullRelativePathValue_SetsEmptyRequestedRelativePath()
    {
        var vm = new WorkspaceViewerViewModel(new FakeOutputService());
        var query = new Dictionary<string, object>
        {
            ["ticketId"] = "T-1",
            ["relativePath"] = (object)null!,
        };

        ((IQueryAttributable)vm).ApplyQueryAttributes(query);

        Assert.Equal(string.Empty, vm.RequestedRelativePath);
    }
}
