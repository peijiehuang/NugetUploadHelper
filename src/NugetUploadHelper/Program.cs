namespace NugetUploadHelper;

/// <summary>
/// 应用程序入口类。
/// </summary>
internal static class Program
{
    /// <summary>
    /// 初始化 WinForms 运行环境，组装配置存储、上传器与服务器包读取服务并启动主窗体。
    /// </summary>
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settingsStore = new SettingsStore(Path.Combine(AppContext.BaseDirectory, "settings.json"));
        var uploader = new NuGetUploader();
        var packageReader = new NuGetPackageReader();

        Application.Run(new MainForm(settingsStore, uploader, packageReader));
    }
}
