using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace NugetUploadHelper.Tests;

/// <summary>
/// 真实 WinForms 主窗体端到端冒烟测试，涵盖配置切换、包导入、服务器版本比对、窗口缩放与批量上传。
/// </summary>
public sealed class FormSmokeTests
{
    /// <summary>
    /// 在独立 STA 线程中启动主窗体并验证完整交互流程与不同窗口尺寸下的控件布局完整性。
    /// </summary>
    [Fact]
    public void FormLoadsSavesSwitchesAuthenticationUploadsAndResizes()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetDefaultFont(new Font("Microsoft YaHei UI", 9));

                // 手动泵送消息期间固定 WinForms 同步上下文，防止 DoEvents 返回后将 async 回调退化到线程池
                WindowsFormsSynchronizationContext.AutoInstall = false;
                using var context = new WindowsFormsSynchronizationContext();
                SynchronizationContext.SetSynchronizationContext(context);

                try
                {
                    ExerciseDesignerPreview();
                    ExerciseForm();
                }
                finally
                {
                    SynchronizationContext.SetSynchronizationContext(null);
                    WindowsFormsSynchronizationContext.AutoInstall = true;
                }
            }
            catch (Exception error)
            {
                failure = error;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(20)), "UI smoke test timed out.");

        if (failure is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
        }
    }

    /// <summary>
    /// 无参构造直接生成完整设计界面，不依赖配置文件、上传器或服务器读取器。
    /// </summary>
    private static void ExerciseDesignerPreview()
    {
        using var form = new MainForm();
        form.Show();
        Application.DoEvents();

        Assert.True(Find<TextBox>(form, "API Key").Visible);
        Assert.True(Find<TextBox>(form, "用户名").Visible);
        Assert.True(Find<TextBox>(form, "明文密码").Visible);
        var grids = Descendants(form).OfType<DataGridView>().ToArray();
        Assert.Equal(8, grids.Single(grid => grid.AccessibleName == "包列表").Columns.Count);
        Assert.Empty(grids.Single(grid => grid.AccessibleName == "上传日志").Rows.Cast<DataGridViewRow>());
        Assert.All(Descendants(form).Where(control => control is not ScrollBar),
            control => Assert.False(string.IsNullOrEmpty(control.Name)));

        string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/ui"));
        Directory.CreateDirectory(output);
        Capture(form, Path.Combine(output, "designer-preview.png"));
        form.Close();
    }

    /// <summary>
    /// 使用演示数据驱动完整界面操作，并在关键节点生成界面截图与控件边界断言。
    /// </summary>
    private static void ExerciseForm()
    {
        using var directory = new TestDirectory();
        var store = new SettingsStore(directory.FilePath("settings.json"));
        store.Save(new UploadSettings
        {
            Authentication = AuthenticationMode.Account,
            Username = "nuget-publisher",
            Password = "demo123456",
            ApiKey = "demo-api-key"
        });

        var packages = new[]
        {
            directory.Package("Example.Core", "1.0.0"),
            directory.Package("Example.Common", "2.3.0"),
            directory.Package("Example.Data", "1.6.2"),
            directory.Package("Example.UI", "1.0.0")
        };

        int uploaded = 0;
        using var form = new MainForm(
            store,
            new FakeUploader(async _ =>
            {
                await Task.Delay(60);
                uploaded++;
            }),
            new FakeRemoteReader((id, _) => Task.FromResult(SmokeMetadata(id))));

        form.Show();
        Application.DoEvents();

        // 验证未填写仓库地址时输入框内容为空，仅通过 PlaceholderText 显示预览提示
        var sourceBox = Find<TextBox>(form, "仓库地址");
        Assert.Equal("", sourceBox.Text);
        Assert.Equal(UploadSettings.DefaultSource, sourceBox.PlaceholderText);

        // 模拟用户真实输入仓库地址，按输入内容显示与保存
        sourceBox.Text = UploadSettings.DefaultSource;

        Assert.Equal("demo123456", Find<TextBox>(form, "明文密码").Text);
        Assert.False(Find<TextBox>(form, "明文密码").UseSystemPasswordChar);

        var apiKeyBox = Find<TextBox>(form, "API Key");
        var usernameBox = Find<TextBox>(form, "用户名");
        var passwordBox = Find<TextBox>(form, "明文密码");
        Find<Button>(form, "API Key").PerformClick();
        Application.DoEvents();
        Assert.True(apiKeyBox.Visible);
        Assert.False(usernameBox.Visible);
        Assert.False(passwordBox.Visible);

        Find<Button>(form, "账号密码").PerformClick();
        Application.DoEvents();
        Assert.False(apiKeyBox.Visible);
        Assert.True(usernameBox.Visible);
        Assert.True(passwordBox.Visible);
        Assert.Equal("demo-api-key", apiKeyBox.Text);
        Assert.Equal("nuget-publisher", usernameBox.Text);
        Assert.Equal("demo123456", passwordBox.Text);
        Find<Button>(form, "保存配置").PerformClick();
        Assert.Equal(AuthenticationMode.Account, store.Load().Authentication);
        Assert.Equal(UploadSettings.DefaultSource, store.Load().Source);

        var load = (Task)typeof(MainForm)
            .GetMethod("AddPathsAsync", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(form, [packages.Select(p => p.FilePath).ToArray()])!;
        PumpUntil(() => load.IsCompleted);
        load.GetAwaiter().GetResult();

        string output = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts/ui"));
        Directory.CreateDirectory(output);

        foreach (string caption in new[] { "用户名", "密码" })
        {
            var label = Descendants(form).OfType<Label>().Single(c => c.Text == caption);
            Assert.True(label.Parent!.ClientRectangle.Contains(label.Bounds), $"Field label clipped: {caption}");
        }

        Find<Button>(form, "刷新服务器信息").PerformClick();
        var grid = Descendants(form).OfType<DataGridView>().Single(control => control.AccessibleName == "包列表");
        Assert.False(grid.AutoGenerateColumns);
        Assert.Equal(8, grid.Columns.Count);
        PumpUntil(() => Find<Button>(form, "刷新服务器信息").Enabled);

        var fileNameColumn = grid.Columns.Cast<DataGridViewColumn>()
            .Single(column => column.DataPropertyName == nameof(PackageItem.FileName));
        Assert.Equal(packages[0].FileName, grid.Rows[0].Cells[fileNameColumn.Index].Value);
        Assert.Equal("本地版本较新", Assert.IsType<PackageItem>(grid.Rows[0].DataBoundItem).ComparisonText);

        // 修改仓库地址后，旧的服务器比对状态应自动失效并重置为“待刷新”
        Find<TextBox>(form, "仓库地址").Text += " ";
        Assert.Equal("待刷新", Assert.IsType<PackageItem>(grid.Rows[0].DataBoundItem).RemoteVersionText);

        Find<TextBox>(form, "仓库地址").Text = UploadSettings.DefaultSource;
        Find<Button>(form, "刷新服务器信息").PerformClick();
        PumpUntil(() => Find<Button>(form, "刷新服务器信息").Enabled);
        Capture(form, Path.Combine(output, "workbench.png"));

        // 验证紧凑与宽屏尺寸下所有可见按钮均未发生裁剪越界
        var originalSize = form.Size;
        foreach (var size in new[] { new Size(1000, 790), new Size(1420, 980) })
        {
            form.Size = size;
            Application.DoEvents();
            foreach (var button in Descendants(form).OfType<Button>().Where(control => control.Visible))
            {
                Assert.True(
                    button.Parent!.ClientRectangle.Contains(button.Bounds),
                    $"Button clipped: {button.Text}; Bounds={button.Bounds}; Parent={button.Parent.ClientRectangle}; Form={form.Size}; DPI={form.DeviceDpi}");
            }
        }

        form.Size = originalSize;
        Application.DoEvents();
        Find<Button>(form, "开始上传").PerformClick();
        PumpUntil(() => uploaded == 4 && Find<Button>(form, "开始上传").Enabled);
        Capture(form, Path.Combine(output, "uploaded.png"));

        Assert.Equal(4, uploaded);
        form.Close();
    }

    /// <summary>
    /// 构造覆盖“本地版本较新”“同版本已存在”“本地版本较旧”和“服务器无此包”四种情形的演示元数据。
    /// </summary>
    private static RemotePackageSnapshot SmokeMetadata(string id)
    {
        string? version = id switch
        {
            "Example.Core" => "0.9.0",
            "Example.Common" => "2.3.0",
            "Example.Data" => "2.0.0",
            _ => null
        };

        return new RemotePackageSnapshot(
            version is null
                ? []
                : [new RemotePackageVersion(NuGet.Versioning.NuGetVersion.Parse(version), DateTimeOffset.Parse("2026-10-18T08:30:45Z"))]);
    }

    /// <summary>
    /// 按 <see cref="Control.AccessibleName"/> 递归查找唯一指定类型的控件。
    /// </summary>
    private static T Find<T>(Control root, string name) where T : Control =>
        Descendants(root).OfType<T>().Single(control => control.AccessibleName == name);

    /// <summary>
    /// 递归遍历控件树中的所有子孙控件。
    /// </summary>
    private static IEnumerable<Control> Descendants(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child))
            {
                yield return nested;
            }
        }
    }

    /// <summary>
    /// 循环泵送 WinForms 消息队列直至条件满足，超时则抛出异常以防测试挂起。
    /// </summary>
    private static void PumpUntil(Func<bool> complete)
    {
        var timer = Stopwatch.StartNew();
        while (!complete())
        {
            if (timer.Elapsed > TimeSpan.FromSeconds(8))
            {
                throw new TimeoutException("UI operation timed out.");
            }

            Application.DoEvents();
            Thread.Sleep(5);
        }

        Application.DoEvents();
    }

    /// <summary>
    /// 将窗体当前视觉状态渲染并保存为 PNG 图像。
    /// </summary>
    private static void Capture(Form form, string path)
    {
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
        bitmap.Save(path);
    }
}
