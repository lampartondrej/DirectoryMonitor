using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Models;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Application.Services;

/// <summary>
/// Orchestrates directory analysis: load snapshot ? build current state
/// ? detect changes ? persist. Each concern is handled by a focused service.
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

        _logger.LogInformation("Analysis started: {DirectoryPath}", directoryPath);

        var previousSnapshot = await _snapshotRepository.LoadAsync(directoryPath, cancellationToken);

        if (previousSnapshot is null)
            _logger.LogDebug("No previous snapshot found for {DirectoryPath} — treating all files as new", directoryPath);
        else
            _logger.LogDebug("Previous snapshot loaded: {FileCount} file(s) tracked for {DirectoryPath}", previousSnapshot.Files.Count, directoryPath);

        var currentSnapshot = await _snapshotBuilder.BuildAsync(directoryPath, previousSnapshot, cancellationToken);
        var changes = _changeDetection.DetectChanges(previousSnapshot, currentSnapshot);

        await _snapshotRepository.SaveAsync(currentSnapshot, cancellationToken);

        _logger.LogInformation(
            "Analysis complete: {DirectoryPath} — {Total} file(s) scanned, {Added} added, {Modified} modified, {Deleted} deleted",
            directoryPath,
            currentSnapshot.Files.Count,
            changes.Count(c => c.ChangeType == ChangeType.Added),
            changes.Count(c => c.ChangeType == ChangeType.Modified),
            changes.Count(c => c.ChangeType == ChangeType.Deleted));

        return new AnalysisResult
        {
            DirectoryPath    = directoryPath,
            AnalyzedAt       = currentSnapshot.LastAnalyzedAt,
            TotalFilesScanned = currentSnapshot.Files.Count,
            Changes          = changes
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

