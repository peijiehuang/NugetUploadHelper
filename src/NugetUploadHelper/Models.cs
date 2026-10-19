using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using NuGet.Versioning;

namespace NugetUploadHelper;

/// <summary>
/// NuGet 仓库认证方式。
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuthenticationMode>))]
internal enum AuthenticationMode
{
    /// <summary>
    /// 使用 X-NuGet-ApiKey 请求头进行认证。
    /// </summary>
    ApiKey,

    /// <summary>
    /// 使用 HTTP Basic 账号密码进行认证。
    /// </summary>
    Account
}

/// <summary>
/// 本地包上传状态。
/// </summary>
internal enum PackageState
{
    /// <summary>
    /// 等待上传。
    /// </summary>
    Pending,

    /// <summary>
    /// 正在上传中。
    /// </summary>
    Uploading,

    /// <summary>
    /// 上传成功。
    /// </summary>
    Succeeded,

    /// <summary>
    /// 上传失败。
    /// </summary>
    Failed
}

/// <summary>
/// 服务器包版本信息查询状态。
/// </summary>
internal enum RemoteQueryState
{
    /// <summary>
    /// 尚未查询或配置变更后待刷新。
    /// </summary>
    NotQueried,

    /// <summary>
    /// 正在从服务器查询元数据。
    /// </summary>
    Loading,

    /// <summary>
    /// 已成功获取服务器元数据。
    /// </summary>
    Loaded,

    /// <summary>
    /// 查询失败（如超时、网络异常或无读取权限）。
    /// </summary>
    Failed
}

/// <summary>
/// 上传与仓库连接配置模型，支持明文序列化至本地 <c>settings.json</c>。
/// </summary>
internal sealed record UploadSettings
{
    /// <summary>
    /// 仓库地址输入框的默认预览提示（Placeholder）示例地址。
    /// </summary>
    public const string DefaultSource = "http://nexus.example.com:8081/repository/nuget-hosted/";

    /// <summary>
    /// 单个包上传的默认超时时间（秒）。
    /// </summary>
    public const int DefaultTimeoutSeconds = 300;

    /// <summary>
    /// NuGet 仓库源地址（支持 V2 地址或 V3 <c>index.json</c> 地址，默认为空，由用户输入实际地址）。
    /// </summary>
    public string Source { get; init; } = "";

    /// <summary>
    /// 当前启用的认证模式。
    /// </summary>
    public AuthenticationMode Authentication { get; init; } = AuthenticationMode.ApiKey;

    /// <summary>
    /// API Key 认证密钥。
    /// </summary>
    public string ApiKey { get; init; } = "";

    /// <summary>
    /// 账号密码认证的用户名。
    /// </summary>
    public string Username { get; init; } = "";

    /// <summary>
    /// 账号密码认证的明文密码。
    /// </summary>
    public string Password { get; init; } = "";

    /// <summary>
    /// 最近一次导入包的目录路径。
    /// </summary>
    public string RecentDirectory { get; init; } = "";

    /// <summary>
    /// 验证仓库地址和当前认证方式所需字段；保存/加载配置时允许暂不填写仓库地址与凭据。
    /// </summary>
    /// <param name="requireCredentials">是否要求仓库地址及当前选中认证方式的凭据必填。</param>
    /// <returns>验证通过的仓库 <see cref="Uri"/> 对象（当仅保存空地址配置时返回占位 URI）。</returns>
    /// <exception cref="ArgumentException">当地址格式非法、字段为空或包含非法字符时抛出。</exception>
    public Uri Validate(bool requireCredentials = true)
    {
        if (!Enum.IsDefined(Authentication)
            || Source is null
            || ApiKey is null
            || Username is null
            || Password is null
            || RecentDirectory is null)
        {
            throw new ArgumentException("配置字段或认证方式无效，请检查配置文件。");
        }

        string trimmedSource = Source.Trim();
        if (trimmedSource.Length == 0 && !requireCredentials)
        {
            return new Uri(DefaultSource);
        }

        if (!Uri.TryCreate(trimmedSource, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("请填写有效的 HTTP 或 HTTPS 仓库地址，不要在地址中填写账号密码或片段。");
        }

        if (!requireCredentials)
        {
            return uri;
        }

        if (Authentication == AuthenticationMode.ApiKey && string.IsNullOrWhiteSpace(ApiKey))
        {
            throw new ArgumentException("请填写 API Key。");
        }

        if (Authentication == AuthenticationMode.Account && (string.IsNullOrWhiteSpace(Username) || Password.Length == 0))
        {
            throw new ArgumentException("请填写用户名和密码。");
        }

        // 防止请求头注入或 HTTP Basic 认证格式被冒号破坏
        if ((Authentication == AuthenticationMode.ApiKey && (ApiKey.Contains('\r') || ApiKey.Contains('\n')))
            || (Authentication == AuthenticationMode.Account && (Username.Contains('\r') || Username.Contains('\n') || Username.Contains(':'))))
        {
            throw new ArgumentException("API Key 和用户名中不能包含换行，用户名中不能包含冒号。");
        }

        return uri;
    }
}

/// <summary>
/// 服务器端单个包版本的元数据记录。
/// </summary>
/// <param name="Version">NuGet 语义化版本号。</param>
/// <param name="Published">服务器记录的发布时间（若服务器未提供则为 <c>null</c>）。</param>
internal sealed record RemotePackageVersion(NuGetVersion Version, DateTimeOffset? Published);

/// <summary>
/// 服务器端指定包 ID 的版本快照集合（按版本号降序排列）。
/// </summary>
internal sealed class RemotePackageSnapshot(IEnumerable<RemotePackageVersion> versions)
{
    /// <summary>
    /// 按 NuGet 版本规则降序排列的所有服务器版本列表。
    /// </summary>
    public IReadOnlyList<RemotePackageVersion> Versions { get; } = versions
        .OrderByDescending(item => item.Version, VersionComparer.VersionRelease)
        .ToArray();

