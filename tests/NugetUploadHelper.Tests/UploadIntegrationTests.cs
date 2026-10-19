using System.Text;
using Xunit;

namespace NugetUploadHelper.Tests;

/// <summary>
/// 端到端 HTTP 协议上传集成测试，验证 API Key、HTTP Basic 认证、凭据隔离与超时/错误处理。
/// </summary>
public sealed class UploadIntegrationTests
{
    /// <summary>
    /// 验证 API Key 模式下使用 Nexus V2 PUT 端点上传真实 <c>.nupkg</c> 字节内容，且不附带账号认证头。
    /// </summary>
    [Fact]
    public async Task ApiKeyUploadUsesNexusV2EndpointAndSendsActualPackage()
    {
        using var directory = new TestDirectory();
        var package = directory.Package(name: "中文 包.1.2.3.nupkg");
        await using var feed = new TestFeed();

        var settings = new UploadSettings
        {
            Source = feed.Source,
            ApiKey = "test-api-key",
            Username = "inactive-user",
            Password = "inactive-password"
        };

        var log = new RecordingProgress<UploadLog>();
        await new NuGetUploader().PushAsync(package, settings, log);

        var request = Assert.Single(feed.Requests);
        Assert.Equal("PUT", request.Method);
        Assert.Equal("/repository/nuget-hosted/", request.Path);
        Assert.Equal(settings.ApiKey, request.ApiKey);
        Assert.Null(request.Authorization);

        byte[] original = File.ReadAllBytes(package.FilePath);
        Assert.True(request.Body.AsSpan().IndexOf(original) >= 0, "Multipart request must contain the original .nupkg bytes.");
        Assert.DoesNotContain(log.Values, entry => entry.Message.Contains(settings.ApiKey));
    }

    /// <summary>
    /// 验证账号密码模式下正确响应 HTTP 401 Basic 认证挑战，且不会发送未启用的 API Key。
    /// </summary>
    [Fact]
    public async Task AccountUploadAnswersBasicChallengeWithoutUsingInactiveApiKey()
    {
        using var directory = new TestDirectory();
        var package = directory.Package();
        string authorization = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("publisher:account-password"));

        await using var feed = new TestFeed(context =>
        {
            if (context.Request.Headers["Authorization"] == authorization)
            {
                return TestFeed.Error(context, 201);
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Nexus\"";
            return TestFeed.Error(context, 401);
        });

        var settings = new UploadSettings
        {
            Source = feed.Source,
            Authentication = AuthenticationMode.Account,
            Username = "publisher",
            Password = "account-password",
            ApiKey = "inactive-api-key"
        };

        var log = new RecordingProgress<UploadLog>();
        await new NuGetUploader().PushAsync(package, settings, log);

        Assert.Contains(feed.Requests, request => request.Authorization == authorization);
        Assert.DoesNotContain(feed.Requests, request => request.ApiKey == settings.ApiKey);
        Assert.DoesNotContain(log.Values, entry => entry.Message.Contains(settings.Password));
    }

    /// <summary>
    /// 验证对同一仓库地址切换认证方式或修改密码后，不会复用先前连接池中的旧凭据。
    /// </summary>
    [Fact]
    public async Task SwitchingModesAndPasswordsDoesNotReusePreviousCredentials()
    {
        using var directory = new TestDirectory();
        var package = directory.Package();
        string expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("publisher:first-password"));

        await using var feed = new TestFeed(context =>
        {
            if (context.Request.Headers["X-NuGet-ApiKey"] == "active-api-key"
                || context.Request.Headers["Authorization"] == expected)
            {
                return TestFeed.Error(context, 201);
            }

            context.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Nexus\"";
            return TestFeed.Error(context, 401);
        });

        var uploader = new NuGetUploader();
        var log = new RecordingProgress<UploadLog>();
        var account = new UploadSettings
        {
            Source = feed.Source,
            Authentication = AuthenticationMode.Account,
            Username = "publisher",
            Password = "first-password",
            ApiKey = "inactive-key"
        };

        await uploader.PushAsync(package, account, log);
        int afterAccount = feed.Requests.Count;

        await uploader.PushAsync(package, account with { Authentication = AuthenticationMode.ApiKey, ApiKey = "active-api-key" }, log);
        Assert.All(feed.Requests.Skip(afterAccount), request => Assert.Null(request.Authorization));

        expected = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("publisher:changed-password"));
        int afterKey = feed.Requests.Count;

        await uploader.PushAsync(package, account with { Password = "changed-password" }, log);
        Assert.Contains(feed.Requests.Skip(afterKey), request => request.Authorization == expected);
        Assert.DoesNotContain(feed.Requests.Skip(afterKey), request => request.ApiKey == "active-api-key");
    }

    /// <summary>
    /// 验证服务器返回 400/401/403/409 错误状态码时，包状态准确标记为上传失败且包含状态码信息。
    /// </summary>
    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(409)]
    public async Task ServerRejectionsFailThePackage(int status)
    {
        using var directory = new TestDirectory();
        var package = directory.Package();
        await using var feed = new TestFeed(context => TestFeed.Error(context, status));

        var settings = new UploadSettings
        {
            Source = feed.Source,
            ApiKey = "test-api-key"
        };

        var batch = new UploadBatch(new NuGetUploader());
        var result = await batch.RunAsync(
            [package],
            settings,
            new RecordingProgress<UploadLog>(),
            new RecordingProgress<BatchProgress>());

        Assert.Equal(1, result.Failed);
        Assert.Equal(0, result.Succeeded);
        Assert.Equal(PackageState.Failed, package.State);
        Assert.Contains(status.ToString(), package.Error);
    }

    /// <summary>
    /// 验证上传请求超时时抛出 <see cref="TimeoutException"/>，不会误判为上传成功。
    /// </summary>
    [Fact]
    public async Task TimeoutFailsInsteadOfReportingSuccess()
    {
        using var directory = new TestDirectory();
        var package = directory.Package();
        await using var feed = new TestFeed(async context =>
        {
            await Task.Delay(1800);
            context.Response.StatusCode = 201;
        });

        var settings = new UploadSettings
        {
            Source = feed.Source,
            ApiKey = "test-api-key"
        };

        await Assert.ThrowsAsync<TimeoutException>(() =>
            new NuGetUploader(1).PushAsync(package, settings, new RecordingProgress<UploadLog>()));
    }
}
