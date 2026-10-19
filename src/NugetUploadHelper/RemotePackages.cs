using System.Net;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetUploadHelper;

/// <summary>
/// 远程 NuGet 仓库包元数据读取接口。
/// </summary>
internal interface IRemotePackageReader
{
    /// <summary>
    /// 读取指定包 ID 在服务器上的全部版本及发布时间，并响应取消请求。
    /// </summary>
    /// <param name="packageId">要查询的包 ID。</param>
    /// <param name="settings">仓库地址与认证配置。</param>
    /// <param name="log">日志进度汇报接口。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>该包在服务器上的版本快照。</returns>
    Task<RemotePackageSnapshot> ReadAsync(string packageId, UploadSettings settings, IProgress<UploadLog> log, CancellationToken token);
}

/// <summary>
/// 基于 <see cref="PackageMetadataResource"/> 的远程包元数据读取实现。
/// </summary>
/// <param name="timeoutSeconds">单次查询的超时秒数（默认 30 秒）。</param>
internal sealed class NuGetPackageReader(int timeoutSeconds = 30) : IRemotePackageReader
{
    /// <inheritdoc />
    public async Task<RemotePackageSnapshot> ReadAsync(
        string packageId,
        UploadSettings settings,
        IProgress<UploadLog> log,
        CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));
        var logger = new UploadLogger(log, settings);

        try
        {
            var repository = NuGetRepositoryFactory.Create(settings, includeReadApiKey: true);
            var resource = await repository.GetResourceAsync<PackageMetadataResource>(timeout.Token).ConfigureAwait(false)
                ?? throw new InvalidOperationException("仓库未提供 NuGet 包元数据查询接口。");

            // 禁用本地 HTTP 缓存以确保每次刷新均获取服务器实时状态
            using var cache = new SourceCacheContext
            {
                NoCache = true,
                DirectDownload = true
            };

            var metadata = await resource.GetMetadataAsync(
                packageId,
                includePrerelease: true,
                includeUnlisted: true,
                cache,
                logger,
                timeout.Token).ConfigureAwait(false);

            // 若底层协议仅通过日志记录错误而未抛出异常，主动转为异常以防误判为“服务器无此包”
            if (logger.LastError is { } error)
            {
                throw new InvalidOperationException(error);
            }

            // 过滤匹配目标包 ID 的版本；NuGet V2 未提供发布时间时常返回 1900 年占位值，需转为 null
            var versions = metadata
                .Where(item => string.Equals(item.Identity.Id, packageId, StringComparison.OrdinalIgnoreCase))
                .Select(item => new RemotePackageVersion(
                    item.Identity.Version,
                    item.Published is { Year: > 1900 } date ? date : null));

            return new RemotePackageSnapshot(versions);
        }
        catch (OperationCanceledException ex) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException($"读取服务器包信息超时（{timeoutSeconds} 秒）。", ex);
        }
        catch (Exception ex) when (GetAccessStatus(ex) is { } status)
        {
            string hint = settings.Authentication == AuthenticationMode.ApiKey
                ? "当前 API Key 未能获得读取权限，请切换为具有仓库读取权限的账号密码后刷新。"
                : "请检查账号密码及目标仓库的读取权限。";
            throw new UnauthorizedAccessException($"服务器拒绝读取（HTTP {(int)status}）。{hint}", ex);
        }
    }

    /// <summary>
    /// 沿内部异常链查找 HTTP 401/403 状态码，以便给出针对性的认证/权限提示。
    /// </summary>
    private static HttpStatusCode? GetAccessStatus(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            if (current is HttpRequestException { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden } http)
            {
                return http.StatusCode;
            }
        }

        return null;
    }
}

/// <summary>
/// 批量比对本地包与远程服务器版本信息的协调服务。
/// </summary>
/// <param name="reader">远程包元数据读取器。</param>
internal sealed class PackageComparison(IRemotePackageReader reader)
{
    /// <summary>
    /// 按包 ID 分组去重后依次查询服务器版本信息，并将结果同步至同 ID 的所有本地包项。
    /// </summary>
    /// <param name="packages">待比对的本地包列表。</param>
    /// <param name="settings">当前仓库与认证配置。</param>
    /// <param name="log">日志输出进度接口。</param>
    /// <param name="progress">查询进度回调接口。</param>
    /// <param name="token">取消令牌。</param>
    /// <returns>包含已加载数量、失败数量和取消状态的比对结果。</returns>
    public async Task<ComparisonResult> RefreshAsync(
        IReadOnlyList<PackageItem> packages,
        UploadSettings settings,
        IProgress<UploadLog> log,
        IProgress<BatchProgress> progress,
        CancellationToken token)
    {
        settings.Validate();

        foreach (var package in packages)
        {
            package.SetRemote(RemoteQueryState.NotQueried);
        }

        int loaded = 0;
        int failed = 0;
        var redactor = new SecretRedactor(settings);

        // 相同包 ID 只需向服务器发起一次请求
        var groups = packages
            .GroupBy(item => item.Id, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        int completed = 0;
        foreach (var group in groups)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }

            foreach (var package in group)
            {
                package.SetRemote(RemoteQueryState.Loading);
            }

            progress.Report(new BatchProgress(completed, groups.Length, group.First()));

            try
            {
                var snapshot = await reader.ReadAsync(group.Key, settings, log, token);
                foreach (var package in group)
                {
                    package.SetRemote(RemoteQueryState.Loaded, snapshot);
                }

                loaded += group.Count();
                string summary = snapshot.Latest is { } latest
                    ? $"服务器最新版本 {latest.Version}"
                    : "服务器无此包";
                log.Report(new UploadLog(DateTime.Now, "信息", $"{group.Key}：{summary}。"));
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // 用户主动取消时，将当前未完成的组恢复为“待刷新”状态
                foreach (var package in group)
                {
                    package.SetRemote(RemoteQueryState.NotQueried);
                }

                break;
            }
            catch (Exception ex)
            {
                string error = redactor.Redact(ex.Message);
                foreach (var package in group)
                {
                    package.SetRemote(RemoteQueryState.Failed, error: error);
                }

                failed += group.Count();
                log.Report(new UploadLog(DateTime.Now, "错误", $"{group.Key} 查询失败：{error}"));
            }

            completed++;
            progress.Report(new BatchProgress(completed, groups.Length, null));
        }

        return new ComparisonResult(loaded, failed, token.IsCancellationRequested);
    }
}
