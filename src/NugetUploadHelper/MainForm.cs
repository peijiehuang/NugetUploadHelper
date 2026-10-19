using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;

namespace NugetUploadHelper;

/// <summary>
/// NuGet 上传助手主窗体，负责协调界面交互、配置读写、本地包导入、服务器版本比对及批量上传流程。
/// </summary>
public partial class MainForm : Form
{
    /// <summary>
    /// 界面日志表格保留的最大行数，超过后自动移除最早记录以控制内存与渲染开销。
    /// </summary>
    private const int MaxLogEntries = 500;

    /// <summary>
    /// 单条日志消息的最大字符长度，超长内容自动截断。
    /// </summary>
    private const int MaxLogMessageLength = 8000;

    /// <summary>
    /// 设计基准 DPI（Windows 标准 100% 缩放）。
    /// </summary>
    private const float DesignDpi = 96f;

    /// <summary>
    /// 触发紧凑布局模式的窗口客户区高度阈值（按 96 DPI 基准计算）。
    /// </summary>
    private const int CompactClientHeight = 740;

    // 界面主题调色板常量
    private static readonly Color BorderColor = Color.FromArgb(220, 225, 231);
    private static readonly Color AccentColor = Color.FromArgb(18, 100, 189);
    private static readonly Color MutedColor = Color.FromArgb(100, 111, 124);
    private static readonly Color SuccessColor = Color.FromArgb(25, 117, 68);
    private static readonly Color WarningColor = Color.FromArgb(151, 93, 21);
    private static readonly Color ErrorColor = Color.FromArgb(185, 49, 62);
    private static readonly Color SelectedRowBackColor = Color.FromArgb(238, 245, 255);

    private static readonly string ApplicationVersion = typeof(MainForm).Assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    private static readonly string RepositoryUrl = typeof(MainForm).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(attribute => attribute.Key == "RepositoryUrl").Value!;

    private readonly float _apiKeyRowHeight;
    private readonly float _accountRowHeight;
    private readonly float _configurationHeight;
    private readonly PackageComparison _comparison = null!;
    private bool _comparing;
    private CancellationTokenSource? _comparisonCancellation;

    private readonly SettingsStore _settingsStore = null!;
    private readonly PackageCatalog _catalog = new();
    private readonly BindingList<PackageItem> _packages = [];
    private readonly UploadBatch _batch = null!;

    private AuthenticationMode _authentication = AuthenticationMode.ApiKey;
    private string _recentDirectory = "";
    private bool _running;
    private bool _importing;
    private bool _loading;
    private bool _closeAfterOperation;

    /// <summary>
    /// 当前是否有后台任务（上传、导入或服务器查询）正在执行。
    /// </summary>
    private bool IsBusy => _running || _importing || _comparing;

