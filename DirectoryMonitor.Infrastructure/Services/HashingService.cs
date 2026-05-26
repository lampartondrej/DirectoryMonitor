using System.Security.Cryptography;
using DirectoryMonitor.Application.Interfaces;

namespace DirectoryMonitor.Infrastructure.Services;

public class HashingService : IHashingService
{
    public async Task<string> ComputeHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        await using var stream = new FileStream(
            filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes);
    }
}
