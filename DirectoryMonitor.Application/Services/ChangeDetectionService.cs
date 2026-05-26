using DirectoryMonitor.Application.Interfaces;
using DirectoryMonitor.Application.Models;
using DirectoryMonitor.Domain.Entities;

namespace DirectoryMonitor.Application.Services;

public class ChangeDetectionService : IChangeDetectionService
{
    public List<FileChange> DetectChanges(DirectorySnapshot? previousSnapshot, DirectorySnapshot currentSnapshot)
    {
        var changes = new List<FileChange>();
        var previousFiles = previousSnapshot?.Files
            .ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            ?? [];

        var currentFiles = currentSnapshot.Files
            .ToDictionary(f => f.RelativePath, StringComparer.OrdinalIgnoreCase);

        foreach (var current in currentSnapshot.Files)
        {
            if (!previousFiles.TryGetValue(current.RelativePath, out var previous))
            {
                changes.Add(new FileChange
                {
                    RelativePath = current.RelativePath,
                    ChangeType = ChangeType.Added,
                    Version = current.Version
                });
            }
            else if (!string.Equals(current.Hash, previous.Hash, StringComparison.Ordinal))
            {
                changes.Add(new FileChange
                {
                    RelativePath = current.RelativePath,
                    ChangeType = ChangeType.Modified,
                    Version = current.Version
                });
            }
        }

        foreach (var previous in previousFiles.Values)
        {
            if (!currentFiles.ContainsKey(previous.RelativePath))
            {
                changes.Add(new FileChange
                {
                    RelativePath = previous.RelativePath,
                    ChangeType = ChangeType.Deleted,
                    Version = previous.Version
                });
            }
        }

        return changes;
    }
}
