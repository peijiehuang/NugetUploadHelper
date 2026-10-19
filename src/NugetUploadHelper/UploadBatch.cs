namespace NugetUploadHelper;

/// <summary>
/// 批量上传队列执行器，负责按顺序推送包、隔离单包异常并支持在当前包完成后平滑停止。
/// </summary>
internal sealed class UploadBatch(IPackageUploader uploader)
{
    private int _stopRequested;
    private int _running;

    /// <summary>
    /// 请求在当前正在上传的包完成后停止后续队列，避免强行中断正在传输的 HTTP 请求导致状态不明。
    /// </summary>
    public void RequestStop() => Interlocked.Exchange(ref _stopRequested, 1);

    /// <summary>
    /// 顺序执行批量上传队列；单包失败时记录错误并继续后续包，最后返回统计结果。
    /// </summary>
    /// <param name="packages">待上传的包集合。</param>
    /// <param name="settings">仓库与认证配置。</param>
    /// <param name="log">日志进度接口。</param>
    /// <param name="progress">批次进度接口。</param>
    /// <returns>包含成功、失败、剩余数量及停止标志的批次结果。</returns>
    /// <exception cref="InvalidOperationException">当已有上传任务正在运行时抛出。</exception>
    public async Task<BatchResult> RunAsync(
        IReadOnlyList<PackageItem> packages,
        UploadSettings settings,
        IProgress<UploadLog> log,
        IProgress<BatchProgress> progress)
    {
        settings.Validate();
        if (Interlocked.Exchange(ref _running, 1) != 0)
        {
            throw new InvalidOperationException("已有上传任务正在运行。");
        }

        Interlocked.Exchange(ref _stopRequested, 0);
        int succeeded = 0;
        int failed = 0;
        var redactor = new SecretRedactor(settings);

        try
        {
            // 启动前将本次队列中的所有包重置为“待上传”状态
            foreach (var package in packages)
            {
                package.SetState(PackageState.Pending);
            }

            foreach (var package in packages)
            {
                if (Volatile.Read(ref _stopRequested) != 0)
                {
                    break;
                }

                package.SetState(PackageState.Uploading);
                progress.Report(new BatchProgress(succeeded + failed, packages.Count, package));

                try
                {
                    await uploader.PushAsync(package, settings, log);
                    package.SetState(PackageState.Succeeded);
                    succeeded++;
                    log.Report(new UploadLog(DateTime.Now, "完成", $"{package.Id} {package.Version} 上传成功。"));
                }
                catch (Exception ex)
                {
                    string error = redactor.Redact(ex.Message);
                    package.SetState(PackageState.Failed, error);
                    failed++;
                    log.Report(new UploadLog(DateTime.Now, "错误", $"{package.Id} {package.Version}：{error}"));
                }

                progress.Report(new BatchProgress(succeeded + failed, packages.Count, null));
            }

            int remaining = packages.Count - succeeded - failed;
            bool stopped = Volatile.Read(ref _stopRequested) != 0;
            return new BatchResult(succeeded, failed, remaining, stopped);
        }
        finally
        {
            Volatile.Write(ref _running, 0);
        }
    }
}