    /// <summary>
    /// 服务器上的最新版本（若服务器无此包则为 <c>null</c>）。
    /// </summary>
    public RemotePackageVersion? Latest => Versions.FirstOrDefault();

    /// <summary>
    /// 按 NuGet 版本语义（含预发布标签，忽略元数据后缀）判断指定版本字符串是否已存在于服务器。
    /// </summary>
    /// <param name="version">待检查的本地版本字符串。</param>
    public bool Contains(string version) => Contains(NuGetVersion.Parse(version));

    /// <summary>
    /// 按 NuGet 版本语义判断指定 <see cref="NuGetVersion"/> 是否已存在于服务器。
    /// </summary>
    /// <param name="localVersion">待检查的本地版本对象。</param>
    public bool Contains(NuGetVersion localVersion) =>
        Versions.Any(item => VersionComparer.VersionRelease.Equals(item.Version, localVersion));
}

/// <summary>
/// 表示待上传列表中的单个 NuGet 包项，支持 WinForms 数据绑定通知。
/// </summary>
internal sealed class PackageItem(string path, string id, string version, long size) : INotifyPropertyChanged
{
    private readonly NuGetVersion _parsedVersion = NuGetVersion.Parse(version);
    private bool _selected = true;
    private PackageState _state;
    private string _error = "";
    private RemotePackageSnapshot? _remote;

    /// <summary>
    /// 包文件的完整本地路径。
    /// </summary>
    public string FilePath { get; } = path;

    /// <summary>
    /// 包含扩展名的完整文件名（用于在列表中展示）。
    /// </summary>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// 从 <c>.nuspec</c> 清单读取的真实包 ID。
    /// </summary>
    public string Id { get; } = id;

    /// <summary>
    /// 规范化后的本地包版本字符串。
    /// </summary>
    public string Version { get; } = version;

    /// <summary>
    /// 包文件字节大小。
    /// </summary>
    public long Size { get; } = size;

    /// <summary>
    /// 格式化后的文件大小文本（自动按 KB 或 MB 显示）。
    /// </summary>
    public string SizeText => Size < 1024 * 1024
        ? $"{Size / 1024d:0.#} KB"
        : $"{Size / (1024d * 1024):0.##} MB";

    /// <summary>
    /// 是否勾选参与批量上传。
    /// </summary>
    public bool Selected
    {
        get => _selected;
        set
        {
            if (_selected == value)
            {
                return;
            }

            _selected = value;
            Changed();
        }
    }

    /// <summary>
    /// 当前包的上传状态。
    /// </summary>
    public PackageState State
    {
        get => _state;
        private set
        {
            if (_state == value)
            {
                return;
            }

            _state = value;
            Changed();
            Changed(nameof(StatusText));
        }
    }

    /// <summary>
    /// 上传失败时的脱敏错误详情。
    /// </summary>
    public string Error
    {
        get => _error;
        private set
        {
            if (_error == value)
            {
                return;
            }

            _error = value;
            Changed();
        }
    }

    /// <summary>
    /// 用于界面展示的上传状态中文文本。
    /// </summary>
    public string StatusText => State switch
    {
        PackageState.Uploading => "上传中",
        PackageState.Succeeded => "上传成功",
        PackageState.Failed => "上传失败",
        _ => "待上传"
    };

    /// <summary>
    /// 服务器版本信息的查询状态。
    /// </summary>
    public RemoteQueryState RemoteState { get; private set; }

    /// <summary>
    /// 服务器查询失败时的脱敏错误原因。
    /// </summary>
    public string RemoteError { get; private set; } = "";

    /// <summary>
    /// 服务器最新版本展示文本。
    /// </summary>
    public string RemoteVersionText => RemoteState switch
    {
        RemoteQueryState.Loading => "查询中…",
        RemoteQueryState.Failed => "查询失败",
        RemoteQueryState.Loaded => _remote?.Latest?.Version.ToNormalizedString() ?? "未发布",
        _ => "待刷新"
    };

