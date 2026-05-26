using DirectoryMonitor.Application.Models;

namespace DirectoryMonitor.Models;

public class AnalyzeViewModel
{
    public string DirectoryPath { get; set; } = string.Empty;
    public AnalysisResult? Result { get; set; }
    public string? ErrorMessage { get; set; }
}
