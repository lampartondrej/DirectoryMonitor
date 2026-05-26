using DirectoryMonitor.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DirectoryMonitor.Infrastructure.Services;

public class FileScanner : IFileScanner
{
    private readonly ILogger<FileScanner> _logger;

    public FileScanner(ILogger<FileScanner> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> ScanFiles(string directoryPath)
    {
        _logger.LogDebug("Scanning directory: {DirectoryPath}", directoryPath);

        return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase);
    }
}
