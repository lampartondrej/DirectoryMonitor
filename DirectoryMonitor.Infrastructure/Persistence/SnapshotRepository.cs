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
            return null;

        try
        {
            await using var stream = File.OpenRead(filePath);
            var snapshot = await JsonSerializer.DeserializeAsync<DirectorySnapshot>(stream, JsonOptions, cancellationToken);
            _logger.LogDebug("Snapshot loaded: {FileCount} file(s) for {DirectoryPath}", snapshot?.Files.Count ?? 0, directoryPath);
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize snapshot for {DirectoryPath} — treating as no prior snapshot", directoryPath);
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
            _logger.LogDebug("Snapshot saved: {FileCount} file(s) for {DirectoryPath}", snapshot.Files.Count, snapshot.DirectoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save snapshot for {DirectoryPath}", snapshot.DirectoryPath);
            throw;
        }
    }

    /// <summary>
    /// Derives a stable, filesystem-safe filename from a directory path using SHA256.
    /// Normalises casing so the same logical path always maps to the same file.
    /// </summary>
    private string GetSnapshotFilePath(string directoryPath)
    {
        var key = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(directoryPath.ToLowerInvariant())));

        return Path.Combine(_storageDirectory, $"{key}.json");
    }
}

