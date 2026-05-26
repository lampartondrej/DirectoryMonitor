using DirectoryMonitor.Application.Models;
using DirectoryMonitor.Domain.Entities;

namespace DirectoryMonitor.Application.Interfaces;

public interface IChangeDetectionService
{
    List<FileChange> DetectChanges(DirectorySnapshot? previousSnapshot, DirectorySnapshot currentSnapshot);
}
