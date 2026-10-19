namespace NugetUploadHelper.Tests;

/// <summary>
/// 用于单元测试的模拟远程包元数据读取器，通过注入委托替代真实网络请求。
/// </summary>
internal sealed class FakeRemoteReader(Func<string, CancellationToken, Task<RemotePackageSnapshot>> action) : IRemotePackageReader
{
    /// <inheritdoc />
    public Task<RemotePackageSnapshot> ReadAsync(
        string id,
        UploadSettings settings,
        IProgress<UploadLog> log,
        CancellationToken token) => action(id, token);
}