    /// <summary>
    /// 服务器最新版本的上传/发布时间展示文本（转换为本机时区）。
    /// </summary>
    public string RemotePublishedText => RemoteState == RemoteQueryState.Loaded
        ? _remote?.Latest is { } latest
            ? latest.Published?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "服务器未提供"
            : "—"
        : "—";

    /// <summary>
    /// 本地版本与服务器版本列表的比对结论文本。
    /// </summary>
    public string ComparisonText
    {
        get
        {
            if (RemoteState != RemoteQueryState.Loaded)
            {
                return RemoteState == RemoteQueryState.Failed ? "无法比对" : "—";
            }

            if (_remote?.Latest is not { } latest)
            {
                return "服务器无此包";
            }

            if (_remote.Contains(_parsedVersion))
            {
                return "同版本已存在";
            }

            return VersionComparer.VersionRelease.Compare(_parsedVersion, latest.Version) > 0
                ? "本地版本较新"
                : "本地版本较旧";
        }
    }

    /// <summary>
    /// 鼠标悬停提示中展示的服务器版本历史明细（最多展示前 30 个版本）。
    /// </summary>
    public string RemoteDetail => RemoteState switch
    {
        RemoteQueryState.Loaded =>
            $"包 ID：{Id}{Environment.NewLine}服务器版本（按版本降序，时间为本机时区；Published）{Environment.NewLine}"
            + string.Join(
                Environment.NewLine,
                _remote!.Versions.Take(30).Select(item =>
                    $"{item.Version.ToNormalizedString()}  {item.Published?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "服务器未提供时间"}")),
        RemoteQueryState.Failed => RemoteError,
        _ => "点击“刷新服务器信息”查询版本和上传/发布时间。"
    };

    /// <summary>
    /// 更新单包上传状态和错误详情，并通知数据绑定刷新。
    /// </summary>
    /// <param name="state">新的上传状态。</param>
    /// <param name="error">错误详情（默认为空）。</param>
    public void SetState(PackageState state, string error = "")
    {
        Error = error;
        State = state;
    }

    /// <summary>
    /// 更新服务器查询状态、版本快照和错误信息，并统一通知相关绑定属性刷新。
    /// </summary>
    /// <param name="state">新的服务器查询状态。</param>
    /// <param name="snapshot">查询成功时的版本快照。</param>
    /// <param name="error">查询失败时的错误原因。</param>
    public void SetRemote(RemoteQueryState state, RemotePackageSnapshot? snapshot = null, string error = "")
    {
        RemoteState = state;
        _remote = snapshot;
        RemoteError = error;

        Changed(nameof(RemoteState));
        Changed(nameof(RemoteVersionText));
        Changed(nameof(RemotePublishedText));
        Changed(nameof(ComparisonText));
        Changed(nameof(RemoteDetail));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// 向数据绑定层通知指定属性发生变化。
    /// </summary>
    /// <param name="property">发生变化的属性名。</param>
    private void Changed([CallerMemberName] string? property = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

/// <summary>
/// 界面日志条目记录。
/// </summary>
/// <param name="Timestamp">日志产生时间。</param>
/// <param name="Level">日志级别（如“信息”“警告”“错误”“完成”）。</param>
/// <param name="Message">日志文本内容。</param>
internal sealed record UploadLog(DateTime Timestamp, string Level, string Message);

/// <summary>
/// 批量任务（上传或服务器查询）的进度快照。
/// </summary>
/// <param name="Completed">已完成数量。</param>
/// <param name="Total">任务总数量。</param>
/// <param name="Current">当前正在处理的包项（刚完成单步时可为 <c>null</c>）。</param>
internal sealed record BatchProgress(int Completed, int Total, PackageItem? Current);

/// <summary>
/// 批量上传任务的最终统计结果。
/// </summary>
/// <param name="Succeeded">上传成功数量。</param>
/// <param name="Failed">上传失败数量。</param>
/// <param name="Remaining">未处理的剩余数量。</param>
/// <param name="Stopped">是否因用户请求停止而提前结束。</param>
internal sealed record BatchResult(int Succeeded, int Failed, int Remaining, bool Stopped);

/// <summary>
/// 服务器版本批量比对任务的汇总结果。
/// </summary>
/// <param name="Loaded">成功更新版本信息的包数量。</param>
/// <param name="Failed">查询失败的包数量。</param>
/// <param name="Canceled">是否被用户取消。</param>
internal sealed record ComparisonResult(int Loaded, int Failed, bool Canceled);

/// <summary>
/// 本地包文件批量导入结果。
/// </summary>
/// <param name="Added">本次新加入列表的包集合。</param>
/// <param name="Errors">导入失败的文件错误描述列表。</param>
/// <param name="Duplicates">因完整路径重复而被忽略的文件数量。</param>
internal sealed record PackageImportResult(IReadOnlyList<PackageItem> Added, IReadOnlyList<string> Errors, int Duplicates);