    /// <summary>
    /// 设计器入口：只创建控件，不读取配置或启动后台操作。
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
    }

    internal MainForm(SettingsStore settingsStore, IPackageUploader uploader, IRemotePackageReader remoteReader)
        : this()
    {
        _settingsStore = settingsStore;
        _batch = new UploadBatch(uploader);
        _comparison = new PackageComparison(remoteReader);

        // 从设计器布局记录基准尺寸，切换认证时保留编辑器中设置的行高。
        float scale = DeviceDpi / DesignDpi;
        _apiKeyRowHeight = _configuration.RowStyles[2].Height / scale;
        _accountRowHeight = _configuration.RowStyles[3].Height / scale;
        _configurationHeight = _body.RowStyles[0].Height / scale;

        Text = $"NuGet 上传助手 · v{ApplicationVersion}";
        _projectLink.Text = $"GitHub · v{ApplicationVersion}";
        _projectLink.ToolTipText = RepositoryUrl;
        _timeoutStatus.Text = $"上传超时 {UploadSettings.DefaultTimeoutSeconds} 秒";
        _source.PlaceholderText = UploadSettings.DefaultSource;
        // AutoGenerateColumns 不由设计器持久化；绑定前关闭，避免额外生成模型内部字段。
        _grid.AutoGenerateColumns = false;
        _grid.DataSource = _packages;
        EnableDoubleBuffering(_grid);
        EnableDoubleBuffering(_logs);
        EnableDrop(_grid);
        EnableDrop(_dropArea);
        BindEvents();
        UpdateAuthenticationLayout();
        UpdateSelection();
    }

    /// <summary>
    /// 集中绑定窗体与控件事件，确保勾选状态、配置修改标记和任务状态保持同步。
    /// </summary>
    private void BindEvents()
    {
        Shown += (_, _) =>
        {
            FitToWorkingArea();
            LoadSettings();
        };

        ClientSizeChanged += (_, _) => AdjustForWindowSize();
        _keyMode.Click += (_, _) => SetAuthentication(AuthenticationMode.ApiKey);
        _accountMode.Click += (_, _) => SetAuthentication(AuthenticationMode.Account);
        _save.Click += (_, _) => SaveSettings();

        // 任何连接或凭据输入变化时，标记配置未保存并使旧的服务器比对结果失效
        foreach (var box in new[] { _source, _apiKey, _username, _password })
        {
            box.TextChanged += (_, _) =>
            {
                MarkDirty();
                InvalidateRemoteComparisons();
            };
        }

        _add.Click += async (_, _) => await SelectFilesAsync();
        _folder.Click += async (_, _) => await SelectFolderAsync();
        _remove.Click += (_, _) => RemoveSelected();
        _clear.Click += (_, _) =>
        {
            _catalog.Clear();
            _packages.Clear();
            ResetProgress();
        };

        _start.Click += async (_, _) => await StartUploadAsync();
        _refresh.Click += async (_, _) => await RefreshServerAsync();
        _stop.Click += (_, _) => StopAfterCurrent();

        _packages.ListChanged += (_, _) => UpdateSelection();

        // 勾选框点击后立即提交编辑，以便实时更新顶部全选框与底部已选计数
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty)
            {
                _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        };

        // 点击首列表头切换全选/取消全选
        _grid.ColumnHeaderMouseClick += (_, e) =>
        {
            if (e.ColumnIndex == _selectedColumn.Index && !IsBusy)
            {
                SelectAll();
            }
        };

        _grid.CellFormatting += FormatCell;

        // 悬停时展示完整路径、真实包 ID、服务器版本历史与上传错误详情
        _grid.CellToolTipTextNeeded += (_, e) =>
        {
            if (e.RowIndex >= 0 && _grid.Rows[e.RowIndex].DataBoundItem is PackageItem item)
            {
                string tooltip = $"{item.FilePath}{Environment.NewLine}包 ID：{item.Id}{Environment.NewLine}{item.RemoteDetail}";
                if (!string.IsNullOrEmpty(item.Error))
                {
                    tooltip += $"{Environment.NewLine}{item.Error}";
                }

                e.ToolTipText = tooltip;
            }
        };

        // 双击“上传状态”单元格时，若该包存在上传失败信息则弹窗显示完整错误
        _grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex >= 0
                && e.ColumnIndex == _uploadStateColumn.Index
                && _grid.Rows[e.RowIndex].DataBoundItem is PackageItem { Error.Length: > 0 } item)
            {
                MessageBox.Show(this, item.Error, "上传失败详情", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        _projectLink.Click += (_, _) => OpenProjectHomepage();
        FormClosing += HandleClosing;
    }

    /// <summary>
    /// 从当前界面输入控件生成配置快照，保留密码原文并去除仓库地址和用户名两端空白。
    /// </summary>
    private UploadSettings CaptureSettings() => new()
    {
        Source = _source.Text.Trim(),
        Authentication = _authentication,
        ApiKey = _apiKey.Text,
        Username = _username.Text.Trim(),
        Password = _password.Text,
        RecentDirectory = _recentDirectory
    };

    /// <summary>
    /// 启动时读取本地配置；若文件损坏或读取失败则明确弹窗提示，防止静默覆盖用户配置。
    /// </summary>
    private void LoadSettings()
    {
        _loading = true;
        try
        {
            var settings = _settingsStore.Load();
            _source.Text = settings.Source;
            _apiKey.Text = settings.ApiKey;
            _username.Text = settings.Username;
            _password.Text = settings.Password;
            _authentication = settings.Authentication;
            _recentDirectory = settings.RecentDirectory;

            UpdateAuthenticationLayout();
            _saveState.Text = File.Exists(_settingsStore.FilePath) ? "已读取" : "未保存";
            WriteLog("信息", "就绪，请添加需要上传的 .nupkg 包。");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            _source.Text = "";
            _saveState.Text = "读取失败";
            _saveState.ForeColor = ErrorColor;
            ShowError("读取配置失败", ex);
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>
    /// 将当前界面配置明文保存至本地 <c>settings.json</c>，并向用户反馈保存状态。
    /// </summary>
    private void SaveSettings()
    {
        try
        {
            _settingsStore.Save(CaptureSettings());
            _saveState.Text = "已保存";
            _saveState.ForeColor = SuccessColor;
            WriteLog("完成", "配置已明文保存到程序目录的 settings.json。");
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            _saveState.Text = "保存失败";
            _saveState.ForeColor = ErrorColor;
            ShowError("保存配置失败", ex);
        }
    }

    /// <summary>
    /// 切换认证模式（API Key / 账号密码），同时刷新输入框布局并清除旧的服务器比对结果。
    /// </summary>
    private void SetAuthentication(AuthenticationMode mode)
    {
        if (_authentication == mode)
        {
            return;
        }

        _authentication = mode;
        UpdateAuthenticationLayout();
        MarkDirty();
        InvalidateRemoteComparisons();
    }

    /// <summary>
    /// 将配置状态标记为“未保存”（初始化加载配置期间忽略控件变更事件）。
    /// </summary>
    private void MarkDirty()
    {
        if (_loading)
        {
            return;
        }

        _saveState.Text = "未保存";
        _saveState.ForeColor = MutedColor;
    }

    /// <summary>
    /// 打开文件选择对话框，选择一个或多个待上传的普通 <c>.nupkg</c> 包。
    /// </summary>
    private async Task SelectFilesAsync()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "NuGet 包 (*.nupkg)|*.nupkg",
            Multiselect = true,
            Title = "选择需要上传的 NuGet 包",
            InitialDirectory = ExistingRecentDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await AddPathsAsync(dialog.FileNames);
        }
    }

    /// <summary>
    /// 打开文件夹选择对话框，导入所选文件夹顶层的所有 <c>.nupkg</c> 包。
    /// </summary>
    private async Task SelectFolderAsync()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择包所在文件夹（只读取顶层 .nupkg）",
            UseDescriptionForTitle = true,
            InitialDirectory = ExistingRecentDirectory()
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            await AddPathsAsync([dialog.SelectedPath]);
        }
    }

    /// <summary>
    /// 获取文件/文件夹对话框的初始目录：优先使用仍存在的最近目录，否则回退至程序所在目录。
    /// </summary>
    private string ExistingRecentDirectory() =>
        Directory.Exists(_recentDirectory) ? _recentDirectory : AppContext.BaseDirectory;

    /// <summary>
    /// 在后台线程解析并去重所选路径中的包文件，完成后回到 UI 线程更新列表与日志。
    /// </summary>
    /// <param name="paths">文件路径或文件夹路径集合。</param>
    private async Task AddPathsAsync(IEnumerable<string> paths)
    {
        if (IsBusy)
        {
            return;
        }

        _importing = true;
        SetBusy();
        var input = paths.ToArray();

        try
        {
            var result = await Task.Run(() => _catalog.Add(ExpandPaths(input)));
            foreach (var package in result.Added)
            {
                _packages.Add(package);
            }

            foreach (string error in result.Errors)
            {
                WriteLog("错误", error);
            }

            if (result.Added.Count > 0)
            {
                _recentDirectory = Path.GetDirectoryName(result.Added[^1].FilePath)!;
                MarkDirty();
                ResetProgress();
            }

            WriteLog("信息", $"添加 {result.Added.Count} 个包，忽略 {result.Duplicates} 个重复路径，失败 {result.Errors.Count} 个。");
            if (result.Errors.Count > 0)
            {
                MessageBox.Show(
                    this,
                    string.Join(Environment.NewLine, result.Errors.Take(8)),
                    "部分文件未能添加",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException)
        {
            ShowError("加载文件失败", ex);
        }
        finally
        {
            _importing = false;
            SetBusy();
        }
    }

    /// <summary>
    /// 将输入路径中的目录展开为按文件名排序的顶层 <c>*.nupkg</c> 文件列表，普通文件路径则原样保留。
    /// </summary>
    private static string[] ExpandPaths(IEnumerable<string> paths) =>
        paths.SelectMany(path => Directory.Exists(path)
                ? Directory.GetFiles(path, "*.nupkg", SearchOption.TopDirectoryOnly)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : [path])
            .ToArray();

    /// <summary>
    /// 为指定控件启用文件/文件夹拖放导入；当有后台任务运行时禁止拖放。
    /// </summary>
    private void EnableDrop(Control control)
    {
        control.AllowDrop = true;
        control.DragEnter += (_, e) =>
            e.Effect = !IsBusy && e.Data?.GetDataPresent(DataFormats.FileDrop) == true
                ? DragDropEffects.Copy
                : DragDropEffects.None;

        control.DragDrop += async (_, e) =>
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            {
                await AddPathsAsync(paths);
            }
        };
    }

    /// <summary>
    /// 根据当前列表勾选状态切换全选或取消全选（若存在未勾选项则全选，否则全部取消勾选）。
    /// </summary>
    private void SelectAll()
    {
        bool select = _packages.Any(item => !item.Selected);
        foreach (var item in _packages)
        {
            item.Selected = select;
        }
    }

    /// <summary>
    /// 从列表和路径去重目录中同时移除所有勾选项，使其后续可被再次导入。
    /// </summary>
    private void RemoveSelected()
    {
        foreach (var package in _packages.Where(p => p.Selected).ToArray())
        {
            _catalog.Remove(package);
            _packages.Remove(package);
        }

        ResetProgress();
    }

    /// <summary>
    /// 同步包总数、已勾选数量、表头全选图标以及底部操作按钮的可用状态。
    /// </summary>
    private void UpdateSelection()
    {
        int selected = _packages.Count(p => p.Selected);
        _packageCount.Text = $"待上传的包 · {_packages.Count} 个文件";
        _selection.Text = $"已选择 {selected} 个文件";
        _selectedColumn.HeaderText = selected > 0 && selected == _packages.Count ? "☑" : "☐";
        _start.Enabled = !IsBusy && selected > 0;
        _refresh.Enabled = !IsBusy && _packages.Count > 0;

        if (!_running && !_comparing && _progress.Value == 0)
        {
            _fraction.Text = $"0 / {selected}";
        }
    }

    /// <summary>
    /// 在包列表发生增删后重置进度条与汇总提示。
    /// </summary>
    private void ResetProgress()
    {
        _progress.Value = 0;
        _summary.Text = "准备就绪";
        _fraction.Text = $"0 / {_packages.Count(p => p.Selected)}";
    }

    /// <summary>
    /// 验证配置并启动勾选包的批量上传，上传结束后更新状态汇总并将成功上传的包标记为“待刷新”。
    /// </summary>
    private async Task StartUploadAsync()
    {
        _grid.EndEdit();
        var queue = _packages.Where(p => p.Selected).ToArray();
        if (_running || _comparing || queue.Length == 0)
        {
            return;
        }

        var settings = CaptureSettings();
        try
        {
            settings.Validate();
        }
        catch (ArgumentException ex)
        {
            ShowError("无法开始上传", ex);
            return;
        }

        _running = true;
        _closeAfterOperation = false;
        _progress.Value = 0;
        _progress.Maximum = queue.Length;
        _stop.Text = "停止后续上传";
        _status.Text = "● 正在上传";
        _status.ForeColor = AccentColor;
        SetBusy();

        var redactor = new SecretRedactor(settings);
        var log = new Progress<UploadLog>(entry => AppendLog(entry, redactor));
        string authText = settings.Authentication == AuthenticationMode.ApiKey ? "API Key" : "账号密码";
        WriteLog("信息", $"开始上传 {queue.Length} 个包，认证方式：{authText}。");

        try
        {
            var result = await _batch.RunAsync(queue, settings, log, new CallbackProgress<BatchProgress>(UpdateProgress));

            // 上传成功的包在服务器上的状态已改变，将其比对状态重置为“待刷新”
            foreach (var package in queue.Where(item => item.State == PackageState.Succeeded))
            {
                package.SetRemote(RemoteQueryState.NotQueried);
            }

            string prefix = result.Stopped && result.Remaining > 0 ? "已停止" : "上传完成";
            _summary.Text = $"{prefix} · 成功 {result.Succeeded}，失败 {result.Failed}，待处理 {result.Remaining}";
            _status.Text = result.Failed > 0
                ? "● 上传结束，存在失败项"
                : result.Remaining > 0
                    ? "● 已停止后续上传"
                    : "● 上传完成";
            _status.ForeColor = result.Failed > 0 ? ErrorColor : SuccessColor;
            WriteLog("信息", _summary.Text);
        }
        finally
        {
            _running = false;
            SetBusy();
            if (_closeAfterOperation)
            {
                Close();
            }
        }
    }

    /// <summary>
    /// 将批量上传进度实时同步至进度条、计数标签和列表视图。
    /// </summary>
    private void UpdateProgress(BatchProgress progress)
    {
        _progress.Value = progress.Completed;
        _fraction.Text = $"{progress.Completed} / {progress.Total}";
        if (progress.Current is { } current)
        {
            _summary.Text = $"正在上传 {current.Id} {current.Version}";
        }

        _grid.Invalidate();
    }

    /// <summary>
    /// 响应底部停止/取消按钮：若正在查询服务器则立即取消令牌；若正在上传则请求在当前包结束后停止。
    /// </summary>
    private void StopAfterCurrent()
    {
        if (_comparing)
        {
            _comparisonCancellation?.Cancel();
            _stop.Enabled = false;
            _stop.Text = "正在取消刷新";
            return;
        }

        _batch.RequestStop();
        _stop.Enabled = false;
        _stop.Text = "等待当前包完成";
        _summary.Text = "等待当前包完成后停止";
    }

    /// <summary>
    /// 根据当前任务运行状态统一启用或禁用配置输入、文件操作按钮和表格编辑。
    /// </summary>
    private void SetBusy()
    {
        bool enabled = !IsBusy;
        foreach (var control in new Control[] { _source, _apiKey, _username, _password, _keyMode, _accountMode, _save })
        {
            control.Enabled = enabled;
        }

        foreach (var button in new[] { _add, _folder, _remove, _clear, _refresh })
        {
            button.Enabled = enabled;
        }

        _grid.ReadOnly = !enabled;
        _stop.Enabled = _running || _comparing;
        UseWaitCursor = _importing;
        UpdateSelection();
    }

    /// <summary>
    /// 拦截窗口关闭事件，协调正在进行的导入、服务器查询或上传任务，防止中途强退导致状态不一致。
    /// </summary>
    private void HandleClosing(object? sender, FormClosingEventArgs e)
    {
        if (_importing)
        {
            e.Cancel = true;
            return;
        }

        if (_comparing)
        {
            e.Cancel = true;
            _closeAfterOperation = true;
            StopAfterCurrent();
            return;
        }

        if (!_running)
        {
            return;
        }

        e.Cancel = true;
        var choice = MessageBox.Show(
            this,
            "当前包仍在上传。是否在当前包结束后停止并关闭？",
            "上传进行中",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (choice != DialogResult.Yes)
        {
            return;
        }

        _closeAfterOperation = true;
        StopAfterCurrent();
    }

    /// <summary>
    /// 自定义单元格样式：高亮已勾选行背景色，并按语义着色“版本比对”和“上传状态”列文本。
    /// </summary>
    private void FormatCell(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || _grid.Rows[e.RowIndex].DataBoundItem is not PackageItem item)
        {
            return;
        }

        e.CellStyle!.BackColor = item.Selected ? SelectedRowBackColor : Color.White;
        string property = _grid.Columns[e.ColumnIndex].DataPropertyName;

        if (property == nameof(PackageItem.ComparisonText))
        {
            e.CellStyle.ForeColor = item.RemoteState == RemoteQueryState.Failed
                ? ErrorColor
                : item.ComparisonText switch
                {
                    "本地版本较新" => SuccessColor,
                    "本地版本较旧" or "同版本已存在" => WarningColor,
                    "服务器无此包" => AccentColor,
                    _ => MutedColor
                };
            return;
        }

        if (property == nameof(PackageItem.StatusText))
        {
            e.CellStyle.ForeColor = item.State switch
            {
                PackageState.Succeeded => SuccessColor,
                PackageState.Failed => ErrorColor,
                PackageState.Uploading => AccentColor,
                _ => MutedColor
            };
        }
    }

    /// <summary>
    /// 使用当前界面凭据脱敏后写入一条带时间戳的界面日志。
    /// </summary>
    private void WriteLog(string level, string message) =>
        AppendLog(new UploadLog(DateTime.Now, level, message), new SecretRedactor(CaptureSettings()));

    /// <summary>
    /// 向日志表格追加脱敏后的日志，自动限制最大保留条数与单条字符长度并滚动至最新行。
    /// </summary>
    private void AppendLog(UploadLog entry, SecretRedactor redactor)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (_logs.Rows.Count >= MaxLogEntries)
        {
            _logs.Rows.RemoveAt(0);
        }

        string message = redactor.Redact(entry.Message);
        if (message.Length > MaxLogMessageLength)
        {
            message = message[..MaxLogMessageLength] + "（内容过长，已截断）";
        }

        int index = _logs.Rows.Add(entry.Timestamp.ToString("HH:mm:ss"), entry.Level, message);
        _logs.Rows[index].DefaultCellStyle.ForeColor = entry.Level switch
        {
            "错误" => ErrorColor,
            "完成" => SuccessColor,
            _ => MutedColor
        };
        _logs.Rows[index].Selected = false;
        _logs.FirstDisplayedScrollingRowIndex = index;
    }

    /// <summary>
    /// 将异常信息脱敏后同时记录到日志面板并弹出错误对话框。
    /// </summary>
    private void ShowError(string title, Exception error)
    {
        var redactor = new SecretRedactor(CaptureSettings());
        string message = redactor.Redact(error.Message);
        WriteLog("错误", $"{title}：{message}");
        MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>
    /// 首次显示时按当前显示器工作区大小约束窗口尺寸并居中，防止在小分辨率或高缩放屏幕下超出可视范围。
    /// </summary>
    private void FitToWorkingArea()
    {
        var area = Screen.FromControl(this).WorkingArea;
        var available = new Size(Math.Max(1, area.Width - 24), Math.Max(1, area.Height - 24));
        MinimumSize = new Size(Math.Min(MinimumSize.Width, available.Width), Math.Min(MinimumSize.Height, available.Height));
        Size = new Size(Math.Min(Width, available.Width), Math.Min(Height, available.Height));
        Location = new Point(area.Left + (area.Width - Width) / 2, area.Top + (area.Height - Height) / 2);
        AdjustForWindowSize();
    }

    /// <summary>
    /// 窗口高度较小时自动收起拖放提示条并适度压缩日志区高度，优先保障包列表与操作按钮的可视空间。
    /// </summary>
    private void AdjustForWindowSize()
    {
        float scale = DeviceDpi / DesignDpi;
        bool compact = ClientSize.Height < CompactClientHeight * scale;

        // 紧凑模式下隐藏拖放提示横幅（包列表表格本身仍支持直接拖入文件）
        _body.RowStyles[2].Height = (compact ? 100 : 142) * scale;
        _packagePanel.RowStyles[1].Height = compact ? 0 : 44 * scale;
        _dropArea.Visible = !compact;
    }

    private void UpdateAuthenticationLayout()
    {
        bool account = _authentication == AuthenticationMode.Account;
        float scale = DeviceDpi / DesignDpi;
        _configuration.SuspendLayout();
        _apiKeyFields.Visible = !account;
        _accountFields.Visible = account;
        _configuration.RowStyles[2].Height = account ? 0 : _apiKeyRowHeight * scale;
        _configuration.RowStyles[3].Height = account ? _accountRowHeight * scale : 0;
        _body.RowStyles[0].Height = (_configurationHeight - (account ? _apiKeyRowHeight : _accountRowHeight)) * scale;
        _configuration.ResumeLayout(true);

        foreach (var (button, selected) in new[] { (_keyMode, !account), (_accountMode, account) })
        {
            button.BackColor = selected ? AccentColor : Color.White;
            button.ForeColor = selected ? Color.White : MutedColor;
            button.FlatAppearance.BorderColor = selected ? AccentColor : BorderColor;
        }
    }

    private static void EnableDoubleBuffering(DataGridView grid) =>
        typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(grid, true);

    /// <summary>
    /// 当仓库地址或认证信息发生修改后，立即清除旧的服务器版本比对结果，防止误用其他仓库的数据。
    /// </summary>
    private void InvalidateRemoteComparisons()
    {
        if (_loading)
        {
            return;
        }

        foreach (var item in _packages)
        {
            item.SetRemote(RemoteQueryState.NotQueried);
        }
    }

    /// <summary>
    /// 按包内部 ID 去重刷新服务器最新版本与发布时间，并同步管理进度条、取消令牌及界面按钮状态。
    /// </summary>
    private async Task RefreshServerAsync()
    {
        if (IsBusy || _packages.Count == 0)
        {
            return;
        }

        var settings = CaptureSettings();
        try
        {
            settings.Validate();
        }
        catch (ArgumentException error)
        {
            ShowError("无法刷新服务器信息", error);
            return;
        }

        _comparing = true;
        _closeAfterOperation = false;
        using var cancellation = new CancellationTokenSource();
        _comparisonCancellation = cancellation;

        _stop.Text = "取消刷新";
        _status.Text = "● 正在读取服务器信息";
        _status.ForeColor = AccentColor;

        int total = _packages
            .Select(item => item.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        _progress.Value = 0;
        _progress.Maximum = total;
        SetBusy();
        WriteLog("信息", $"开始查询 {total} 个类库的服务器版本及上传/发布时间；相同包 ID 只查询一次。");

        var redactor = new SecretRedactor(settings);
        var log = new Progress<UploadLog>(entry => AppendLog(entry, redactor));
        var progress = new CallbackProgress<BatchProgress>(value =>
        {
            _progress.Value = value.Completed;
            _fraction.Text = $"{value.Completed} / {value.Total}";
            if (value.Current is { } package)
            {
                _summary.Text = $"正在查询服务器：{package.Id}";
            }
        });

        try
        {
            var result = await _comparison.RefreshAsync(_packages.ToArray(), settings, log, progress, cancellation.Token);
            string prefix = result.Canceled ? "刷新已取消" : "刷新完成";
            _summary.Text = $"{prefix} · 已读取 {result.Loaded} 个包，失败 {result.Failed} 个";
            _status.Text = result.Failed > 0
                ? "● 服务器信息部分查询失败，请查看日志"
                : result.Canceled
                    ? "● 已取消刷新"
                    : "● 服务器信息已更新";
            _status.ForeColor = result.Failed > 0 ? ErrorColor : SuccessColor;
            WriteLog("信息", _summary.Text);
        }
        finally
        {
            _comparisonCancellation = null;
            _comparing = false;
            _stop.Text = "停止后续上传";
            SetBusy();
            if (_closeAfterOperation)
            {
                Close();
            }
        }
    }

    private void OpenProjectHomepage()
    {
        try
        {
            Process.Start(new ProcessStartInfo(RepositoryUrl) { UseShellExecute = true });
        }
        catch (Exception error) when (error is Win32Exception or InvalidOperationException)
        {
            ShowError("无法打开项目主页", error);
        }
    }
}

/// <summary>
/// 在调用方当前线程直接同步执行回调的 <see cref="IProgress{T}"/> 实现，避免异步投递导致界面状态乱序。
/// </summary>
internal sealed class CallbackProgress<T>(Action<T> callback) : IProgress<T>
{
    /// <inheritdoc />
    public void Report(T value) => callback(value);
}
