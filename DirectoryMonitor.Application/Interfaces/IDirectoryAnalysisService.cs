using DirectoryMonitor.Application.Models;

namespace DirectoryMonitor.Application.Interfaces;

public interface IDirectoryAnalysisService
{
    Task<AnalysisResult> AnalyzeAsync(string directoryPath, CancellationToken cancellationToken = default);
}
