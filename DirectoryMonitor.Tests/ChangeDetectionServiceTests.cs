using DirectoryMonitor.Application.Models;
using DirectoryMonitor.Application.Services;
using DirectoryMonitor.Domain.Entities;

namespace DirectoryMonitor.Tests;

/// <summary>
/// Unit tests for ChangeDetectionService.
/// These are pure logic tests — no mocks needed.
/// </summary>
public class ChangeDetectionServiceTests
{
    private readonly ChangeDetectionService _sut = new();

    [Fact]
    public void DetectChanges_NoExistingSnapshot_AllFilesAreAdded()
    {
        var current = SnapshotWith([("a.txt", "hash1", 1), ("b.txt", "hash2", 1)]);

        var changes = _sut.DetectChanges(previousSnapshot: null, current);

        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeType.Added, c.ChangeType));
    }

    [Fact]
    public void DetectChanges_SameFiles_NoChanges()
    {
        var snapshot = SnapshotWith([("a.txt", "hash1", 1)]);

        var changes = _sut.DetectChanges(snapshot, snapshot);

        Assert.Empty(changes);
    }

    [Fact]
    public void DetectChanges_NewFileAppears_DetectedAsAdded()
    {
        var previous = SnapshotWith([("a.txt", "hash1", 1)]);
        var current  = SnapshotWith([("a.txt", "hash1", 1), ("b.txt", "hash2", 1)]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Single(changes);
        Assert.Equal(ChangeType.Added, changes[0].ChangeType);
        Assert.Equal("b.txt", changes[0].RelativePath);
    }

    [Fact]
    public void DetectChanges_FileContentChanges_DetectedAsModified()
    {
        var previous = SnapshotWith([("a.txt", "oldHash", 1)]);
        var current  = SnapshotWith([("a.txt", "newHash", 2)]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Single(changes);
        Assert.Equal(ChangeType.Modified, changes[0].ChangeType);
        Assert.Equal(2, changes[0].Version);
    }

    [Fact]
    public void DetectChanges_FileMissing_DetectedAsDeleted()
    {
        var previous = SnapshotWith([("a.txt", "hash1", 1), ("b.txt", "hash2", 1)]);
        var current  = SnapshotWith([("a.txt", "hash1", 1)]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Single(changes);
        Assert.Equal(ChangeType.Deleted, changes[0].ChangeType);
        Assert.Equal("b.txt", changes[0].RelativePath);
    }

    [Fact]
    public void DetectChanges_EmptyDirectory_AllPreviousFilesDeleted()
    {
        var previous = SnapshotWith([("a.txt", "hash1", 1), ("b.txt", "hash2", 3)]);
        var current  = SnapshotWith([]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Equal(2, changes.Count);
        Assert.All(changes, c => Assert.Equal(ChangeType.Deleted, c.ChangeType));
    }

    [Fact]
    public void DetectChanges_MixedChanges_AllTypesDetected()
    {
        var previous = SnapshotWith([("keep.txt", "hash", 1), ("modify.txt", "old", 1), ("delete.txt", "hash", 1)]);
        var current  = SnapshotWith([("keep.txt", "hash", 1), ("modify.txt", "new", 2), ("add.txt", "hash", 1)]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Equal(3, changes.Count);
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Added   && c.RelativePath == "add.txt");
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Modified && c.RelativePath == "modify.txt");
        Assert.Contains(changes, c => c.ChangeType == ChangeType.Deleted  && c.RelativePath == "delete.txt");
    }

    [Fact]
    public void DetectChanges_PathComparisonIsCaseInsensitive()
    {
        var previous = SnapshotWith([("README.txt", "hash", 1)]);
        var current  = SnapshotWith([("readme.txt", "hash", 1)]);

        // Same file, different casing — should report no changes
        var changes = _sut.DetectChanges(previous, current);

        Assert.Empty(changes);
    }

    [Fact]
    public void DetectChanges_NestedFiles_HandledCorrectly()
    {
        var previous = SnapshotWith([(@"src\helpers\util.cs", "hash1", 1)]);
        var current  = SnapshotWith([(@"src\helpers\util.cs", "hash2", 2)]);

        var changes = _sut.DetectChanges(previous, current);

        Assert.Single(changes);
        Assert.Equal(ChangeType.Modified, changes[0].ChangeType);
    }

    // Helper to build a snapshot from a list of (path, hash, version) tuples
    private static DirectorySnapshot SnapshotWith(IEnumerable<(string path, string hash, int version)> files) => new()
    {
        DirectoryPath = "C:\\TestDir",
        Files = files.Select(f => new FileSnapshot
        {
            RelativePath = f.path,
            Hash         = f.hash,
            Version      = f.version
        }).ToList()
    };
}
