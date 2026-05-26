using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Models;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Application.Services;

/// <summary>
/// Orchestrates directory analysis: load previous snapshot ? build current snapshot
/// ? detect changes ? persist. Delegates all real work to focused services.
/// </summary>
public class DirectoryAnalysisService : IDirectoryAnalysisService
{
    private readonly ISnapshotBuilder _snapshotBuilder;
    private readonly IChangeDetectionService _changeDetection;
    private readonly ISnapshotRepository _snapshotRepository;
    private readonly ILogger<DirectoryAnalysisService> _logger;

    public DirectoryAnalysisService(
        ISnapshotBuilder snapshotBuilder,
        IChangeDetectionService changeDetection,
        ISnapshotRepository snapshotRepository,
        ILogger<DirectoryAnalysisService> logger)
    {
        _snapshotBuilder = snapshotBuilder;
        _changeDetection = changeDetection;
        _snapshotRepository = snapshotRepository;
        _logger = logger;
    }

    public async Task<AnalysisResult> AnalyzeAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        ValidateDirectory(directoryPath);

        _logger.LogInformation("Analysis started for {DirectoryPath}", directoryPath);

        var previousSnapshot = await _snapshotRepository.LoadAsync(directoryPath, cancellationToken);
        _logger.LogDebug("Previous snapshot {Status} for {DirectoryPath}",
            previousSnapshot is null ? "not found" : $"loaded ({previousSnapshot.Files.Count} file(s))",
            directoryPath);

        var currentSnapshot = await _snapshotBuilder.BuildAsync(directoryPath, previousSnapshot, cancellationToken);
        var changes = _changeDetection.DetectChanges(previousSnapshot, currentSnapshot);

        await _snapshotRepository.SaveAsync(currentSnapshot, cancellationToken);

        _logger.LogInformation(
            "Analysis complete for {DirectoryPath}: {Added} added, {Modified} modified, {Deleted} deleted",
            directoryPath,
            changes.Count(c => c.ChangeType == ChangeType.Added),
            changes.Count(c => c.ChangeType == ChangeType.Modified),
            changes.Count(c => c.ChangeType == ChangeType.Deleted));

        return new AnalysisResult
        {
            DirectoryPath = directoryPath,
            AnalyzedAt = currentSnapshot.LastAnalyzedAt,
            Changes = changes
        };
    }

    private static void ValidateDirectory(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path must not be empty.", nameof(directoryPath));

        if (directoryPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            throw new ArgumentException("Directory path contains invalid characters.", nameof(directoryPath));

        if (!Directory.Exists(directoryPath))
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");

        // Verify the directory is actually accessible before scanning
        try
        {
            _ = Directory.EnumerateFileSystemEntries(directoryPath).FirstOrDefault();
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new UnauthorizedAccessException($"Access denied to directory: {directoryPath}", ex);
        }
    }
}
