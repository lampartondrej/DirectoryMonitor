using DirectoryMonitor.Infrastructure.Services;

namespace DirectoryMonitor.Tests;

public class HashingServiceTests : IDisposable
{
    private readonly string _tempFile = Path.GetTempFileName();
    private readonly HashingService _sut = new();

    [Fact]
    public async Task ComputeHashAsync_SameContent_ReturnsSameHash()
    {
        await File.WriteAllTextAsync(_tempFile, "hello world");
        var hash1 = await _sut.ComputeHashAsync(_tempFile);
        var hash2 = await _sut.ComputeHashAsync(_tempFile);
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public async Task ComputeHashAsync_DifferentContent_ReturnsDifferentHash()
    {
        await File.WriteAllTextAsync(_tempFile, "hello");
        var hash1 = await _sut.ComputeHashAsync(_tempFile);
        await File.WriteAllTextAsync(_tempFile, "world");
        var hash2 = await _sut.ComputeHashAsync(_tempFile);
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public async Task ComputeHashAsync_NonExistentFile_Throws()
    {
        await Assert.ThrowsAnyAsync<Exception>(
            () => _sut.ComputeHashAsync("nonexistent_absolutely_missing_file.txt"));
    }

    public void Dispose() => File.Delete(_tempFile);
}
