using System.Net.Http;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace NugetUploadHelper;

/// <summary>
/// NuGet 源仓库连接工厂，按当前认证模式创建相互隔离的 <see cref="SourceRepository"/>。
/// </summary>
internal static class NuGetRepositoryFactory
{
    /// <summary>
    /// 按当前配置与认证方式创建独立的 <see cref="SourceRepository"/>，防止跨认证模式复用凭据。
    /// </summary>
    /// <param name="settings">上传与认证配置。</param>
    /// <param name="includeReadApiKey">是否在元数据读取请求中也注入 <c>X-NuGet-ApiKey</c> 请求头。</param>
    /// <returns>配置完成的 NuGet 源仓库对象。</returns>
    public static SourceRepository Create(UploadSettings settings, bool includeReadApiKey = false)
    {
        var uri = settings.Validate();
        var source = new PackageSource(uri.AbsoluteUri)
        {
            // 以 .json 结尾判定为 NuGet V3 (index.json)，其余视为 V2 (如 Nexus hosted 源)
            ProtocolVersion = uri.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ? 3 : 2,
            AllowInsecureConnections = uri.Scheme == Uri.UriSchemeHttp
        };

        // 仅在账号密码模式下挂载 HTTP Basic 凭据
        if (settings.Authentication == AuthenticationMode.Account)
        {
            source.Credentials = new PackageSourceCredential(
                source.Source,
                settings.Username,
                settings.Password,
                isPasswordClearText: true,
                validAuthenticationTypesText: "basic");
        }

        var providers = Repository.Provider.GetCoreV3().ToList();

        // NuGet 默认仅在推送时发送 X-NuGet-ApiKey；若需使用 API Key 查询私有源元数据，则注入自定义 HTTP 处理器
        if (includeReadApiKey && settings.Authentication == AuthenticationMode.ApiKey)
        {
            providers.Insert(0, new Lazy<INuGetResourceProvider>(() => new ApiKeyReadHandlerProvider(settings.ApiKey, uri)));
        }

        return new SourceRepository(source, providers);
    }
}

/// <summary>
/// 自定义 NuGet HTTP 资源提供器，用于在只读元数据查询管线中挂载限定源范围的 API Key 处理器。
/// </summary>
internal sealed class ApiKeyReadHandlerProvider : ResourceProvider
{
    private readonly string _key;
    private readonly Uri _source;

    /// <summary>
    /// 初始化 <see cref="ApiKeyReadHandlerProvider"/>，声明在默认 <see cref="HttpHandlerResourceV3Provider"/> 之前生效。
    /// </summary>
    /// <param name="key">NuGet API Key。</param>
    /// <param name="source">目标仓库源地址。</param>
    public ApiKeyReadHandlerProvider(string key, Uri source)
        : base(typeof(HttpHandlerResource), nameof(ApiKeyReadHandlerProvider), nameof(HttpHandlerResourceV3Provider))
    {
        _key = key;
        _source = source;
    }

    /// <summary>
    /// 复用官方 HTTP 管线，并在外层包装限定仓库范围的 API Key 请求头处理器。
    /// </summary>
    public override async Task<Tuple<bool, INuGetResource?>> TryCreate(SourceRepository source, CancellationToken token)
    {
        var result = await new HttpHandlerResourceV3Provider().TryCreate(source, token).ConfigureAwait(false);
        if (!result.Item1 || result.Item2 is not HttpHandlerResource original)
        {
            throw new InvalidOperationException("无法创建仓库的 HTTP 读取连接。");
        }

        // API Key 属于敏感自定义请求头，禁用自动重定向以防凭据随 301/302 泄露到外部站点
        original.ClientHandler.AllowAutoRedirect = false;

        var handler = new ScopedApiKeyHandler(_key, _source)
        {
            InnerHandler = original.MessageHandler
        };

        return Tuple.Create(true, (INuGetResource?)new HttpHandlerResourceV3(original.ClientHandler, handler));
    }
}

/// <summary>
/// 限定仓库路径范围的 HTTP 消息处理器，确保仅向目标仓库下的请求附加 <c>X-NuGet-ApiKey</c>。
/// </summary>
internal sealed class ScopedApiKeyHandler(string key, Uri source) : DelegatingHandler
{
    /// <summary>
    /// 计算受信任的基础 URI 范围：若源地址指向 <c>index.json</c> 则取其所在目录，否则取确保以斜杠结尾的目录路径。
    /// </summary>
    private readonly Uri _scope = source.AbsolutePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        ? new Uri(source, ".")
        : new Uri(source.AbsoluteUri.TrimEnd('/') + "/");

    /// <summary>
    /// 发送 HTTP 请求前检查目标地址是否处于配置仓库范围内，仅在范围内附加 API Key。
    /// </summary>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri is { } target && _scope.IsBaseOf(target))
        {
            request.Headers.Remove("X-NuGet-ApiKey");
            request.Headers.Add("X-NuGet-ApiKey", key);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
