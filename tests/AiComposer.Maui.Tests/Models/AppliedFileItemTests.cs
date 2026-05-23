using AiComposer.Maui.Models;

namespace AiComposer.Maui.Tests.Models;

public sealed class AppliedFileItemTests
{
    [Fact]
    public void DefaultRelativePath_IsEmpty()
    {
        var item = new AppliedFileItem();
        Assert.Equal(string.Empty, item.RelativePath);
    }

    [Fact]
    public void DefaultFullPath_IsEmpty()
    {
        var item = new AppliedFileItem();
        Assert.Equal(string.Empty, item.FullPath);
    }

    [Fact]
    public void DefaultChangeKind_IsModified()
    {
        var item = new AppliedFileItem();
        Assert.Equal("Modified", item.ChangeKind);
    }

    [Fact]
    public void InitProperties_StoreAssignedValues()
    {
        var item = new AppliedFileItem
        {
            RelativePath = "src/Foo.cs",
            FullPath = "/output/T-101/src/Foo.cs",
            ChangeKind = "New",
        };

        Assert.Equal("src/Foo.cs", item.RelativePath);
        Assert.Equal("/output/T-101/src/Foo.cs", item.FullPath);
        Assert.Equal("New", item.ChangeKind);
    }

    [Fact]
    public void ChangeKind_CanBeSetToNew()
    {
        var item = new AppliedFileItem { ChangeKind = "New" };
        Assert.Equal("New", item.ChangeKind);
    }

    [Fact]
    public void ChangeKind_CanBeSetToModified()
    {
        var item = new AppliedFileItem { ChangeKind = "Modified" };
        Assert.Equal("Modified", item.ChangeKind);
    }

    [Fact]
    public void RelativePath_AndFullPath_AreIndependent()
    {
        var item = new AppliedFileItem
        {
            RelativePath = "Models/Foo.cs",
            FullPath = @"C:\output\T-42\Models\Foo.cs",
        };

        Assert.Equal("Models/Foo.cs", item.RelativePath);
        Assert.Equal(@"C:\output\T-42\Models\Foo.cs", item.FullPath);
    }

    [Fact]
    public void TwoItems_WithSameValues_AreNotReferenceEqual()
    {
        var a = new AppliedFileItem { RelativePath = "a.cs", FullPath = "/a.cs", ChangeKind = "New" };
        var b = new AppliedFileItem { RelativePath = "a.cs", FullPath = "/a.cs", ChangeKind = "New" };

        Assert.NotSame(a, b);
        Assert.Equal(a.RelativePath, b.RelativePath);
        Assert.Equal(a.FullPath, b.FullPath);
        Assert.Equal(a.ChangeKind, b.ChangeKind);
    }
}