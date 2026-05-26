namespace DirectoryMonitor.Application.Models;

public class AnalysisResult
{
    public string DirectoryPath { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public List<FileChange> Changes { get; set; } = [];

    public bool HasChanges => Changes.Count > 0;
    public IEnumerable<FileChange> AddedFiles    => Changes.Where(c => c.ChangeType == ChangeType.Added);
    public IEnumerable<FileChange> ModifiedFiles => Changes.Where(c => c.ChangeType == ChangeType.Modified);
    public IEnumerable<FileChange> DeletedFiles  => Changes.Where(c => c.ChangeType == ChangeType.Deleted);
}

public class FileChange
{
    public string RelativePath { get; set; } = string.Empty;
    public ChangeType ChangeType { get; set; }
    public int Version { get; set; }
}

public enum ChangeType
{
    Added,
    Modified,
    Deleted
}

