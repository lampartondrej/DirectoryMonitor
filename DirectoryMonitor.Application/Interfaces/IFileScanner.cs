namespace DirectoryMonitor.Application.Interfaces;

public interface IFileScanner
{
    /// <summary>
    /// Returns all file paths found recursively under the given directory.
    /// </summary>
    IEnumerable<string> ScanFiles(string directoryPath);
}
