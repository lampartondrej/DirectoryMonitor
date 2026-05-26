using DirectoryMonitor.Domain.Entities;

namespace DirectoryMonitor.Application.Interfaces;

public interface ISnapshotRepository
{
    Task<DirectorySnapshot?> LoadAsync(string directoryPath, CancellationToken cancellationToken = default);
    Task SaveAsync(DirectorySnapshot snapshot, CancellationToken cancellationToken = default);
}
