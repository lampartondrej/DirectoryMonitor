namespace DirectoryMonitor.Domain.Entities;

public class DirectorySnapshot
{
    public string DirectoryPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastAnalyzedAt { get; set; }
    public List<FileSnapshot> Files { get; set; } = [];
}
