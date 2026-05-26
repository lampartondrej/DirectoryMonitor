using DirectoryMonitor.Application.Models;

namespace DirectoryMonitor.Models;

public class FileGroupViewModel
{
    public string GroupId     { get; set; } = string.Empty;
    public string Title       { get; set; } = string.Empty;
    public string HeaderClass { get; set; } = string.Empty;
    public string IconPath    { get; set; } = string.Empty;
    public string FileIconPath { get; set; } = string.Empty;
    public List<FileChange> Files { get; set; } = [];
}
