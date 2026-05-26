using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Models;
using DirectoryMonitor.Application.Services;
using DirectoryMonitor.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace DirectoryMonitor.Tests;

public class DirectoryAnalysisServiceTests
{
    private readonly ISnapshotBuilder _snapshotBuilder = Substitute.For<ISnapshotBuilder>();
    private readonly IChangeDetectionService _changeDetection = Substitute.For<IChangeDetectionService>();
    private readonly ISnapshotRepository _snapshotRepository = Substitute.For<ISnapshotRepository>();

    private readonly DirectoryAnalysisService _sut;
    private readonly string _testDir = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);

    public DirectoryAnalysisServiceTests()
    {
        _sut = new DirectoryAnalysisService(
            _snapshotBuilder,
            _changeDetection,
            _snapshotRepository,
            NullLogger<DirectoryAnalysisService>.Instance);
    }

    [Fact]
    public async Task AnalyzeAsync_NewDirectory_ReturnsAddedChanges()
    {
        var currentSnapshot = SnapshotWith("file.txt", "hash1", version: 1);
        var changes = new List<FileChange>
        {
            new() { RelativePath = "file.txt", ChangeType = ChangeType.Added, Version = 1 }
        };

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(currentSnapshot);
        _changeDetection.DetectChanges(null, currentSnapshot).Returns(changes);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Single(result.Changes);
        Assert.Equal(ChangeType.Added, result.Changes[0].ChangeType);
        Assert.Equal(1, result.Changes[0].Version);
    }

    [Fact]
    public async Task AnalyzeAsync_NoChanges_ReturnsEmptyChangeList()
    {
        var snapshot = SnapshotWith("file.txt", "hash1", version: 1);

        _snapshotRepository.LoadAsync(_testDir, default).Returns(snapshot);
        _snapshotBuilder.BuildAsync(_testDir, snapshot, default).Returns(snapshot);
        _changeDetection.DetectChanges(snapshot, snapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Empty(result.Changes);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyDirectory_ReturnsZeroFilesScanned()
    {
        var emptySnapshot = new DirectorySnapshot { DirectoryPath = _testDir, Files = [] };

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(emptySnapshot);
        _changeDetection.DetectChanges(null, emptySnapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Equal(0, result.TotalFilesScanned);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public async Task AnalyzeAsync_ReportsTotalFilesScanned()
    {
        var snapshot = new DirectorySnapshot
        {
            DirectoryPath = _testDir,
            Files =
            [
                new FileSnapshot { RelativePath = "a.txt", Hash = "h1", Version = 1 },
                new FileSnapshot { RelativePath = "b.txt", Hash = "h2", Version = 1 }
            ]
        };

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(snapshot);
        _changeDetection.DetectChanges(null, snapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Equal(2, result.TotalFilesScanned);
    }

    [Fact]
    public async Task AnalyzeAsync_NestedDirectoryFiles_AreIncluded()
    {
        var snapshot = new DirectorySnapshot
        {
            DirectoryPath = _testDir,
            Files =
            [
                new FileSnapshot { RelativePath = @"src\core\Program.cs", Hash = "h1", Version = 1 },
                new FileSnapshot { RelativePath = @"src\tests\UnitTest.cs", Hash = "h2", Version = 1 }
            ]
        };
        var changes = snapshot.Files
            .Select(f => new FileChange { RelativePath = f.RelativePath, ChangeType = ChangeType.Added, Version = 1 })
            .ToList();

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(snapshot);
        _changeDetection.DetectChanges(null, snapshot).Returns(changes);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Equal(2, result.Changes.Count);
        Assert.All(result.Changes, c => Assert.Equal(ChangeType.Added, c.ChangeType));
    }

    [Fact]
    public async Task AnalyzeAsync_SavesSnapshotAfterAnalysis()
    {
        var snapshot = SnapshotWith("file.txt", "hash1", version: 1);

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(snapshot);
        _changeDetection.DetectChanges(null, snapshot).Returns([]);

        await _sut.AnalyzeAsync(_testDir);

        await _snapshotRepository.Received(1).SaveAsync(snapshot, default);
    }

    [Fact]
    public async Task AnalyzeAsync_ReturnsCorrectDirectoryPath()
    {
        var snapshot = SnapshotWith("a.txt", "hash", version: 1);

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(snapshot);
        _changeDetection.DetectChanges(null, snapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Equal(_testDir, result.DirectoryPath);
    }

    [Fact]
    public async Task AnalyzeAsync_InvalidDirectory_ThrowsDirectoryNotFoundException()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => _sut.AnalyzeAsync("/does/not/exist/at/all_xyz"));
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.AnalyzeAsync("   "));
    }

    [Fact]
    public async Task AnalyzeAsync_PathWithInvalidChars_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.AnalyzeAsync("C:\\bad\0path"));
    }

    // Helper
    private static DirectorySnapshot SnapshotWith(string relativePath, string hash, int version) => new()
    {
        DirectoryPath = Path.GetTempPath(),
        Files = [new FileSnapshot { RelativePath = relativePath, Hash = hash, Version = version }]
    };
}


