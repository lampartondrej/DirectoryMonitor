using System.Text.Json;
using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Infrastructure.Persistence;

public class SnapshotRepository : ISnapshotRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _storageDirectory;
    private readonly ILogger<SnapshotRepository> _logger;

    public SnapshotRepository(string storageDirectory, ILogger<SnapshotRepository> logger)
    {
        _storageDirectory = storageDirectory;
        _logger = logger;
        Directory.CreateDirectory(_storageDirectory);
    }

    public async Task<DirectorySnapshot?> LoadAsync(string directoryPath, CancellationToken cancellationToken = default)
    {
        var filePath = GetSnapshotFilePath(directoryPath);

        if (!File.Exists(filePath))
        {
            _logger.LogDebug("No existing snapshot for: {DirectoryPath}", directoryPath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(filePath);
            return await JsonSerializer.DeserializeAsync<DirectorySnapshot>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load snapshot from {FilePath}", filePath);
            return null;
        }
    }

    public async Task SaveAsync(DirectorySnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var filePath = GetSnapshotFilePath(snapshot.DirectoryPath);

        try
        {
            await using var stream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);
            _logger.LogDebug("Snapshot saved to {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save snapshot to {FilePath}", filePath);
            throw;
        }
    }

    /// <summary>
    /// Converts a directory path into a stable, filesystem-safe filename using its SHA256 hash.
    /// </summary>
    private string GetSnapshotFilePath(string directoryPath)
    {
        var key = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(directoryPath.ToLowerInvariant())));

        return Path.Combine(_storageDirectory, $"{key}.json");
    }
}
