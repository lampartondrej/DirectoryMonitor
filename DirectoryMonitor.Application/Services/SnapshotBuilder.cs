using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Application.Services;

public class SnapshotBuilder : ISnapshotBuilder
{
    private readonly IFileScanner _fileScanner;
    private readonly IHashingService _hashingService;
    private readonly ILogger<SnapshotBuilder> _logger;

    public SnapshotBuilder(IFileScanner fileScanner, IHashingService hashingService, ILogger<SnapshotBuilder> logger)
    {
        _fileScanner = fileScanner;
        _hashingService = hashingService;
        _logger = logger;
    }

    public async Task<DirectorySnapshot> BuildAsync(
        string directoryPath,
        DirectorySnapshot? previousSnapshot,
        CancellationToken cancellationToken = default)
    {
        var previousFiles = previousSnapshot?.Files
            .ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            ?? [];

        var scannedFiles = _fileScanner.ScanFiles(directoryPath).ToList();
        _logger.LogInformation("Found {FileCount} file(s) in {DirectoryPath}", scannedFiles.Count, directoryPath);

        var snapshot = new DirectorySnapshot
        {
            DirectoryPath = directoryPath,
            CreatedAt = previousSnapshot?.CreatedAt ?? DateTime.UtcNow,
            LastAnalyzedAt = DateTime.UtcNow,
            Files = []
        };

        foreach (var absolutePath in scannedFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var relativePath = Path.GetRelativePath(directoryPath, absolutePath);

            string hash;
            try
            {
                hash = await _hashingService.ComputeHashAsync(absolutePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not hash file {FilePath}, skipping.", absolutePath);
                continue;
            }

            var version = previousFiles.TryGetValue(relativePath, out var previous)
                ? (string.Equals(hash, previous.Hash, StringComparison.Ordinal) ? previous.Version : previous.Version + 1)
                : 1;

            snapshot.Files.Add(new FileSnapshot
            {
                RelativePath = relativePath,
                Hash = hash,
                Version = version,
                LastModified = DateTime.UtcNow
            });
        }

        return snapshot;
    }
}
