using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Models;
using DirectoryMonitor.Application.Services;
using DirectoryMonitor.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
        var previousSnapshot = SnapshotWith("file.txt", "hash1", version: 1);
        var currentSnapshot  = SnapshotWith("file.txt", "hash1", version: 1);

        _snapshotRepository.LoadAsync(_testDir, default).Returns(previousSnapshot);
        _snapshotBuilder.BuildAsync(_testDir, previousSnapshot, default).Returns(currentSnapshot);
        _changeDetection.DetectChanges(previousSnapshot, currentSnapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Empty(result.Changes);
        Assert.False(result.HasChanges);
    }

    [Fact]
    public async Task AnalyzeAsync_SavesSnapshotAfterAnalysis()
    {
        var currentSnapshot = SnapshotWith("file.txt", "hash1", version: 1);

        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(currentSnapshot);
        _changeDetection.DetectChanges(null, currentSnapshot).Returns([]);

        await _sut.AnalyzeAsync(_testDir);

        await _snapshotRepository.Received(1).SaveAsync(currentSnapshot, default);
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

    [Fact]
    public async Task AnalyzeAsync_ReturnsCorrectDirectoryPath()
    {
        var currentSnapshot = SnapshotWith("a.txt", "hash", version: 1);
        _snapshotRepository.LoadAsync(_testDir, default).Returns((DirectorySnapshot?)null);
        _snapshotBuilder.BuildAsync(_testDir, null, default).Returns(currentSnapshot);
        _changeDetection.DetectChanges(null, currentSnapshot).Returns([]);

        var result = await _sut.AnalyzeAsync(_testDir);

        Assert.Equal(_testDir, result.DirectoryPath);
    }

    // Helper
    private static DirectorySnapshot SnapshotWith(string relativePath, string hash, int version) => new()
    {
        DirectoryPath = Path.GetTempPath(),
        Files = [new FileSnapshot { RelativePath = relativePath, Hash = hash, Version = version }]
    };
}

