using System.Net;
using System.Text;
using System.Xml.Linq;
using NuGet.Versioning;
using Xunit;

namespace NugetUploadHelper.Tests;

/// <summary>
/// 服务器包版本查询、分页合并、版本语义比对及 API Key 作用域安全限制测试。
/// </summary>
public sealed class RemotePackageTests
{
    private static readonly DateTimeOffset Published = DateTimeOffset.Parse("2026-10-18T08:30:45Z");

    /// <summary>
    /// 构造指向指定本地测试源的 API Key 查询配置。
    /// </summary>
    private static UploadSettings Settings(string source) => new()
    {
        Source = source,
        ApiKey = "metadata-read-key"
    };

    /// <summary>
    /// 创建带固定服务器发布时间的版本快照。
    /// </summary>
    private static RemotePackageSnapshot Snapshot(params string[] versions) =>
        new(versions.Select(version => new RemotePackageVersion(NuGetVersion.Parse(version), Published)));

    /// <summary>
    /// 验证完整文件名展示与 <c>.nuspec</c> 真实包 ID 相互独立，且版本比对严格遵循 NuGet 语义化版本排序规则。
    /// </summary>
    [Fact]
    public void FullFilenameIsIndependentOfNuspecIdAndComparisonsUseNuGetVersionOrdering()
    {
        using var directory = new TestDirectory();
        var item = directory.Package("Actual.Id", "1.10.0", "完整 文件名.1.10.0.nupkg");
        Assert.Equal("完整 文件名.1.10.0.nupkg", item.FileName);

        item.SetRemote(RemoteQueryState.Loaded, Snapshot("1.9.0", "1.2.0"));
        Assert.Equal("1.9.0", item.RemoteVersionText);
        Assert.Equal("本地版本较新", item.ComparisonText);

        item.SetRemote(RemoteQueryState.Loaded, Snapshot("1.9.0", "1.10.0", "2.0.0-beta.1"));
        Assert.Equal("2.0.0-beta.1", item.RemoteVersionText);
        Assert.Equal("同版本已存在", item.ComparisonText);

        item.SetRemote(RemoteQueryState.Loaded, Snapshot("2.0.0"));
        Assert.Equal("本地版本较旧", item.ComparisonText);

        item.SetRemote(RemoteQueryState.Loaded, Snapshot());
        Assert.Equal("服务器无此包", item.ComparisonText);
    }

    /// <summary>
    /// 验证 NuGet V2 元数据分页读取、<c>X-NuGet-ApiKey</c> 请求头注入以及服务器发布时间解析。
    /// </summary>
    [Fact]
    public async Task ReadsV2VersionsPublishedTimeAndAllPagesWithApiKey()
    {
        await using var feed = new TestFeed(context =>
        {
            Assert.Equal("metadata-read-key", context.Request.Headers["X-NuGet-ApiKey"]);
            Assert.Null(context.Request.Headers["Authorization"]);

            bool second = context.Request.QueryString["page"] == "2";
            string root = context.Request.Url!.GetLeftPart(UriPartial.Authority) + "/repository/nuget-hosted/";
            string xml = AtomFeed(
                root,
                "Actual.Id",
                second ? [new FeedVersion("1.10.0", Published)] : [new FeedVersion("1.9.0", Published.AddDays(-1))],
                second ? null : root + "FindPackagesById()?id='Actual.Id'&page=2");

            return RespondXml(context, xml);
        });

        var result = await new NuGetPackageReader().ReadAsync(
            "Actual.Id",
            Settings(feed.Source),
            new RecordingProgress<UploadLog>(),
            CancellationToken.None);

        Assert.Equal(2, result.Versions.Count);
        Assert.Equal("1.10.0", result.Latest!.Version.ToNormalizedString());
        Assert.Equal(Published, result.Latest.Published);
        Assert.Equal(2, feed.Requests.Count(request => request.Path.Contains("FindPackagesById", StringComparison.OrdinalIgnoreCase)));
        Assert.All(feed.Requests, request => Assert.Equal("GET", request.Method));
        Assert.Contains(
            "Actual.Id",
            Uri.UnescapeDataString(feed.Requests.First(request => request.Path.Contains("FindPackagesById", StringComparison.OrdinalIgnoreCase)).Path));
    }

    /// <summary>
    /// 验证账号密码模式下查询元数据仅使用 HTTP Basic 认证，不附带未启用的 API Key。
    /// </summary>
    [Fact]
    public async Task AccountMetadataUsesBasicAuthenticationAndNoInactiveKey()
    {
        string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("reader:read-password"));
        await using var feed = new TestFeed(context =>
        {
            if (context.Request.Headers["Authorization"] != expected)
            {
                context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Nexus\"";
                return TestFeed.Error(context, 401);
            }

            string root = context.Request.Url!.GetLeftPart(UriPartial.Authority) + "/repository/nuget-hosted/";
            return RespondXml(context, AtomFeed(root, "Example.Library", [new FeedVersion("1.2.3", null)]));
        });

        var settings = Settings(feed.Source) with
        {
            Authentication = AuthenticationMode.Account,
            Username = "reader",
            Password = "read-password"
        };

