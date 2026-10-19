using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace NugetUploadHelper.Tests;

/// <summary>
/// 为每个测试用例提供独立临时目录的夹具，测试结束时自动校验并清理。
/// </summary>
internal sealed class TestDirectory : IDisposable
{
    private static readonly string TestRoot =
        Path.GetFullPath(Path.Combine(Path.GetTempPath(), "NugetUploadHelperTests")) + Path.DirectorySeparatorChar;

    /// <summary>
    /// 当前测试实例专属的临时目录完整路径。
    /// </summary>
    public string PathName { get; } = Path.Combine(TestRoot, Guid.NewGuid().ToString("N"));

    /// <summary>
    /// 初始化并创建隔离测试目录。
    /// </summary>
    public TestDirectory() => Directory.CreateDirectory(PathName);

    /// <summary>
    /// 获取当前测试目录下指定文件名的完整路径。
    /// </summary>
    public string FilePath(string name) => Path.Combine(PathName, name);

    /// <summary>
    /// 在测试目录中生成一个包含最小有效 <c>.nuspec</c> 清单的 <c>.nupkg</c> 包并解析为 <see cref="PackageItem"/>。
    /// </summary>
    /// <param name="id">包内部 ID。</param>
    /// <param name="version">包版本号。</param>
    /// <param name="name">自定义文件名（默认使用 <c>{id}.{version}.nupkg</c>）。</param>
    public PackageItem Package(string id = "Example.Library", string version = "1.2.3", string? name = null)
    {
        string path = FilePath(name ?? $"{id}.{version}.nupkg");
        using (var archive = ZipFile.Open(path, ZipArchiveMode.Create))
        {
            using var writer = new StreamWriter(archive.CreateEntry(id + ".nuspec").Open());
            writer.Write(
                $"<?xml version=\"1.0\"?><package><metadata><id>{id}</id><version>{version}</version><authors>Tests</authors><description>Integration test package</description></metadata></package>");
        }

        return PackageCatalog.Read(path);
    }

    /// <summary>
    /// 校验目录位于受控测试根路径下后递归删除。
    /// </summary>
    public void Dispose()
    {
        string target = Path.GetFullPath(PathName);
        if (!target.StartsWith(TestRoot, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid cleanup path.");
        }

        if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }
}

/// <summary>
/// 线程安全的进度记录器，用于收集异步测试期间产生的所有进度通知。
/// </summary>
internal sealed class RecordingProgress<T> : IProgress<T>
{
    private readonly List<T> _values = [];

    /// <summary>
    /// 获取已记录的所有进度项快照。
    /// </summary>
    public IReadOnlyList<T> Values
    {
        get
        {
            lock (_values)
            {
                return _values.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public void Report(T value)
    {
        lock (_values)
        {
            _values.Add(value);
        }
    }
}

/// <summary>
/// 模拟包上传器，通过外部委托控制单包上传成功、失败或阻塞时机。
/// </summary>
internal sealed class FakeUploader(Func<PackageItem, Task> action) : IPackageUploader
{
    /// <inheritdoc />
    public Task PushAsync(PackageItem package, UploadSettings settings, IProgress<UploadLog> log) => action(package);
}

/// <summary>
/// 本地测试 HTTP 源捕获到的单次请求记录。
/// </summary>
internal sealed record ReceivedRequest(
    string Method,
    string Path,
    string? ApiKey,
    string? Authorization,
    byte[] Body);

/// <summary>
/// 基于 <see cref="HttpListener"/> 的本地模拟 NuGet V2 仓库服务，支持捕获请求头、请求体并自定义响应。
/// </summary>
internal sealed class TestFeed : IAsyncDisposable
{
    private readonly HttpListener _listener = new();
    private readonly CancellationTokenSource _shutdown = new();
    private readonly List<ReceivedRequest> _requests = [];
    private readonly List<Task> _handlers = [];
    private readonly Task _loop;
    private readonly Func<HttpListenerContext, Task> _respond;

    /// <summary>
    /// 模拟仓库的完整源地址。
    /// </summary>
    public string Source { get; }

    /// <summary>
    /// 已接收到的所有 HTTP 请求快照。
    /// </summary>
    public IReadOnlyList<ReceivedRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return _requests.ToArray();
            }
        }
    }

    /// <summary>
    /// 分配空闲回环端口并启动本地 HTTP 监听服务。
    /// </summary>
    /// <param name="respond">自定义请求处理委托（默认返回 HTTP 201 Created）。</param>
    public TestFeed(Func<HttpListenerContext, Task>? respond = null)
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        int port = ((IPEndPoint)socket.LocalEndpoint).Port;
        socket.Stop();

        Source = $"http://127.0.0.1:{port}/repository/nuget-hosted/";
        _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
        _respond = respond ?? (context =>
        {
            context.Response.StatusCode = 201;
            return Task.CompletedTask;
        });

        _listener.Start();
        _loop = AcceptAsync();
    }

    /// <summary>
    /// 后台循环接收客户端连接并派发异步处理任务。
    /// </summary>
    private async Task AcceptAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync().WaitAsync(_shutdown.Token);
            }
            catch (Exception ex) when (ex is OperationCanceledException or HttpListenerException or ObjectDisposedException)
            {
                break;
            }

            var task = HandleAsync(context);
            lock (_handlers)
            {
                _handlers.Add(task);
            }
        }
    }

    /// <summary>
    /// 读取并记录请求的认证头与字节载荷，随后执行自定义响应委托。
    /// </summary>
    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            using var buffer = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(buffer);
            var request = new ReceivedRequest(
                context.Request.HttpMethod,
                context.Request.RawUrl!,
                context.Request.Headers["X-NuGet-ApiKey"],
                context.Request.Headers["Authorization"],
                buffer.ToArray());

            lock (_requests)
            {
                _requests.Add(request);
            }

            await _respond(context);
            context.Response.Close();
        }
        catch (Exception ex) when (ex is IOException or HttpListenerException or ObjectDisposedException)
        {
            if (!_shutdown.IsCancellationRequested)
            {
                throw;
            }
        }
    }

    /// <summary>
    /// 辅助方法：向客户端返回指定的 HTTP 状态码。
    /// </summary>
    public static Task Error(HttpListenerContext context, int status)
    {
        context.Response.StatusCode = status;
        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止监听并等待所有进行中的请求处理完毕。
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _shutdown.Cancel();
        _listener.Close();
        await _loop;

        Task[] handlers;
        lock (_handlers)
        {
            handlers = _handlers.ToArray();
        }

        await Task.WhenAll(handlers);
        _shutdown.Dispose();
    }
}
