namespace DirectoryMonitor.Application.Interfaces;

public interface IHashingService
{
    Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default);
}
