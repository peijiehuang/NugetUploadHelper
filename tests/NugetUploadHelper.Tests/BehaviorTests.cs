using System.Text.Json;
using Xunit;

namespace NugetUploadHelper.Tests;

/// <summary>
/// 本地目录去重、配置读写、凭据脱敏与批量队列核心行为单元测试。
/// </summary>
public sealed class BehaviorTests
{
    /// <summary>
    /// 验证包元数据读取、按完整路径（忽略大小写）去重以及损坏文件错误收集互不干扰。
    /// </summary>
    [Fact]
    public void CatalogReadsMetadataDeduplicatesFullPathsAndReportsCorruptFiles()
    {
        using var directory = new TestDirectory();
        var package = directory.Package(id: "实际包名", name: "中文 文件.nupkg");
        File.WriteAllText(directory.FilePath("broken.nupkg"), "not a package");

        var catalog = new PackageCatalog();
        var result = catalog.Add([
            package.FilePath,
            package.FilePath.ToUpperInvariant(),
            directory.FilePath("broken.nupkg")
        ]);

        Assert.Equal("实际包名", Assert.Single(result.Added).Id);
        Assert.Equal("1.2.3", result.Added[0].Version);
        Assert.Equal(1, result.Duplicates);
        Assert.Single(result.Errors);

        catalog.Remove(result.Added[0]);
        Assert.Single(catalog.Add([package.FilePath]).Added);
    }

    /// <summary>
    /// 验证配置文件不存在时返回默认值，且所有字段（含密码）均以明文序列化与还原。
    /// </summary>
    [Fact]
    public void SettingsRoundTripAllFieldsAsPlaintextAndMissingFileUsesDefaults()
    {
        using var directory = new TestDirectory();
        var store = new SettingsStore(directory.FilePath("settings.json"));
        Assert.Equal(new UploadSettings(), store.Load());

        var settings = new UploadSettings
        {
            Source = "http://localhost:8081/repository/nuget-hosted/",
            Authentication = AuthenticationMode.Account,
            Username = "用户",
            Password = "plain password",
            ApiKey = "plain-key",
            RecentDirectory = @"D:\中文 包"
        };

        store.Save(settings);
        using var json = JsonDocument.Parse(File.ReadAllText(store.FilePath));
        Assert.Equal(settings.Password, json.RootElement.GetProperty("Password").GetString());
        Assert.Equal(settings, store.Load());

        store.Save(settings with { Authentication = AuthenticationMode.ApiKey });
        Assert.Equal(AuthenticationMode.ApiKey, store.Load().Authentication);
    }

    /// <summary>
    /// 验证损坏的配置文件在读取时明确抛出 <see cref="InvalidDataException"/>，且不会破坏原文件内容。
    /// </summary>
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{\"Authentication\":\"invalid\"}")]
    [InlineData("{\"Source\":null}")]
    [InlineData("{\"Password\":null}")]
    public void CorruptConfigurationDoesNotSilentlyBecomeDefaults(string json)
    {
        using var directory = new TestDirectory();
        var store = new SettingsStore(directory.FilePath("settings.json"));
        File.WriteAllText(store.FilePath, json);

        Assert.Throws<InvalidDataException>(() => store.Load());
        Assert.Equal(json, File.ReadAllText(store.FilePath));
    }

    /// <summary>
    /// 验证文件被锁定时读写异常正常抛出，且保存失败不会覆盖原配置文件或遗留临时文件。
    /// </summary>
    [Fact]
    public void ReadAndWriteFailuresAreReportedAndFailedSaveKeepsOriginalFile()
    {
        using var directory = new TestDirectory();
        var store = new SettingsStore(directory.FilePath("settings.json"));
        store.Save(new UploadSettings());
        string original = File.ReadAllText(store.FilePath);

        using (File.Open(store.FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Throws<IOException>(() => store.Load());
            var error = Record.Exception(() => store.Save(new UploadSettings { ApiKey = "changed" }));
            Assert.True(error is IOException or UnauthorizedAccessException);
        }

        Assert.Equal(original, File.ReadAllText(store.FilePath));
        Assert.Empty(Directory.GetFiles(directory.PathName, "*.tmp"));
    }

    /// <summary>
    /// 验证批量队列在单包上传失败后继续上传后续包，并对错误详情和日志中的敏感凭据进行脱敏。
    /// </summary>
    [Fact]
    public async Task BatchContinuesAfterFailureAndRedactsErrors()
    {
        using var directory = new TestDirectory();
        var first = directory.Package("First");
        var second = directory.Package("Second");
        var settings = new UploadSettings
        {
            Source = "http://localhost:8081/repository/nuget-hosted/",
            ApiKey = "secret-key",
            Password = "secret-password"
        };

        var uploader = new FakeUploader(item =>
            item == first
                ? Task.FromException(new IOException("secret-key secret-password"))
                : Task.CompletedTask);

        var log = new RecordingProgress<UploadLog>();
        var result = await new UploadBatch(uploader).RunAsync(
            [first, second],
            settings,
            log,
            new RecordingProgress<BatchProgress>());

        Assert.Equal(new BatchResult(1, 1, 0, false), result);
        Assert.Equal(PackageState.Succeeded, second.State);
        Assert.DoesNotContain("secret", first.Error);
        Assert.DoesNotContain(log.Values, entry =>
            entry.Message.Contains("secret-key") || entry.Message.Contains("secret-password"));
    }

    /// <summary>
    /// 验证停止请求会等待当前正在上传的包完成，并将剩余未处理的包保留为“待上传”状态。
    /// </summary>
    [Fact]
    public async Task StopWaitsForCurrentPackageAndLeavesRemainingPending()
    {
        using var directory = new TestDirectory();
        var first = directory.Package("First");
        var second = directory.Package("Second");
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;

        var batch = new UploadBatch(new FakeUploader(async _ =>
        {
            calls++;
            started.SetResult();
            await release.Task;
        }));

        var operation = batch.RunAsync(
            [first, second],
            new UploadSettings
            {
                Source = "http://localhost:8081/repository/nuget-hosted/",
                ApiKey = "test-key"
            },
            new RecordingProgress<UploadLog>(),
            new RecordingProgress<BatchProgress>());

        await started.Task;
        batch.RequestStop();
        Assert.False(operation.IsCompleted);

        release.SetResult();
        Assert.Equal(new BatchResult(1, 0, 1, true), await operation);
        Assert.Equal(1, calls);
        Assert.Equal(PackageState.Pending, second.State);
    }

    /// <summary>
    /// 验证未填写仓库地址、非 HTTP/HTTPS 协议或内嵌账号密码的源地址无法通过配置校验。
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("file:///D:/packages")]
    [InlineData("not a url")]
    [InlineData("https://user:password@example.org/feed")]
    public void InvalidSourcesCannotStartUpload(string source) =>
        Assert.Throws<ArgumentException>(() => new UploadSettings { Source = source, ApiKey = "key" }.Validate());
}
