namespace NugetUploadHelper;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle3 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle2 = new DataGridViewCellStyle();
        DataGridViewCellStyle dataGridViewCellStyle4 = new DataGridViewCellStyle();
        _body = new TableLayoutPanel();
        _configuration = new TableLayoutPanel();
        _configurationHeading = new TableLayoutPanel();
        _configurationTitle = new Label();
        _authenticationButtons = new FlowLayoutPanel();
        _keyMode = new Button();
        _accountMode = new Button();
        _sourceFields = new TableLayoutPanel();
        _sourceFieldsLabel = new Label();
        _source = new TextBox();
        _apiKeyFields = new TableLayoutPanel();
        _apiKeyFieldsLabel = new Label();
        _apiKey = new TextBox();
        _accountFields = new TableLayoutPanel();
        _usernameFields = new TableLayoutPanel();
        _usernameFieldsLabel = new Label();
        _username = new TextBox();
        _passwordFields = new TableLayoutPanel();
        _passwordFieldsLabel = new Label();
        _password = new TextBox();
        _configurationFooter = new TableLayoutPanel();
        _configurationPath = new Label();
        _saveState = new Label();
        _save = new Button();
        _packagePanel = new TableLayoutPanel();
        _packageHeading = new TableLayoutPanel();
        _packageCount = new Label();
        _packageButtons = new FlowLayoutPanel();
        _add = new Button();
        _folder = new Button();
        _refresh = new Button();
        _remove = new Button();
        _clear = new Button();
        _dropArea = new Label();
        _grid = new DataGridView();
        _selectedColumn = new DataGridViewCheckBoxColumn();
        _fileNameColumn = new DataGridViewTextBoxColumn();
        _versionColumn = new DataGridViewTextBoxColumn();
        _sizeColumn = new DataGridViewTextBoxColumn();
        _remoteVersionColumn = new DataGridViewTextBoxColumn();
        _publishedColumn = new DataGridViewTextBoxColumn();
        _comparisonColumn = new DataGridViewTextBoxColumn();
        _uploadStateColumn = new DataGridViewTextBoxColumn();
        _selection = new Label();
        _logPanel = new TableLayoutPanel();
        _logTitle = new Label();
        _logs = new DataGridView();
        _timeColumn = new DataGridViewTextBoxColumn();
        _levelColumn = new DataGridViewTextBoxColumn();
        _messageColumn = new DataGridViewTextBoxColumn();
        _footer = new TableLayoutPanel();
        _progressPanel = new TableLayoutPanel();
        _summary = new Label();
        _fraction = new Label();
        _progress = new ProgressBar();
        _stop = new Button();
        _start = new Button();
        _statusStrip = new StatusStrip();
        _status = new ToolStripStatusLabel();
        _timeoutStatus = new ToolStripStatusLabel();
        _projectLink = new ToolStripStatusLabel();
        _toolTip = new ToolTip(components);
        _body.SuspendLayout();
        _configuration.SuspendLayout();
        _configurationHeading.SuspendLayout();
        _authenticationButtons.SuspendLayout();
        _sourceFields.SuspendLayout();
        _apiKeyFields.SuspendLayout();
        _accountFields.SuspendLayout();
        _usernameFields.SuspendLayout();
        _passwordFields.SuspendLayout();
        _configurationFooter.SuspendLayout();
        _packagePanel.SuspendLayout();
        _packageHeading.SuspendLayout();
        _packageButtons.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).BeginInit();
        _logPanel.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)_logs).BeginInit();
        _footer.SuspendLayout();
        _progressPanel.SuspendLayout();
        _statusStrip.SuspendLayout();
        SuspendLayout();
        //
        // _body
        //
        _body.ColumnCount = 1;
        _body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _body.Controls.Add(_configuration, 0, 0);
        _body.Controls.Add(_packagePanel, 0, 1);
        _body.Controls.Add(_logPanel, 0, 2);
        _body.Controls.Add(_footer, 0, 3);
        _body.Dock = DockStyle.Fill;
        _body.Location = new Point(0, 0);
        _body.Margin = new Padding(4, 4, 4, 4);
        _body.Name = "_body";
        _body.Padding = new Padding(33, 27, 33, 18);
        _body.RowCount = 4;
        _body.RowStyles.Add(new RowStyle(SizeType.Absolute, 405F));
        _body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _body.RowStyles.Add(new RowStyle(SizeType.Absolute, 213F));
        _body.RowStyles.Add(new RowStyle(SizeType.Absolute, 117F));
        _body.Size = new Size(1680, 1244);
        _body.TabIndex = 0;
        //
        // _configuration
        //
        _configuration.BackColor = Color.White;
        _configuration.BorderStyle = BorderStyle.FixedSingle;
        _configuration.ColumnCount = 1;
        _configuration.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configuration.Controls.Add(_configurationHeading, 0, 0);
        _configuration.Controls.Add(_sourceFields, 0, 1);
        _configuration.Controls.Add(_apiKeyFields, 0, 2);
        _configuration.Controls.Add(_accountFields, 0, 3);
        _configuration.Controls.Add(_configurationFooter, 0, 4);
        _configuration.Dock = DockStyle.Fill;
        _configuration.Location = new Point(33, 27);
        _configuration.Margin = new Padding(0, 0, 0, 24);
        _configuration.Name = "_configuration";
        _configuration.Padding = new Padding(24, 15, 24, 15);
        _configuration.RowCount = 5;
        _configuration.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        _configuration.RowStyles.Add(new RowStyle(SizeType.Absolute, 63F));
        _configuration.RowStyles.Add(new RowStyle(SizeType.Absolute, 63F));
        _configuration.RowStyles.Add(new RowStyle(SizeType.Absolute, 63F));
        _configuration.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        _configuration.Size = new Size(1614, 381);
        _configuration.TabIndex = 0;
        //
        // _configurationHeading
        //
        _configurationHeading.ColumnCount = 2;
        _configurationHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configurationHeading.ColumnStyles.Add(new ColumnStyle());
        _configurationHeading.Controls.Add(_configurationTitle, 0, 0);
        _configurationHeading.Controls.Add(_authenticationButtons, 1, 0);
        _configurationHeading.Dock = DockStyle.Fill;
        _configurationHeading.Location = new Point(24, 15);
        _configurationHeading.Margin = new Padding(0);
        _configurationHeading.Name = "_configurationHeading";
        _configurationHeading.RowCount = 1;
        _configurationHeading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _configurationHeading.Size = new Size(1564, 72);
        _configurationHeading.TabIndex = 0;
        //
        // _configurationTitle
        //
        _configurationTitle.Anchor = AnchorStyles.Left;
        _configurationTitle.AutoSize = true;
        _configurationTitle.ForeColor = Color.FromArgb(32, 40, 48);
        _configurationTitle.Location = new Point(0, 24);
        _configurationTitle.Margin = new Padding(0);
        _configurationTitle.Name = "_configurationTitle";
        _configurationTitle.Size = new Size(100, 24);
        _configurationTitle.TabIndex = 0;
        _configurationTitle.Text = "仓库与认证";
        _configurationTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _authenticationButtons
        //
        _authenticationButtons.Anchor = AnchorStyles.Right;
        _authenticationButtons.AutoSize = true;
        _authenticationButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _authenticationButtons.Controls.Add(_keyMode);
        _authenticationButtons.Controls.Add(_accountMode);
        _authenticationButtons.Location = new Point(1213, 4);
        _authenticationButtons.Margin = new Padding(0);
        _authenticationButtons.Name = "_authenticationButtons";
        _authenticationButtons.Size = new Size(351, 63);
        _authenticationButtons.TabIndex = 1;
        _authenticationButtons.WrapContents = false;
        //
        // _keyMode
        //
        _keyMode.AccessibleName = "API Key";
        _keyMode.AutoSize = true;
        _keyMode.BackColor = Color.FromArgb(18, 100, 189);
        _keyMode.Cursor = Cursors.Hand;
        _keyMode.FlatAppearance.BorderColor = Color.FromArgb(18, 100, 189);
        _keyMode.FlatStyle = FlatStyle.Flat;
        _keyMode.ForeColor = Color.White;
        _keyMode.Location = new Point(0, 0);
        _keyMode.Margin = new Padding(0, 0, 10, 0);
        _keyMode.MinimumSize = new Size(90, 50);
        _keyMode.Name = "_keyMode";
        _keyMode.Padding = new Padding(15, 4, 15, 4);
        _keyMode.Size = new Size(160, 63);
        _keyMode.TabIndex = 0;
        _keyMode.Text = "API Key";
        _keyMode.UseVisualStyleBackColor = false;
        //
        // _accountMode
        //
        _accountMode.AccessibleName = "账号密码";
        _accountMode.AutoSize = true;
        _accountMode.BackColor = Color.White;
        _accountMode.Cursor = Cursors.Hand;
        _accountMode.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _accountMode.FlatStyle = FlatStyle.Flat;
        _accountMode.ForeColor = Color.FromArgb(32, 40, 48);
        _accountMode.Location = new Point(170, 0);
        _accountMode.Margin = new Padding(0, 0, 10, 0);
        _accountMode.MinimumSize = new Size(90, 50);
        _accountMode.Name = "_accountMode";
        _accountMode.Padding = new Padding(15, 4, 15, 4);
        _accountMode.Size = new Size(171, 63);
        _accountMode.TabIndex = 1;
        _accountMode.Text = "账号密码";
        _accountMode.UseVisualStyleBackColor = false;
        //
        // _sourceFields
        //
        _sourceFields.ColumnCount = 2;
        _sourceFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _sourceFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _sourceFields.Controls.Add(_sourceFieldsLabel, 0, 0);
        _sourceFields.Controls.Add(_source, 1, 0);
        _sourceFields.Dock = DockStyle.Fill;
        _sourceFields.Location = new Point(24, 87);
        _sourceFields.Margin = new Padding(0);
        _sourceFields.Name = "_sourceFields";
        _sourceFields.RowCount = 1;
        _sourceFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _sourceFields.Size = new Size(1564, 63);
        _sourceFields.TabIndex = 1;
        //
        // _sourceFieldsLabel
        //
        _sourceFieldsLabel.AutoEllipsis = true;
        _sourceFieldsLabel.Dock = DockStyle.Fill;
        _sourceFieldsLabel.ForeColor = Color.FromArgb(100, 111, 124);
        _sourceFieldsLabel.Location = new Point(0, 0);
        _sourceFieldsLabel.Margin = new Padding(0);
        _sourceFieldsLabel.Name = "_sourceFieldsLabel";
        _sourceFieldsLabel.Size = new Size(112, 63);
        _sourceFieldsLabel.TabIndex = 0;
        _sourceFieldsLabel.Text = "仓库地址";
        _sourceFieldsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _source
        //
        _source.AccessibleName = "仓库地址";
        _source.BorderStyle = BorderStyle.FixedSingle;
        _source.Dock = DockStyle.Fill;
        _source.Location = new Point(112, 10);
        _source.Margin = new Padding(0, 10, 0, 8);
        _source.MinimumSize = new Size(44, 29);
        _source.Name = "_source";
        _source.Size = new Size(1452, 30);
        _source.TabIndex = 1;
        //
        // _apiKeyFields
        //
        _apiKeyFields.ColumnCount = 2;
        _apiKeyFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _apiKeyFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _apiKeyFields.Controls.Add(_apiKeyFieldsLabel, 0, 0);
        _apiKeyFields.Controls.Add(_apiKey, 1, 0);
        _apiKeyFields.Dock = DockStyle.Fill;
        _apiKeyFields.Location = new Point(24, 150);
        _apiKeyFields.Margin = new Padding(0);
        _apiKeyFields.Name = "_apiKeyFields";
        _apiKeyFields.RowCount = 1;
        _apiKeyFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _apiKeyFields.Size = new Size(1564, 63);
        _apiKeyFields.TabIndex = 2;
        //
        // _apiKeyFieldsLabel
        //
        _apiKeyFieldsLabel.AutoEllipsis = true;
        _apiKeyFieldsLabel.Dock = DockStyle.Fill;
        _apiKeyFieldsLabel.ForeColor = Color.FromArgb(100, 111, 124);
        _apiKeyFieldsLabel.Location = new Point(0, 0);
        _apiKeyFieldsLabel.Margin = new Padding(0);
        _apiKeyFieldsLabel.Name = "_apiKeyFieldsLabel";
        _apiKeyFieldsLabel.Size = new Size(112, 63);
        _apiKeyFieldsLabel.TabIndex = 0;
        _apiKeyFieldsLabel.Text = "API Key";
        _apiKeyFieldsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _apiKey
        //
        _apiKey.AccessibleName = "API Key";
        _apiKey.BorderStyle = BorderStyle.FixedSingle;
        _apiKey.Dock = DockStyle.Fill;
        _apiKey.Location = new Point(112, 10);
        _apiKey.Margin = new Padding(0, 10, 0, 8);
        _apiKey.MinimumSize = new Size(44, 29);
        _apiKey.Name = "_apiKey";
        _apiKey.Size = new Size(1452, 30);
        _apiKey.TabIndex = 1;
        //
        // _accountFields
        //
        _accountFields.ColumnCount = 2;
        _accountFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _accountFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        _accountFields.Controls.Add(_usernameFields, 0, 0);
        _accountFields.Controls.Add(_passwordFields, 1, 0);
        _accountFields.Dock = DockStyle.Fill;
        _accountFields.Location = new Point(24, 213);
        _accountFields.Margin = new Padding(0);
        _accountFields.Name = "_accountFields";
        _accountFields.RowCount = 1;
        _accountFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _accountFields.Size = new Size(1564, 63);
        _accountFields.TabIndex = 3;
        //
        // _usernameFields
        //
        _usernameFields.ColumnCount = 2;
        _usernameFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _usernameFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _usernameFields.Controls.Add(_usernameFieldsLabel, 0, 0);
        _usernameFields.Controls.Add(_username, 1, 0);
        _usernameFields.Dock = DockStyle.Fill;
        _usernameFields.Location = new Point(0, 0);
        _usernameFields.Margin = new Padding(0);
        _usernameFields.Name = "_usernameFields";
        _usernameFields.RowCount = 1;
        _usernameFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _usernameFields.Size = new Size(782, 63);
        _usernameFields.TabIndex = 0;
        //
        // _usernameFieldsLabel
        //
        _usernameFieldsLabel.AutoEllipsis = true;
        _usernameFieldsLabel.Dock = DockStyle.Fill;
        _usernameFieldsLabel.ForeColor = Color.FromArgb(100, 111, 124);
        _usernameFieldsLabel.Location = new Point(0, 0);
        _usernameFieldsLabel.Margin = new Padding(0);
        _usernameFieldsLabel.Name = "_usernameFieldsLabel";
        _usernameFieldsLabel.Size = new Size(112, 63);
        _usernameFieldsLabel.TabIndex = 0;
        _usernameFieldsLabel.Text = "用户名";
        _usernameFieldsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _username
        //
        _username.AccessibleName = "用户名";
        _username.BorderStyle = BorderStyle.FixedSingle;
        _username.Dock = DockStyle.Fill;
        _username.Location = new Point(112, 10);
        _username.Margin = new Padding(0, 10, 0, 8);
        _username.MinimumSize = new Size(44, 29);
        _username.Name = "_username";
        _username.Size = new Size(670, 30);
        _username.TabIndex = 1;
        //
        // _passwordFields
        //
        _passwordFields.ColumnCount = 2;
        _passwordFields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112F));
        _passwordFields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _passwordFields.Controls.Add(_passwordFieldsLabel, 0, 0);
        _passwordFields.Controls.Add(_password, 1, 0);
        _passwordFields.Dock = DockStyle.Fill;
        _passwordFields.Location = new Point(812, 0);
        _passwordFields.Margin = new Padding(30, 0, 0, 0);
        _passwordFields.Name = "_passwordFields";
        _passwordFields.RowCount = 1;
        _passwordFields.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _passwordFields.Size = new Size(752, 63);
        _passwordFields.TabIndex = 1;
        //
        // _passwordFieldsLabel
        //
        _passwordFieldsLabel.AutoEllipsis = true;
        _passwordFieldsLabel.Dock = DockStyle.Fill;
        _passwordFieldsLabel.ForeColor = Color.FromArgb(100, 111, 124);
        _passwordFieldsLabel.Location = new Point(0, 0);
        _passwordFieldsLabel.Margin = new Padding(0);
        _passwordFieldsLabel.Name = "_passwordFieldsLabel";
        _passwordFieldsLabel.Size = new Size(112, 63);
        _passwordFieldsLabel.TabIndex = 0;
        _passwordFieldsLabel.Text = "密码";
        _passwordFieldsLabel.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _password
        //
        _password.AccessibleName = "明文密码";
        _password.BorderStyle = BorderStyle.FixedSingle;
        _password.Dock = DockStyle.Fill;
        _password.Location = new Point(112, 10);
        _password.Margin = new Padding(0, 10, 0, 8);
        _password.MinimumSize = new Size(44, 29);
        _password.Name = "_password";
        _password.Size = new Size(640, 30);
        _password.TabIndex = 1;
        _toolTip.SetToolTip(_password, "密码明文显示，并随配置明文保存到本地。");
        //
        // _configurationFooter
        //
        _configurationFooter.ColumnCount = 3;
        _configurationFooter.ColumnStyles.Add(new ColumnStyle());
        _configurationFooter.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _configurationFooter.ColumnStyles.Add(new ColumnStyle());
        _configurationFooter.Controls.Add(_configurationPath, 0, 0);
        _configurationFooter.Controls.Add(_saveState, 1, 0);
        _configurationFooter.Controls.Add(_save, 2, 0);
        _configurationFooter.Dock = DockStyle.Fill;
        _configurationFooter.Location = new Point(24, 276);
        _configurationFooter.Margin = new Padding(0);
        _configurationFooter.Name = "_configurationFooter";
        _configurationFooter.RowCount = 1;
        _configurationFooter.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _configurationFooter.Size = new Size(1564, 88);
        _configurationFooter.TabIndex = 4;
        //
        // _configurationPath
        //
        _configurationPath.Anchor = AnchorStyles.Left;
        _configurationPath.AutoSize = true;
        _configurationPath.ForeColor = Color.FromArgb(100, 111, 124);
        _configurationPath.Location = new Point(0, 32);
        _configurationPath.Margin = new Padding(0);
        _configurationPath.Name = "_configurationPath";
        _configurationPath.Size = new Size(207, 24);
        _configurationPath.TabIndex = 0;
        _configurationPath.Text = "本地配置：settings.json";
        _configurationPath.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _saveState
        //
        _saveState.Anchor = AnchorStyles.Left;
        _saveState.AutoSize = true;
        _saveState.ForeColor = Color.FromArgb(100, 111, 124);
        _saveState.Location = new Point(231, 32);
        _saveState.Margin = new Padding(24, 0, 0, 0);
        _saveState.Name = "_saveState";
        _saveState.Size = new Size(64, 24);
        _saveState.TabIndex = 1;
        _saveState.Text = "未保存";
        _saveState.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _save
        //
        _save.AccessibleName = "保存配置";
        _save.AutoSize = true;
        _save.BackColor = Color.White;
        _save.Cursor = Cursors.Hand;
        _save.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _save.FlatStyle = FlatStyle.Flat;
        _save.ForeColor = Color.FromArgb(32, 40, 48);
        _save.Location = new Point(1383, 0);
        _save.Margin = new Padding(0, 0, 10, 0);
        _save.MinimumSize = new Size(90, 50);
        _save.Name = "_save";
        _save.Padding = new Padding(15, 4, 15, 4);
        _save.Size = new Size(171, 63);
        _save.TabIndex = 2;
        _save.Text = "保存配置";
        _toolTip.SetToolTip(_save, "保存到程序目录的 settings.json，下次启动自动读取。");
        _save.UseVisualStyleBackColor = false;
        //
        // _packagePanel
        //
        _packagePanel.ColumnCount = 1;
        _packagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _packagePanel.Controls.Add(_packageHeading, 0, 0);
        _packagePanel.Controls.Add(_dropArea, 0, 1);
        _packagePanel.Controls.Add(_grid, 0, 2);
        _packagePanel.Controls.Add(_selection, 0, 3);
        _packagePanel.Dock = DockStyle.Fill;
        _packagePanel.Location = new Point(33, 432);
        _packagePanel.Margin = new Padding(0);
        _packagePanel.Name = "_packagePanel";
        _packagePanel.RowCount = 4;
        _packagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 64F));
        _packagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 66F));
        _packagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _packagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        _packagePanel.Size = new Size(1614, 464);
        _packagePanel.TabIndex = 1;
        //
        // _packageHeading
        //
        _packageHeading.ColumnCount = 2;
        _packageHeading.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _packageHeading.ColumnStyles.Add(new ColumnStyle());
        _packageHeading.Controls.Add(_packageCount, 0, 0);
        _packageHeading.Controls.Add(_packageButtons, 1, 0);
        _packageHeading.Dock = DockStyle.Fill;
        _packageHeading.Location = new Point(0, 0);
        _packageHeading.Margin = new Padding(0);
        _packageHeading.Name = "_packageHeading";
        _packageHeading.RowCount = 1;
        _packageHeading.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _packageHeading.Size = new Size(1614, 64);
        _packageHeading.TabIndex = 0;
        //
        // _packageCount
        //
        _packageCount.Anchor = AnchorStyles.Left;
        _packageCount.AutoSize = true;
        _packageCount.ForeColor = Color.FromArgb(32, 40, 48);
        _packageCount.Location = new Point(0, 20);
        _packageCount.Margin = new Padding(0);
        _packageCount.Name = "_packageCount";
        _packageCount.Size = new Size(184, 24);
        _packageCount.TabIndex = 0;
        _packageCount.Text = "待上传的包 · 0 个文件";
        _packageCount.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _packageButtons
        //
        _packageButtons.Anchor = AnchorStyles.Right;
        _packageButtons.AutoSize = true;
        _packageButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        _packageButtons.Controls.Add(_add);
        _packageButtons.Controls.Add(_folder);
        _packageButtons.Controls.Add(_refresh);
        _packageButtons.Controls.Add(_remove);
        _packageButtons.Controls.Add(_clear);
        _packageButtons.Location = new Point(674, 0);
        _packageButtons.Margin = new Padding(0);
        _packageButtons.Name = "_packageButtons";
        _packageButtons.Size = new Size(940, 63);
        _packageButtons.TabIndex = 1;
        _packageButtons.WrapContents = false;
        //
        // _add
        //
        _add.AccessibleName = "＋ 添加文件";
        _add.AutoSize = true;
        _add.BackColor = Color.White;
        _add.Cursor = Cursors.Hand;
        _add.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _add.FlatStyle = FlatStyle.Flat;
        _add.ForeColor = Color.FromArgb(32, 40, 48);
        _add.Location = new Point(0, 0);
        _add.Margin = new Padding(0, 0, 10, 0);
        _add.MinimumSize = new Size(90, 50);
        _add.Name = "_add";
        _add.Padding = new Padding(15, 4, 15, 4);
        _add.Size = new Size(206, 63);
        _add.TabIndex = 0;
        _add.Text = "＋ 添加文件";
        _add.UseVisualStyleBackColor = false;
        //
        // _folder
        //
        _folder.AccessibleName = "选择文件夹";
        _folder.AutoSize = true;
        _folder.BackColor = Color.White;
        _folder.Cursor = Cursors.Hand;
        _folder.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _folder.FlatStyle = FlatStyle.Flat;
        _folder.ForeColor = Color.FromArgb(32, 40, 48);
        _folder.Location = new Point(216, 0);
        _folder.Margin = new Padding(0, 0, 10, 0);
        _folder.MinimumSize = new Size(90, 50);
        _folder.Name = "_folder";
        _folder.Padding = new Padding(15, 4, 15, 4);
        _folder.Size = new Size(198, 63);
        _folder.TabIndex = 1;
        _folder.Text = "选择文件夹";
        _folder.UseVisualStyleBackColor = false;
        //
        // _refresh
        //
        _refresh.AccessibleName = "刷新服务器信息";
        _refresh.AutoSize = true;
        _refresh.BackColor = Color.White;
        _refresh.Cursor = Cursors.Hand;
        _refresh.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _refresh.FlatStyle = FlatStyle.Flat;
        _refresh.ForeColor = Color.FromArgb(32, 40, 48);
        _refresh.Location = new Point(424, 0);
        _refresh.Margin = new Padding(0, 0, 10, 0);
        _refresh.MinimumSize = new Size(90, 50);
        _refresh.Name = "_refresh";
        _refresh.Padding = new Padding(15, 4, 15, 4);
        _refresh.Size = new Size(252, 63);
        _refresh.TabIndex = 2;
        _refresh.Text = "刷新服务器信息";
        _refresh.UseVisualStyleBackColor = false;
        //
        // _remove
        //
        _remove.AccessibleName = "移除";
        _remove.AutoSize = true;
        _remove.BackColor = Color.White;
        _remove.Cursor = Cursors.Hand;
        _remove.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _remove.FlatStyle = FlatStyle.Flat;
        _remove.ForeColor = Color.FromArgb(32, 40, 48);
        _remove.Location = new Point(686, 0);
        _remove.Margin = new Padding(0, 0, 10, 0);
        _remove.MinimumSize = new Size(90, 50);
        _remove.Name = "_remove";
        _remove.Padding = new Padding(15, 4, 15, 4);
        _remove.Size = new Size(117, 63);
        _remove.TabIndex = 3;
        _remove.Text = "移除";
        _remove.UseVisualStyleBackColor = false;
        //
        // _clear
        //
        _clear.AccessibleName = "清空";
        _clear.AutoSize = true;
        _clear.BackColor = Color.White;
        _clear.Cursor = Cursors.Hand;
        _clear.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _clear.FlatStyle = FlatStyle.Flat;
        _clear.ForeColor = Color.FromArgb(32, 40, 48);
        _clear.Location = new Point(813, 0);
        _clear.Margin = new Padding(0, 0, 10, 0);
        _clear.MinimumSize = new Size(90, 50);
        _clear.Name = "_clear";
        _clear.Padding = new Padding(15, 4, 15, 4);
        _clear.Size = new Size(117, 63);
        _clear.TabIndex = 4;
        _clear.Text = "清空";
        _clear.UseVisualStyleBackColor = false;
        //
        // _dropArea
        //
        _dropArea.AutoSize = true;
        _dropArea.BackColor = Color.White;
        _dropArea.BorderStyle = BorderStyle.FixedSingle;
        _dropArea.Dock = DockStyle.Fill;
        _dropArea.ForeColor = Color.FromArgb(100, 111, 124);
        _dropArea.Location = new Point(0, 64);
        _dropArea.Margin = new Padding(0, 0, 0, 14);
        _dropArea.Name = "_dropArea";
        _dropArea.Size = new Size(1614, 52);
        _dropArea.TabIndex = 1;
        _dropArea.Text = "↑  将 .nupkg 文件或文件夹拖到这里，或使用上方按钮添加";
        _dropArea.TextAlign = ContentAlignment.MiddleCenter;
        //
        // _grid
        //
        _grid.AccessibleName = "包列表";
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _grid.BackgroundColor = Color.White;
        _grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        dataGridViewCellStyle1.BackColor = Color.FromArgb(244, 246, 248);
        dataGridViewCellStyle1.ForeColor = Color.FromArgb(100, 111, 124);
        dataGridViewCellStyle1.Padding = new Padding(5);
        _grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
        _grid.ColumnHeadersHeight = 35;
        _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        _grid.Columns.AddRange(new DataGridViewColumn[] { _selectedColumn, _fileNameColumn, _versionColumn, _sizeColumn, _remoteVersionColumn, _publishedColumn, _comparisonColumn, _uploadStateColumn });
        dataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle3.BackColor = Color.White;
        dataGridViewCellStyle3.Font = new Font("Microsoft YaHei UI", 9F);
        dataGridViewCellStyle3.ForeColor = Color.FromArgb(32, 40, 48);
        dataGridViewCellStyle3.Padding = new Padding(5);
        dataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(224, 237, 254);
        dataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(32, 40, 48);
        dataGridViewCellStyle3.WrapMode = DataGridViewTriState.False;
        _grid.DefaultCellStyle = dataGridViewCellStyle3;
        _grid.Dock = DockStyle.Fill;
        _grid.EnableHeadersVisualStyles = false;
        _grid.GridColor = Color.FromArgb(220, 225, 231);
        _grid.Location = new Point(0, 130);
        _grid.Margin = new Padding(0);
        _grid.Name = "_grid";
        _grid.RowHeadersVisible = false;
        _grid.RowHeadersWidth = 62;
        _grid.RowTemplate.Height = 38;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.Size = new Size(1614, 289);
        _grid.TabIndex = 2;
        //
        // _selectedColumn
        //
        _selectedColumn.DataPropertyName = "Selected";
        _selectedColumn.HeaderText = "☐";
        _selectedColumn.MinimumWidth = 8;
        _selectedColumn.Name = "_selectedColumn";
        _selectedColumn.Resizable = DataGridViewTriState.False;
        _selectedColumn.Width = 45;
        //
        // _fileNameColumn
        //
        _fileNameColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _fileNameColumn.DataPropertyName = "FileName";
        dataGridViewCellStyle2.WrapMode = DataGridViewTriState.True;
        _fileNameColumn.DefaultCellStyle = dataGridViewCellStyle2;
        _fileNameColumn.FillWeight = 34F;
        _fileNameColumn.HeaderText = "完整文件名";
        _fileNameColumn.MinimumWidth = 280;
        _fileNameColumn.Name = "_fileNameColumn";
        _fileNameColumn.ReadOnly = true;
        _fileNameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _versionColumn
        //
        _versionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _versionColumn.DataPropertyName = "Version";
        _versionColumn.FillWeight = 9F;
        _versionColumn.HeaderText = "本地版本";
        _versionColumn.MinimumWidth = 80;
        _versionColumn.Name = "_versionColumn";
        _versionColumn.ReadOnly = true;
        _versionColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _sizeColumn
        //
        _sizeColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _sizeColumn.DataPropertyName = "SizeText";
        _sizeColumn.FillWeight = 8F;
        _sizeColumn.HeaderText = "大小";
        _sizeColumn.MinimumWidth = 65;
        _sizeColumn.Name = "_sizeColumn";
        _sizeColumn.ReadOnly = true;
        _sizeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _remoteVersionColumn
        //
        _remoteVersionColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _remoteVersionColumn.DataPropertyName = "RemoteVersionText";
        _remoteVersionColumn.FillWeight = 12F;
        _remoteVersionColumn.HeaderText = "服务器最新版本";
        _remoteVersionColumn.MinimumWidth = 120;
        _remoteVersionColumn.Name = "_remoteVersionColumn";
        _remoteVersionColumn.ReadOnly = true;
        _remoteVersionColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _publishedColumn
        //
        _publishedColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _publishedColumn.DataPropertyName = "RemotePublishedText";
        _publishedColumn.DefaultCellStyle = dataGridViewCellStyle2;
        _publishedColumn.FillWeight = 18F;
        _publishedColumn.HeaderText = "上传/发布时间";
        _publishedColumn.MinimumWidth = 170;
        _publishedColumn.Name = "_publishedColumn";
        _publishedColumn.ReadOnly = true;
        _publishedColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _publishedColumn.ToolTipText = "服务器最新版本的上传/发布时间（Published），按本机时区显示；服务器未提供时不推测。";
        //
        // _comparisonColumn
        //
        _comparisonColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _comparisonColumn.DataPropertyName = "ComparisonText";
        _comparisonColumn.FillWeight = 12F;
        _comparisonColumn.HeaderText = "版本比对";
        _comparisonColumn.MinimumWidth = 120;
        _comparisonColumn.Name = "_comparisonColumn";
        _comparisonColumn.ReadOnly = true;
        _comparisonColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _uploadStateColumn
        //
        _uploadStateColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _uploadStateColumn.DataPropertyName = "StatusText";
        _uploadStateColumn.FillWeight = 10F;
        _uploadStateColumn.HeaderText = "上传状态";
        _uploadStateColumn.MinimumWidth = 90;
        _uploadStateColumn.Name = "_uploadStateColumn";
        _uploadStateColumn.ReadOnly = true;
        _uploadStateColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _selection
        //
        _selection.Anchor = AnchorStyles.Left;
        _selection.AutoSize = true;
        _selection.ForeColor = Color.FromArgb(100, 111, 124);
        _selection.Location = new Point(0, 429);
        _selection.Margin = new Padding(0);
        _selection.Name = "_selection";
        _selection.Size = new Size(139, 24);
        _selection.TabIndex = 3;
        _selection.Text = "已选择 0 个文件";
        _selection.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _logPanel
        //
        _logPanel.ColumnCount = 1;
        _logPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _logPanel.Controls.Add(_logTitle, 0, 0);
        _logPanel.Controls.Add(_logs, 0, 1);
        _logPanel.Dock = DockStyle.Fill;
        _logPanel.Location = new Point(33, 904);
        _logPanel.Margin = new Padding(0, 8, 0, 0);
        _logPanel.Name = "_logPanel";
        _logPanel.RowCount = 2;
        _logPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        _logPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _logPanel.Size = new Size(1614, 205);
        _logPanel.TabIndex = 2;
        //
        // _logTitle
        //
        _logTitle.Anchor = AnchorStyles.Left;
        _logTitle.AutoSize = true;
        _logTitle.ForeColor = Color.FromArgb(32, 40, 48);
        _logTitle.Location = new Point(0, 10);
        _logTitle.Margin = new Padding(0);
        _logTitle.Name = "_logTitle";
        _logTitle.Size = new Size(82, 24);
        _logTitle.TabIndex = 0;
        _logTitle.Text = "上传日志";
        _logTitle.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _logs
        //
        _logs.AccessibleName = "上传日志";
        _logs.AllowUserToAddRows = false;
        _logs.AllowUserToDeleteRows = false;
        _logs.AllowUserToResizeRows = false;
        _logs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        _logs.BackgroundColor = Color.White;
        _logs.CellBorderStyle = DataGridViewCellBorderStyle.None;
        _logs.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
        _logs.ColumnHeadersHeight = 34;
        _logs.ColumnHeadersVisible = false;
        _logs.Columns.AddRange(new DataGridViewColumn[] { _timeColumn, _levelColumn, _messageColumn });
        dataGridViewCellStyle4.Alignment = DataGridViewContentAlignment.MiddleLeft;
        dataGridViewCellStyle4.BackColor = Color.White;
        dataGridViewCellStyle4.Font = new Font("Microsoft YaHei UI", 9F);
        dataGridViewCellStyle4.ForeColor = Color.FromArgb(100, 111, 124);
        dataGridViewCellStyle4.Padding = new Padding(3);
        dataGridViewCellStyle4.SelectionBackColor = Color.FromArgb(238, 245, 255);
        dataGridViewCellStyle4.SelectionForeColor = Color.FromArgb(32, 40, 48);
        dataGridViewCellStyle4.WrapMode = DataGridViewTriState.False;
        _logs.DefaultCellStyle = dataGridViewCellStyle4;
        _logs.Dock = DockStyle.Fill;
        _logs.Location = new Point(0, 44);
        _logs.Margin = new Padding(0);
        _logs.Name = "_logs";
        _logs.ReadOnly = true;
        _logs.RowHeadersVisible = false;
        _logs.RowHeadersWidth = 62;
        _logs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _logs.Size = new Size(1614, 161);
        _logs.TabIndex = 1;
        //
        // _timeColumn
        //
        _timeColumn.MinimumWidth = 8;
        _timeColumn.Name = "_timeColumn";
        _timeColumn.ReadOnly = true;
        _timeColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _timeColumn.Width = 74;
        //
        // _levelColumn
        //
        _levelColumn.MinimumWidth = 8;
        _levelColumn.Name = "_levelColumn";
        _levelColumn.ReadOnly = true;
        _levelColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        _levelColumn.Width = 52;
        //
        // _messageColumn
        //
        _messageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        _messageColumn.DefaultCellStyle = dataGridViewCellStyle2;
        _messageColumn.MinimumWidth = 8;
        _messageColumn.Name = "_messageColumn";
        _messageColumn.ReadOnly = true;
        _messageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // _footer
        //
        _footer.ColumnCount = 3;
        _footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _footer.ColumnStyles.Add(new ColumnStyle());
        _footer.ColumnStyles.Add(new ColumnStyle());
        _footer.Controls.Add(_progressPanel, 0, 0);
        _footer.Controls.Add(_stop, 1, 0);
        _footer.Controls.Add(_start, 2, 0);
        _footer.Dock = DockStyle.Fill;
        _footer.Location = new Point(33, 1109);
        _footer.Margin = new Padding(0);
        _footer.Name = "_footer";
        _footer.Padding = new Padding(0, 24, 0, 12);
        _footer.RowCount = 1;
        _footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _footer.Size = new Size(1614, 117);
        _footer.TabIndex = 3;
        //
        // _progressPanel
        //
        _progressPanel.ColumnCount = 2;
        _progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        _progressPanel.ColumnStyles.Add(new ColumnStyle());
        _progressPanel.Controls.Add(_summary, 0, 0);
        _progressPanel.Controls.Add(_fraction, 1, 0);
        _progressPanel.Controls.Add(_progress, 0, 1);
        _progressPanel.Dock = DockStyle.Fill;
        _progressPanel.Location = new Point(0, 24);
        _progressPanel.Margin = new Padding(0, 0, 27, 0);
        _progressPanel.Name = "_progressPanel";
        _progressPanel.RowCount = 2;
        _progressPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        _progressPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 12F));
        _progressPanel.Size = new Size(1159, 81);
        _progressPanel.TabIndex = 0;
        //
        // _summary
        //
        _summary.Anchor = AnchorStyles.Left;
        _summary.AutoSize = true;
        _summary.ForeColor = Color.FromArgb(100, 111, 124);
        _summary.Location = new Point(0, 22);
        _summary.Margin = new Padding(0);
        _summary.Name = "_summary";
        _summary.Size = new Size(82, 24);
        _summary.TabIndex = 0;
        _summary.Text = "准备就绪";
        _summary.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _fraction
        //
        _fraction.Anchor = AnchorStyles.Left;
        _fraction.AutoSize = true;
        _fraction.ForeColor = Color.FromArgb(100, 111, 124);
        _fraction.Location = new Point(1109, 22);
        _fraction.Margin = new Padding(0);
        _fraction.Name = "_fraction";
        _fraction.Size = new Size(50, 24);
        _fraction.TabIndex = 1;
        _fraction.Text = "0 / 0";
        _fraction.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _progress
        //
        _progressPanel.SetColumnSpan(_progress, 2);
        _progress.Dock = DockStyle.Fill;
        _progress.Location = new Point(0, 69);
        _progress.Margin = new Padding(0);
        _progress.Name = "_progress";
        _progress.Size = new Size(1159, 12);
        _progress.Style = ProgressBarStyle.Continuous;
        _progress.TabIndex = 2;
        //
        // _stop
        //
        _stop.AccessibleName = "停止后续上传";
        _stop.AutoSize = true;
        _stop.BackColor = Color.White;
        _stop.Cursor = Cursors.Hand;
        _stop.Enabled = false;
        _stop.FlatAppearance.BorderColor = Color.FromArgb(220, 225, 231);
        _stop.FlatStyle = FlatStyle.Flat;
        _stop.ForeColor = Color.FromArgb(32, 40, 48);
        _stop.Location = new Point(1186, 30);
        _stop.Margin = new Padding(0, 6, 14, 0);
        _stop.MinimumSize = new Size(90, 50);
        _stop.Name = "_stop";
        _stop.Padding = new Padding(15, 4, 15, 4);
        _stop.Size = new Size(225, 63);
        _stop.TabIndex = 1;
        _stop.Text = "停止后续上传";
        _toolTip.SetToolTip(_stop, "等待当前包结束，再停止后续包；不会中断正在发送的请求。");
        _stop.UseVisualStyleBackColor = false;
        //
        // _start
        //
        _start.AccessibleName = "开始上传";
        _start.AutoSize = true;
        _start.BackColor = Color.FromArgb(18, 100, 189);
        _start.Cursor = Cursors.Hand;
        _start.FlatAppearance.BorderColor = Color.FromArgb(18, 100, 189);
        _start.FlatStyle = FlatStyle.Flat;
        _start.ForeColor = Color.White;
        _start.Location = new Point(1425, 30);
        _start.Margin = new Padding(0, 6, 0, 0);
        _start.MinimumSize = new Size(189, 60);
        _start.Name = "_start";
        _start.Padding = new Padding(15, 4, 15, 4);
        _start.Size = new Size(189, 63);
        _start.TabIndex = 2;
        _start.Text = "开始上传";
        _start.UseVisualStyleBackColor = false;
        //
        // _statusStrip
        //
        _statusStrip.BackColor = Color.White;
        _statusStrip.ImageScalingSize = new Size(24, 24);
        _statusStrip.Items.AddRange(new ToolStripItem[] { _status, _timeoutStatus, _projectLink });
        _statusStrip.Location = new Point(0, 1244);
        _statusStrip.Name = "_statusStrip";
        _statusStrip.Padding = new Padding(22, 0, 22, 0);
        _statusStrip.ShowItemToolTips = true;
        _statusStrip.Size = new Size(1680, 31);
        _statusStrip.TabIndex = 1;
        //
        // _status
        //
        _status.ForeColor = Color.FromArgb(25, 117, 68);
        _status.Name = "_status";
        _status.Size = new Size(1326, 24);
        _status.Spring = true;
        _status.Text = "● 就绪";
        _status.TextAlign = ContentAlignment.MiddleLeft;
        //
        // _timeoutStatus
        //
        _timeoutStatus.ForeColor = Color.FromArgb(100, 111, 124);
        _timeoutStatus.Name = "_timeoutStatus";
        _timeoutStatus.Size = new Size(143, 24);
        _timeoutStatus.Text = "上传超时 300 秒";
        //
        // _projectLink
        //
        _projectLink.AccessibleName = "GitHub 项目主页";
        _projectLink.IsLink = true;
        _projectLink.Margin = new Padding(18, 3, 0, 2);
        _projectLink.Name = "_projectLink";
        _projectLink.Size = new Size(149, 26);
        _projectLink.Text = "GitHub 项目主页";
        //
        // _toolTip
        //
        _toolTip.AutoPopDelay = 15000;
        _toolTip.InitialDelay = 400;
        _toolTip.ReshowDelay = 100;
        //
        // MainForm
        //
        AutoScaleDimensions = new SizeF(144F, 144F);
        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = Color.FromArgb(244, 246, 248);
        ClientSize = new Size(1680, 1275);
        Controls.Add(_body);
        Controls.Add(_statusStrip);
        Margin = new Padding(4, 4, 4, 4);
        MinimumSize = new Size(1429, 947);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "NuGet 上传助手";
        _body.ResumeLayout(false);
        _configuration.ResumeLayout(false);
        _configurationHeading.ResumeLayout(false);
        _configurationHeading.PerformLayout();
        _authenticationButtons.ResumeLayout(false);
        _authenticationButtons.PerformLayout();
        _sourceFields.ResumeLayout(false);
        _sourceFields.PerformLayout();
        _apiKeyFields.ResumeLayout(false);
        _apiKeyFields.PerformLayout();
        _accountFields.ResumeLayout(false);
        _usernameFields.ResumeLayout(false);
        _usernameFields.PerformLayout();
        _passwordFields.ResumeLayout(false);
        _passwordFields.PerformLayout();
        _configurationFooter.ResumeLayout(false);
        _configurationFooter.PerformLayout();
        _packagePanel.ResumeLayout(false);
        _packagePanel.PerformLayout();
        _packageHeading.ResumeLayout(false);
        _packageHeading.PerformLayout();
        _packageButtons.ResumeLayout(false);
        _packageButtons.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_grid).EndInit();
        _logPanel.ResumeLayout(false);
        _logPanel.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)_logs).EndInit();
        _footer.ResumeLayout(false);
        _footer.PerformLayout();
        _progressPanel.ResumeLayout(false);
        _progressPanel.PerformLayout();
        _statusStrip.ResumeLayout(false);
        _statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel _body = null!;
    private TableLayoutPanel _configuration = null!;
    private TableLayoutPanel _configurationHeading = null!;
    private Label _configurationTitle = null!;
    private FlowLayoutPanel _authenticationButtons = null!;
    private TableLayoutPanel _sourceFields = null!;
    private Label _sourceFieldsLabel = null!;
    private TableLayoutPanel _apiKeyFields = null!;
    private Label _apiKeyFieldsLabel = null!;
    private TableLayoutPanel _accountFields = null!;
    private TableLayoutPanel _usernameFields = null!;
    private Label _usernameFieldsLabel = null!;
    private TableLayoutPanel _passwordFields = null!;
    private Label _passwordFieldsLabel = null!;
    private TableLayoutPanel _configurationFooter = null!;
    private Label _configurationPath = null!;
    private Label _saveState = null!;
    private TextBox _source = null!;
    private TextBox _apiKey = null!;
    private TextBox _username = null!;
    private TextBox _password = null!;
    private TableLayoutPanel _packagePanel = null!;
    private TableLayoutPanel _packageHeading = null!;
    private Label _packageCount = null!;
    private FlowLayoutPanel _packageButtons = null!;
    private Label _dropArea = null!;
    private Label _selection = null!;
    private DataGridView _grid = null!;
    private DataGridViewCheckBoxColumn _selectedColumn = null!;
    private DataGridViewTextBoxColumn _fileNameColumn = null!;
    private DataGridViewTextBoxColumn _versionColumn = null!;
    private DataGridViewTextBoxColumn _sizeColumn = null!;
    private DataGridViewTextBoxColumn _remoteVersionColumn = null!;
    private DataGridViewTextBoxColumn _publishedColumn = null!;
    private DataGridViewTextBoxColumn _comparisonColumn = null!;
    private DataGridViewTextBoxColumn _uploadStateColumn = null!;
    private TableLayoutPanel _logPanel = null!;
    private Label _logTitle = null!;
    private DataGridView _logs = null!;
    private DataGridViewTextBoxColumn _timeColumn = null!;
    private DataGridViewTextBoxColumn _levelColumn = null!;
    private DataGridViewTextBoxColumn _messageColumn = null!;
    private TableLayoutPanel _footer = null!;
    private TableLayoutPanel _progressPanel = null!;
    private Label _summary = null!;
    private Label _fraction = null!;
    private ProgressBar _progress = null!;
    private Button _keyMode = null!;
    private Button _accountMode = null!;
    private Button _save = null!;
    private Button _add = null!;
    private Button _folder = null!;
    private Button _refresh = null!;
    private Button _remove = null!;
    private Button _clear = null!;
    private Button _start = null!;
    private Button _stop = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _status = null!;
    private ToolStripStatusLabel _timeoutStatus = null!;
    private ToolStripStatusLabel _projectLink = null!;
    private ToolTip _toolTip = null!;
}
