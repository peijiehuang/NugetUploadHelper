using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetUploadHelper;

/// <summary>
/// 单个 NuGet 包上传服务接口。
/// </summary>
internal interface IPackageUploader
{
    /// <summary>
    /// 将指定包推送至目标仓库，并通过进度接口实时报告过程日志。
    /// </summary>
    /// <param name="package">待上传的本地包项。</param>
    /// <param name="settings">仓库与认证配置。</param>
    /// <param name="log">日志输出进度接口。</param>
    Task PushAsync(PackageItem package, UploadSettings settings, IProgress<UploadLog> log);
}

/// <summary>
/// 基于 NuGet 官方 <see cref="PackageUpdateResource"/> 的包上传服务实现。
/// </summary>
/// <param name="timeoutSeconds">单包上传超时时间（默认 300 秒）。</param>
internal sealed class NuGetUploader(int timeoutSeconds = UploadSettings.DefaultTimeoutSeconds) : IPackageUploader
{
    /// <inheritdoc />
    public async Task PushAsync(PackageItem package, UploadSettings settings, IProgress<UploadLog> log)
    {
        var uri = settings.Validate();

        // 文件加入队列后可能在磁盘上被重新编译或替换，上传前重新读取并校验包信息一致性
        var current = PackageCatalog.Read(package.FilePath);
        if (current.Id != package.Id || current.Version != package.Version || current.Size != package.Size)
        {
            throw new InvalidDataException("包文件已发生变化，请移除后重新添加。");
        }

        var repository = NuGetRepositoryFactory.Create(settings);
        var resource = await repository.GetResourceAsync<PackageUpdateResource>().ConfigureAwait(false)
            ?? throw new InvalidOperationException("仓库未提供 NuGet 上传接口。");

        var logger = new UploadLogger(log, settings);
        string key = settings.Authentication == AuthenticationMode.ApiKey ? settings.ApiKey : "";

        try
        {
            await resource.PushAsync(
                [package.FilePath],
                symbolSource: null!,
                timeoutInSecond: timeoutSeconds,
                disableBuffering: false,
                getApiKey: _ => key,
                getSymbolApiKey: _ => null!,
                noServiceEndpoint: false,
                skipDuplicate: false,
                allowSnupkg: false,
                allowInsecureConnections: uri.Scheme == Uri.UriSchemeHttp,
                log: logger).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            throw new TimeoutException($"上传超时（{timeoutSeconds} 秒）。服务器可能已接收该包，请核对仓库后再重试。", ex);
        }

        // NuGet.Protocol 在部分 HTTP 状态码下只记录 Error 日志而不抛出异常，此处兜底确保失败状态准确
        if (logger.LastError is { } error)
        {
            throw new InvalidOperationException(error);
        }
    }
}

/// <summary>
/// 敏感凭据脱敏器，用于过滤日志和异常消息中的明文密码与 API Key。
/// </summary>
internal sealed class SecretRedactor(UploadSettings settings)
{
    /// <summary>
    /// 按长度降序排列的非空白敏感凭据列表（优先替换较长凭据以防前缀截断）。
    /// </summary>
    private readonly string[] _secrets = new[] { settings.ApiKey, settings.Password }
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .OrderByDescending(value => value.Length)
        .ToArray();

    /// <summary>
    /// 将消息中出现的明文凭据及其 URL 编码形式替换为 <c>[已隐藏]</c>。
    /// </summary>
    /// <param name="message">原始日志或异常消息。</param>
    /// <returns>脱敏后的安全文本。</returns>
    public string Redact(string message)
    {
        if (string.IsNullOrEmpty(message) || _secrets.Length == 0)
        {
            return message;
        }

        foreach (string secret in _secrets)
        {
            message = message.Replace(secret, "[已隐藏]", StringComparison.Ordinal);
            message = message.Replace(Uri.EscapeDataString(secret), "[已隐藏]", StringComparison.Ordinal);
        }

        return message;
    }
}

/// <summary>
/// 适配 NuGet 官方客户端的日志接收器，负责脱敏转发并捕获协议层错误。
/// </summary>
internal sealed class UploadLogger(IProgress<UploadLog> progress, UploadSettings settings) : LoggerBase
{
    private readonly SecretRedactor _redactor = new(settings);

    /// <summary>
    /// 记录最后一次出现的错误级别日志内容。
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// 过滤调试日志，对消息脱敏后汇报至界面，并记录 Error 级别消息。
    /// </summary>
    public override void Log(ILogMessage message)
    {
        if (message.Level is LogLevel.Debug or LogLevel.Verbose)
        {
            return;
        }

        string text = _redactor.Redact(message.Message);
        if (message.Level == LogLevel.Error)
        {
            LastError = text;
        }

        string level = message.Level switch
        {
            LogLevel.Error => "错误",
            LogLevel.Warning => "警告",
            _ => "信息"
        };

        progress.Report(new UploadLog(DateTime.Now, level, text));
    }

    /// <summary>
    /// 异步日志入口，直接复用同步日志处理逻辑。
    /// </summary>
    public override Task LogAsync(ILogMessage message)
    {
        Log(message);
        return Task.CompletedTask;
    }
}
