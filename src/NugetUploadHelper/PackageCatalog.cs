using NuGet.Packaging;

namespace NugetUploadHelper;

/// <summary>
/// 本地 NuGet 包目录管理器，负责从 <c>.nupkg</c> 压缩包提取真实元数据并按完整路径去重。
/// </summary>
internal sealed class PackageCatalog
{
    /// <summary>
    /// 已导入包的完整路径索引（忽略大小写，适配 Windows 文件系统）。
    /// </summary>
    private readonly Dictionary<string, PackageItem> _items = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 批量读取指定路径集合中的 NuGet 包并按完整路径去重，同时收集可恢复的单文件错误。
    /// </summary>
    /// <param name="paths">待导入的 <c>.nupkg</c> 文件路径列表。</param>
    /// <returns>包含新增包、错误列表和重复路径计数的导入结果。</returns>
    public PackageImportResult Add(IEnumerable<string> paths)
    {
        var added = new List<PackageItem>();
        var errors = new List<string>();
        int duplicates = 0;

        foreach (string path in paths)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                if (_items.ContainsKey(fullPath))
                {
                    duplicates++;
                    continue;
                }

                var item = Read(fullPath);
                _items.Add(fullPath, item);
                added.Add(item);
            }
            catch (Exception ex) when (ex is IOException
                or InvalidDataException
                or UnauthorizedAccessException
                or ArgumentException
                or System.Xml.XmlException
                or NuGet.Packaging.Core.PackagingException)
            {
                errors.Add($"{Path.GetFileName(path)}：{ex.Message}");
            }
        }

        return new PackageImportResult(added, errors, duplicates);
    }

    /// <summary>
    /// 从普通 <c>.nupkg</c> 包内部的 <c>.nuspec</c> 清单读取真实包 ID 和规范化版本号，不依赖外部文件名推断。
    /// </summary>
    /// <param name="path">包文件路径。</param>
    /// <returns>解析得到的 <see cref="PackageItem"/> 实例。</returns>
    /// <exception cref="ArgumentException">当文件不是普通 <c>.nupkg</c> 包或是符号包时抛出。</exception>
    /// <exception cref="InvalidDataException">当包内缺少有效包 ID 时抛出。</exception>
    public static PackageItem Read(string path)
    {
        if (!path.EndsWith(".nupkg", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith(".symbols.nupkg", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("请选择普通 .nupkg 包，暂不支持符号包。");
        }

        using var stream = File.OpenRead(path);
        using var reader = new PackageArchiveReader(stream);
        var identity = reader.GetIdentity();

        if (string.IsNullOrWhiteSpace(identity.Id))
        {
            throw new InvalidDataException("包中缺少 ID。");
        }

        return new PackageItem(
            Path.GetFullPath(path),
            identity.Id,
            identity.Version.ToNormalizedString(),
            stream.Length);
    }

    /// <summary>
    /// 移除指定包的路径去重记录，允许后续重新导入该文件。
    /// </summary>
    /// <param name="item">要移除的包项。</param>
    public void Remove(PackageItem item) => _items.Remove(item.FilePath);

    /// <summary>
    /// 清空所有已记录的包路径索引。
    /// </summary>
    public void Clear() => _items.Clear();
}
