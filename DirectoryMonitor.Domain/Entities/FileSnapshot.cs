namespace DirectoryMonitor.Domain.Entities;

public class FileSnapshot
{
    public string RelativePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime LastModified { get; set; }
}
