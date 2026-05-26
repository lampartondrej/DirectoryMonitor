using DirectoryMonitor.Domain.Entities;

namespace DirectoryMonitor.Application.Interfaces;

public interface ISnapshotBuilder
{
    /// <summary>
    /// Scans the directory and builds a new snapshot, computing hashes and carrying
    /// forward version numbers from the previous snapshot where applicable.
    /// </summary>
    Task<DirectorySnapshot> BuildAsync(
        string directoryPath,
        DirectorySnapshot? previousSnapshot,
        CancellationToken cancellationToken = default);
}