        var result = await new NuGetPackageReader().ReadAsync(
            "Example.Library",
            settings,
            new RecordingProgress<UploadLog>(),
            CancellationToken.None);

        Assert.Null(result.Latest!.Published);
        Assert.DoesNotContain(feed.Requests, request => request.ApiKey is not null);

        using var directory = new TestDirectory();
        var item = directory.Package();
        item.SetRemote(RemoteQueryState.Loaded, result);
        Assert.Equal("服务器未提供", item.RemotePublishedText);
    }

    /// <summary>
    /// 验证相同包 ID 只向服务器查询一次，且严格区分“查询失败”与“服务器无此包”。
    /// </summary>
    [Fact]
    public async Task ComparisonQueriesEachIdOnceAndKeepsFailureDistinctFromNotFound()
    {
        using var directory = new TestDirectory();
        var first = directory.Package("Shared.Id", "1.0.0");
        var second = directory.Package("Shared.Id", "1.1.0");
        var failed = directory.Package("Denied");
        var absent = directory.Package("New.Package");

        var calls = new List<string>();
        var reader = new FakeRemoteReader((id, _) =>
        {
            calls.Add(id);
            if (id == "Denied")
            {
                throw new HttpRequestException("403 denied metadata-read-key");
            }

            return Task.FromResult(id == "Shared.Id" ? Snapshot("1.0.0") : Snapshot());
        });

        var result = await new PackageComparison(reader).RefreshAsync(
            [first, failed, second, absent],
            Settings("http://localhost/feed/"),
            new RecordingProgress<UploadLog>(),
            new RecordingProgress<BatchProgress>(),
            CancellationToken.None);

        Assert.Equal(new ComparisonResult(3, 1, false), result);
        Assert.Equal(3, calls.Count);
        Assert.Equal("同版本已存在", first.ComparisonText);
        Assert.Equal("本地版本较新", second.ComparisonText);
        Assert.Equal("无法比对", failed.ComparisonText);
        Assert.DoesNotContain("metadata-read-key", failed.RemoteError);
        Assert.Equal("服务器无此包", absent.ComparisonText);
    }

    /// <summary>
    /// 验证用户取消查询后，尚未完成查询的行恢复为“待刷新”状态。
    /// </summary>
    [Fact]
    public async Task CancelingComparisonLeavesUnfinishedRowsUnqueried()
    {
        using var directory = new TestDirectory();
        var item = directory.Package();
        using var cancellation = new CancellationTokenSource();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var reader = new FakeRemoteReader(async (_, token) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, token);
            return Snapshot();
        });

        var operation = new PackageComparison(reader).RefreshAsync(
            [item],
            Settings("http://localhost/feed/"),
            new RecordingProgress<UploadLog>(),
            new RecordingProgress<BatchProgress>(),
            cancellation.Token);

        await started.Task;
        cancellation.Cancel();

        Assert.True((await operation).Canceled);
        Assert.Equal(RemoteQueryState.NotQueried, item.RemoteState);
    }

    /// <summary>
    /// 验证仓库返回 401 未授权时明确报错，不会误判为“服务器无此包”。
    /// </summary>
    [Fact]
    public async Task UnauthorizedMetadataIsNotTreatedAsAnEmptyRepository()
    {
        using var directory = new TestDirectory();
        var item = directory.Package();
        await using var feed = new TestFeed(context => TestFeed.Error(context, 401));

        var result = await new PackageComparison(new NuGetPackageReader()).RefreshAsync(
            [item],
            Settings(feed.Source),
            new RecordingProgress<UploadLog>(),
            new RecordingProgress<BatchProgress>(),
            CancellationToken.None);

        Assert.Equal(1, result.Failed);
        Assert.Equal(RemoteQueryState.Failed, item.RemoteState);
        Assert.Equal("无法比对", item.ComparisonText);
        Assert.Contains("401", item.RemoteError);
        Assert.Contains("读取权限", item.RemoteError);
    }

    /// <summary>
    /// 验证元数据读取超时会抛出 <see cref="TimeoutException"/>，而不是返回空版本列表。
    /// </summary>
    [Fact]
    public async Task MetadataTimeoutIsReported()
    {
        await using var feed = new TestFeed(async context =>
        {
            await Task.Delay(1800);
            await RespondXml(context, AtomFeed("http://localhost/", "Example.Library", []));
        });

        await Assert.ThrowsAsync<TimeoutException>(() => new NuGetPackageReader(1).ReadAsync(
            "Example.Library",
            Settings(feed.Source),
            new RecordingProgress<UploadLog>(),
            CancellationToken.None));
    }

    /// <summary>
    /// 验证每次刷新均绕过缓存获取最新数据，且能正确处理正常的空包响应。
    /// </summary>
    [Fact]
    public async Task RefreshReadsFreshMetadataAndEmptyFeedIsDistinctFromFailure()
    {
        string? version = "1.0.0";
        await using var feed = new TestFeed(context => RespondXml(
            context,
            AtomFeed(
                context.Request.Url!.GetLeftPart(UriPartial.Authority) + "/repository/nuget-hosted/",
                "Example.Library",
                version is null ? [] : [new FeedVersion(version, Published)])));

        var reader = new NuGetPackageReader();
        var log = new RecordingProgress<UploadLog>();

        Assert.Equal(
            "1.0.0",
            (await reader.ReadAsync("Example.Library", Settings(feed.Source), log, CancellationToken.None)).Latest!.Version.ToNormalizedString());

        version = "1.1.0";
        Assert.Equal(
            "1.1.0",
            (await reader.ReadAsync("Example.Library", Settings(feed.Source), log, CancellationToken.None)).Latest!.Version.ToNormalizedString());

        version = null;
        Assert.Empty((await reader.ReadAsync("Example.Library", Settings(feed.Source), log, CancellationToken.None)).Versions);
    }

    /// <summary>
    /// 验证 <see cref="ScopedApiKeyHandler"/> 仅向配置的仓库路径范围附加 API Key，不向其他主机或兄弟路径泄露。
    /// </summary>
    [Fact]
    public async Task MetadataKeyIsRestrictedToTheConfiguredRepository()
    {
        var recorder = new HeaderRecorder();
        var handler = new ScopedApiKeyHandler("scoped-key", new Uri("https://nexus.example/repository/feed/index.json"))
        {
            InnerHandler = recorder
        };

        using var client = new System.Net.Http.HttpClient(handler);
        using var first = await client.GetAsync("https://nexus.example/repository/feed/registration/library/index.json");
        using var second = await client.GetAsync("https://other.example/repository/feed/index.json");
        using var third = await client.GetAsync("https://nexus.example/repository/feed-other/index.json");

        Assert.Equal(new string?[] { "scoped-key", null, null }, recorder.Keys);
    }

    /// <summary>
    /// 验证服务器返回 302 重定向时不会将 API Key 自动转发至其他服务器。
    /// </summary>
    [Fact]
    public async Task RedirectDoesNotForwardMetadataApiKeyToAnotherServer()
    {
        await using var other = new TestFeed(context => RespondXml(context, AtomFeed("http://localhost/", "Example.Library", [])));
        await using var feed = new TestFeed(context =>
        {
            context.Response.RedirectLocation = other.Source;
            return TestFeed.Error(context, 302);
        });

        await Assert.ThrowsAnyAsync<Exception>(() => new NuGetPackageReader(2).ReadAsync(
            "Example.Library",
            Settings(feed.Source),
            new RecordingProgress<UploadLog>(),
            CancellationToken.None));

        Assert.NotEmpty(feed.Requests);
        Assert.Empty(other.Requests);
    }

    /// <summary>
    /// 用于记录每次 HTTP 请求中 <c>X-NuGet-ApiKey</c> 头的测试消息处理器。
    /// </summary>
    private sealed class HeaderRecorder : System.Net.Http.HttpMessageHandler
    {
        public List<string?> Keys { get; } = [];

        /// <inheritdoc />
        protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(
            System.Net.Http.HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Keys.Add(request.Headers.TryGetValues("X-NuGet-ApiKey", out var values) ? values.Single() : null);
            return Task.FromResult(new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    /// <summary>
    /// 测试用 Atom Feed 版本条目。
    /// </summary>
    internal sealed record FeedVersion(string Version, DateTimeOffset? Published);

    /// <summary>
    /// 生成包含指定版本列表、发布时间和可选下一页链接的 NuGet V2 OData Atom XML 响应。
    /// </summary>
    internal static string AtomFeed(string root, string id, FeedVersion[] versions, string? next = null)
    {
        XNamespace atom = "http://www.w3.org/2005/Atom";
        XNamespace d = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        XNamespace m = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";

        var feed = new XElement(
            atom + "feed",
            new XAttribute(XNamespace.Xmlns + "d", d),
            new XAttribute(XNamespace.Xmlns + "m", m));

        foreach (var version in versions)
        {
            var properties = new XElement(
                m + "properties",
                new XElement(d + "Id", id),
                new XElement(d + "Version", version.Version),
                new XElement(d + "Description", "Test package"),
                new XElement(d + "Listed", "true"));

            if (version.Published is { } date)
            {
                properties.Add(new XElement(d + "Published", date.ToString("O")));
            }

            feed.Add(new XElement(
                atom + "entry",
                new XElement(atom + "id", "urn:" + id + ":" + version.Version),
                new XElement(atom + "title", id),
                new XElement(
                    atom + "content",
                    new XAttribute("type", "application/zip"),
                    new XAttribute("src", root + id + "." + version.Version + ".nupkg")),
                properties));
        }

        if (next is not null)
        {
            feed.Add(new XElement(atom + "link", new XAttribute("rel", "next"), new XAttribute("href", next)));
        }

        return new XDocument(feed).ToString();
    }

    /// <summary>
    /// 以 <c>application/atom+xml</c> 内容类型向测试客户端输出 XML 响应。
    /// </summary>
    internal static async Task RespondXml(HttpListenerContext context, string xml)
    {
        context.Response.StatusCode = 200;
        context.Response.ContentType = "application/atom+xml";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }
}
