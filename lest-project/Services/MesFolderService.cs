namespace LestProject.Services;

/// <summary>
/// MES 資料以「瀏覽資料夾」為主；檔名與副檔名不限（客戶提供）。
/// </summary>
internal sealed class MesFolderService
{
    public string? RootFolder { get; private set; }

    public void SetRoot(string folder)
    {
        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException(folder);
        RootFolder = folder;
    }

    public IReadOnlyList<string> ListFiles(string? searchPattern = "*")
    {
        if (string.IsNullOrWhiteSpace(RootFolder))
            return Array.Empty<string>();

        return Directory
            .EnumerateFiles(RootFolder, searchPattern ?? "*", SearchOption.AllDirectories)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>在 MES 資料夾內依副檔名偏好挑一個檔；找不到則回傳第一個檔或 null。</summary>
    public string? PreferFile(params string[] preferredExtensions)
    {
        var all = ListFiles();
        if (all.Count == 0) return null;

        foreach (var ext in preferredExtensions)
        {
            var hit = all.FirstOrDefault(p =>
                p.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
            if (hit != null) return hit;
        }

        return all[0];
    }
}
