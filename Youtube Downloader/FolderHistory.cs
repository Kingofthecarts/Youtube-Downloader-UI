using System.Xml.Linq;

namespace Youtube_Downloader;

public class FolderHistory
{
    private readonly string historyPath;
    private List<FolderEntry> folders = new();

    public const int MaxFolders = 100;

    public IReadOnlyList<FolderEntry> Folders => folders.AsReadOnly();
    public string HistoryFilePath => historyPath;

    public FolderHistory(string? path = null)
    {
        // Use provided path or default to beside app
        if (string.IsNullOrEmpty(path))
        {
            string appDir = AppPaths.AppDirectory;
            historyPath = Path.Combine(appDir, "folder_history.xml");
        }
        else
        {
            historyPath = path;
        }
        Load();
    }

    public void Load()
    {
        folders.Clear();

        if (!File.Exists(historyPath))
        {
            return;
        }

        try
        {
            var doc = XDocument.Load(historyPath);
            var root = doc.Element("FolderHistory");

            if (root != null)
            {
                foreach (var item in root.Elements("Folder"))
                {
                    var name = item.Element("Name")?.Value ?? item.Value;
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    var lastUsed = DateTime.TryParse(item.Element("LastUsed")?.Value, out var date)
                        ? date
                        : DateTime.MinValue;

                    folders.Add(new FolderEntry
                    {
                        Name = name.Trim(),
                        LastUsed = lastUsed
                    });
                }
            }
        }
        catch
        {
            // Silently fail - file may be corrupted
        }
    }

    public void Save()
    {
        var doc = new XDocument(
            new XElement("FolderHistory",
                folders.Select(f => new XElement("Folder",
                    new XElement("Name", f.Name),
                    new XElement("LastUsed", f.LastUsed.ToString("o"))
                ))
            )
        );

        doc.Save(historyPath);
    }

    /// <summary>
    /// Add or update a folder entry. If it exists, update LastUsed. If new, add it.
    /// </summary>
    public void AddOrUpdate(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return;

        folderName = folderName.Trim();

        var existing = folders.FirstOrDefault(f =>
            f.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.LastUsed = DateTime.Now;
        }
        else
        {
            // Check if we're at max capacity
            if (folders.Count >= MaxFolders)
            {
                // Remove the oldest entry
                var oldest = folders.OrderBy(f => f.LastUsed).First();
                folders.Remove(oldest);
            }

            folders.Add(new FolderEntry
            {
                Name = folderName,
                LastUsed = DateTime.Now
            });
        }

        Save();
    }

    /// <summary>
    /// Remove a folder from the history
    /// </summary>
    public void Remove(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return;

        var entry = folders.FirstOrDefault(f =>
            f.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase));

        if (entry != null)
        {
            folders.Remove(entry);
            Save();
        }
    }

    /// <summary>
    /// Get folder names sorted according to preference
    /// </summary>
    public List<string> GetSortedFolderNames(bool sortByRecent)
    {
        if (sortByRecent)
        {
            return folders
                .OrderByDescending(f => f.LastUsed)
                .Select(f => f.Name)
                .ToList();
        }
        else
        {
            return folders
                .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
                .Select(f => f.Name)
                .ToList();
        }
    }

    /// <summary>
    /// Check if a folder name exists in history
    /// </summary>
    public bool Contains(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName)) return false;
        return folders.Any(f => f.Name.Equals(folderName.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Clear all folder history
    /// </summary>
    public void Clear()
    {
        folders.Clear();
        Save();
    }

    /// <summary>
    /// Update the list with a new set of folder names (for editor)
    /// </summary>
    public void UpdateList(List<string> newFolderNames)
    {
        // Keep existing LastUsed dates where possible
        var newFolders = new List<FolderEntry>();

        foreach (var name in newFolderNames.Take(MaxFolders))
        {
            if (string.IsNullOrWhiteSpace(name)) continue;

            var existing = folders.FirstOrDefault(f =>
                f.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase));

            newFolders.Add(new FolderEntry
            {
                Name = name.Trim(),
                LastUsed = existing?.LastUsed ?? DateTime.Now
            });
        }

        folders = newFolders;
        Save();
    }
}

public class FolderEntry
{
    public string Name { get; set; } = "";
    public DateTime LastUsed { get; set; } = DateTime.Now;
}
