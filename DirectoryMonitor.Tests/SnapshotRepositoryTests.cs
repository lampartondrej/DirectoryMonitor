using System.Text.Json;
using DirectoryMonitor.Domain.Entities;
using DirectoryMonitor.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace DirectoryMonitor.Tests;

/// <summary>
/// Integration-style tests for SnapshotRepository — exercises real file IO
/// against a temporary directory that is cleaned up after each test.
/// </summary>
public class SnapshotRepositoryTests : IDisposable
{
    private readonly string _storageDir = Path.Combine(Path.GetTempPath(), $"dm-tests-{Guid.NewGuid():N}");
    private readonly SnapshotRepository _sut;

    public SnapshotRepositoryTests()
    {
        _sut = new SnapshotRepository(_storageDir, NullLogger<SnapshotRepository>.Instance);
    }

    [Fact]
    public async Task LoadAsync_NoSnapshotExists_ReturnsNull()
    {
        var result = await _sut.LoadAsync("C:\\NonExistentDirectory");

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_ReturnsEquivalentSnapshot()
    {
        var snapshot = new DirectorySnapshot
        {
            DirectoryPath   = "C:\\MyProject",
            CreatedAt       = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            LastAnalyzedAt  = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
            Files =
            [
                new FileSnapshot { RelativePath = "readme.txt", Hash = "abc123", Version = 2 }
            ]
        };

        await _sut.SaveAsync(snapshot);
        var loaded = await _sut.LoadAsync(snapshot.DirectoryPath);

        Assert.NotNull(loaded);
        Assert.Equal(snapshot.DirectoryPath, loaded.DirectoryPath);
        Assert.Single(loaded.Files);
        Assert.Equal("readme.txt", loaded.Files[0].RelativePath);
        Assert.Equal("abc123",     loaded.Files[0].Hash);
        Assert.Equal(2,            loaded.Files[0].Version);
    }

    [Fact]
    public async Task SaveAsync_OverwritesPreviousSnapshot()
    {
        const string dir = "C:\\SomeProject";

        var first = new DirectorySnapshot
        {
            DirectoryPath = dir,
            Files = [new FileSnapshot { RelativePath = "a.txt", Hash = "hash1", Version = 1 }]
        };

        var second = new DirectorySnapshot
        {
            DirectoryPath = dir,
            Files = [new FileSnapshot { RelativePath = "a.txt", Hash = "hash2", Version = 2 }]
        };

        await _sut.SaveAsync(first);
        await _sut.SaveAsync(second);
        var loaded = await _sut.LoadAsync(dir);

        Assert.NotNull(loaded);
        Assert.Equal("hash2", loaded.Files[0].Hash);
        Assert.Equal(2, loaded.Files[0].Version);
    }

    [Fact]
    public async Task SaveAsync_DifferentDirectories_StoredInSeparateFiles()
    {
        var snap1 = new DirectorySnapshot { DirectoryPath = "C:\\DirA", Files = [] };
        var snap2 = new DirectorySnapshot { DirectoryPath = "C:\\DirB", Files = [] };

        await _sut.SaveAsync(snap1);
        await _sut.SaveAsync(snap2);

        Assert.Equal(2, Directory.GetFiles(_storageDir, "*.json").Length);
    }

    [Fact]
    public async Task SaveAsync_SameDirectoryDifferentCase_ProduceSameFile()
    {
        var snap1 = new DirectorySnapshot { DirectoryPath = "C:\\MyProject", Files = [] };
        var snap2 = new DirectorySnapshot { DirectoryPath = "C:\\MYPROJECT", Files = [] };

        await _sut.SaveAsync(snap1);
        await _sut.SaveAsync(snap2);

        // Both paths normalize to the same key ? only one JSON file
        Assert.Single(Directory.GetFiles(_storageDir, "*.json"));
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsNull()
    {
        // Write corrupt JSON to the expected snapshot location using the same
        // key derivation logic the repository uses internally.
        var key = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes("C:\\CorruptDir".ToLowerInvariant())));

        var filePath = Path.Combine(_storageDir, $"{key}.json");
        await File.WriteAllTextAsync(filePath, "{ not valid json ]]]");

        var result = await _sut.LoadAsync("C:\\CorruptDir");

        Assert.Null(result);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageDir))
            Directory.Delete(_storageDir, recursive: true);
    }
}
