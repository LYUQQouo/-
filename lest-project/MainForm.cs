using LestProject.Services;

namespace LestProject;

public sealed class MainForm : Form
{
    private readonly ProgCtrlHost _host = new();
    private readonly MesFolderService _mes = new();
    private BurnFlowService? _flows;
    private CancellationTokenSource? _cts;

    private readonly TextBox _txtDll = new();
    private readonly Button _btnApplyDll = new();
    private readonly TextBox _txtMesFolder = new();
    private readonly Button _btnMesBrowse = new();
    private readonly ListBox _lstMesFiles = new();
    private readonly TextBox _txtLog = new();

    // Flow 1
    private readonly TextBox _f1Project = new();
    private readonly TextBox _f1Chip = new();
    private readonly TextBox _f1Fw = new();
    private readonly NumericUpDown _f1Qty = new();
    private readonly ComboBox _f1Mode = new();
    private readonly TextBox _f1Tsk = new();

    // Flow 2
    private readonly TextBox _f2Project = new();
    private readonly NumericUpDown _f2Pass = new();
    private readonly TextBox _f2Tsk = new();

    // Flow 3
    private readonly TextBox _f3Project = new();
    private readonly TextBox _f3Chip = new();
    private readonly TextBox _f3Fw = new();

    public MainForm()
    {
        Text = "BurnDevice Control";
        Width = 920;
        Height = 860;
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft JhengHei UI", 9F);
        MinimumSize = new Size(800, 700);

        BuildUi();
        _flows = new BurnFlowService(_host, AppendLog);

        _txtDll.Text = @"C:\ACROVIEW\MultiAprog\";
        _f1Chip.Text = "{CC2530F128}{VQFN40(6x6)-S01}";
        _f3Chip.Text = "{W25Q128JV}{SOIC8-S01}";
        _f1Qty.Value = 4;
        _f2Pass.Value = 1;
        _f1Mode.SelectedIndex = 2; // Program
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(10),
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 160));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 200));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        // DLL row
        var dllRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
        dllRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        dllRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dllRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
        dllRow.Controls.Add(new Label { Text = "DLL 目錄 :", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtDll.Dock = DockStyle.Fill;
        dllRow.Controls.Add(_txtDll, 1, 0);
        _btnApplyDll.Text = "套用";
        _btnApplyDll.Dock = DockStyle.Fill;
        _btnApplyDll.Click += (_, _) => ApplyDll();
        dllRow.Controls.Add(_btnApplyDll, 2, 0);
        root.Controls.Add(dllRow, 0, 0);

        // MES folder section
        root.Controls.Add(BuildMesPanel(), 0, 1);

        // Three flows
        root.Controls.Add(BuildFlow1(), 0, 2);
        root.Controls.Add(BuildFlow2(), 0, 3);
        root.Controls.Add(BuildFlow3(), 0, 4);

        // Log
        var logBox = new GroupBox { Text = "執行紀錄", Dock = DockStyle.Fill };
        _txtLog.Multiline = true;
        _txtLog.ScrollBars = ScrollBars.Both;
        _txtLog.Dock = DockStyle.Fill;
        _txtLog.ReadOnly = true;
        _txtLog.Font = new Font("Consolas", 9F);
        logBox.Controls.Add(_txtLog);
        root.Controls.Add(logBox, 0, 5);
    }

    private Control BuildMesPanel()
    {
        var box = new GroupBox
        {
            Text = "MES 資料（瀏覽資料夾；檔名／副檔名不限，由客戶提供）",
            Dock = DockStyle.Fill,
        };
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2, Padding = new Padding(6) };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label { Text = "MES 資料夾 :", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
        _txtMesFolder.Dock = DockStyle.Fill;
        _txtMesFolder.ReadOnly = true;
        layout.Controls.Add(_txtMesFolder, 1, 0);
        _btnMesBrowse.Text = "瀏覽...";
        _btnMesBrowse.Dock = DockStyle.Fill;
        _btnMesBrowse.Click += (_, _) => BrowseMesFolder();
        layout.Controls.Add(_btnMesBrowse, 2, 0);

        _lstMesFiles.Dock = DockStyle.Fill;
        _lstMesFiles.HorizontalScrollbar = true;
        _lstMesFiles.DoubleClick += (_, _) => UseSelectedMesFile();
        layout.Controls.Add(_lstMesFiles, 0, 1);
        layout.SetColumnSpan(_lstMesFiles, 3);

        var hint = new Label
        {
            Text = "提示：雙擊清單檔案可填入目前焦點欄位；工程偏好 .apr/.apj，自動機偏好 .tsk，其餘副檔名皆可。",
            AutoSize = true,
            Dock = DockStyle.Bottom,
            ForeColor = Color.DimGray,
        };
        box.Controls.Add(hint);
        box.Controls.Add(layout);
        return box;
    }

    private Control BuildFlow1()
    {
        var box = new GroupBox
        {
            Text = "1. FullProgrammingFlow —— 完整燒錄流程（建立工程 → 燒錄 → 取得報告）",
            Dock = DockStyle.Fill,
        };
        var t = Grid();
        int r = 0;
        AddBrowseRow(t, r++, "工程檔 :", _f1Project, BrowseAnyFile, "選擇工程檔輸出路徑", saveMode: true);
        AddTextRow(t, r++, "晶片資訊 :", _f1Chip);
        AddBrowseRow(t, r++, "韌體檔 :", _f1Fw, BrowseAnyFile, "選擇韌體／燒錄資料檔");
        AddBrowseRow(t, r++, ".tsk（可選）:", _f1Tsk, BrowseAnyFile, "選擇 IPS 任務檔");

        var opt = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
        opt.Controls.Add(new Label { Text = "數量 :", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _f1Qty.Minimum = 1;
        _f1Qty.Maximum = 1_000_000;
        _f1Qty.Width = 80;
        opt.Controls.Add(_f1Qty);
        opt.Controls.Add(new Label { Text = "模式 :", AutoSize = true, Margin = new Padding(12, 6, 4, 0) });
        _f1Mode.DropDownStyle = ComboBoxStyle.DropDownList;
        _f1Mode.Items.AddRange(new object[]
        {
            "1 BlankCheck", "2 Verify", "3 Program", "4 Erase", "5 Secure",
        });
        _f1Mode.Width = 140;
        opt.Controls.Add(_f1Mode);
        t.Controls.Add(new Label { Text = "選項 :", AutoSize = true, Anchor = AnchorStyles.Left }, 0, r);
        t.Controls.Add(opt, 1, r);
        r++;

        var btn = PrimaryButton("FullProgrammingFlow");
        btn.Click += async (_, _) => await RunSafeAsync(async ct =>
        {
            await _flows!.FullProgrammingFlowAsync(
                _f1Project.Text.Trim(),
                _f1Chip.Text,
                _f1Fw.Text.Trim(),
                (int)_f1Qty.Value,
                (ProgramMode)(_f1Mode.SelectedIndex + 1),
                ct);
            if (!string.IsNullOrWhiteSpace(_f1Tsk.Text))
                _flows.LoadTskToAutomatic(_f1Tsk.Text.Trim(), (int)_f1Qty.Value);
        });
        PlaceAction(t, r, btn);
        box.Controls.Add(t);
        return box;
    }

    private Control BuildFlow2()
    {
        var box = new GroupBox
        {
            Text = "2. LoadAndProgram —— 載入已有工程並燒錄",
            Dock = DockStyle.Fill,
        };
        var t = Grid();
        int r = 0;
        AddBrowseRow(t, r++, "工程檔 :", _f2Project, BrowseAnyFile, "選擇既有工程檔");
        AddBrowseRow(t, r++, ".tsk（可選）:", _f2Tsk, BrowseAnyFile, "選擇 IPS 任務檔");

        var opt = new FlowLayoutPanel { Dock = DockStyle.Fill };
        opt.Controls.Add(new Label { Text = "目標 Pass 數 :", AutoSize = true, Margin = new Padding(0, 6, 4, 0) });
        _f2Pass.Minimum = 1;
        _f2Pass.Maximum = 1_000_000;
        _f2Pass.Width = 80;
        opt.Controls.Add(_f2Pass);
        t.Controls.Add(new Label { Text = "選項 :", AutoSize = true, Anchor = AnchorStyles.Left }, 0, r);
        t.Controls.Add(opt, 1, r);
        r++;

        var btn = PrimaryButton("LoadAndProgram");
        btn.Click += async (_, _) => await RunSafeAsync(async ct =>
        {
            await _flows!.LoadAndProgramAsync(_f2Project.Text.Trim(), (int)_f2Pass.Value, ct);
            if (!string.IsNullOrWhiteSpace(_f2Tsk.Text))
                _flows.LoadTskToAutomatic(_f2Tsk.Text.Trim(), (int)_f2Pass.Value);
        });
        PlaceAction(t, r, btn);
        box.Controls.Add(t);
        return box;
    }

    private Control BuildFlow3()
    {
        var box = new GroupBox
        {
            Text = "3. ChecksumOnlyFlow —— 僅取得 CheckSum",
            Dock = DockStyle.Fill,
        };
        var t = Grid();
        int r = 0;
        AddBrowseRow(t, r++, "工程檔 :", _f3Project, BrowseAnyFile, "選擇工程檔輸出路徑", saveMode: true);
        AddTextRow(t, r++, "晶片資訊 :", _f3Chip);
        AddBrowseRow(t, r++, "韌體檔 :", _f3Fw, BrowseAnyFile, "選擇韌體／燒錄資料檔");

        var btn = PrimaryButton("ChecksumOnlyFlow");
        btn.Click += async (_, _) => await RunSafeAsync(ct =>
            _flows!.ChecksumOnlyFlowAsync(_f3Project.Text.Trim(), _f3Chip.Text, _f3Fw.Text.Trim(), ct));
        PlaceAction(t, r, btn);
        box.Controls.Add(t);
        return box;
    }

    private static TableLayoutPanel Grid()
    {
        var t = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            Padding = new Padding(8),
        };
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        return t;
    }

    private static void AddTextRow(TableLayoutPanel t, int row, string label, TextBox box)
    {
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        box.Dock = DockStyle.Fill;
        t.Controls.Add(box, 1, row);
        t.SetColumnSpan(box, 2);
    }

    private void AddBrowseRow(
        TableLayoutPanel t,
        int row,
        string label,
        TextBox box,
        Action<TextBox, string, bool> browser,
        string title,
        bool saveMode = false)
    {
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        t.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        box.Dock = DockStyle.Fill;
        t.Controls.Add(box, 1, row);
        var btn = new Button { Text = "瀏覽...", Dock = DockStyle.Fill };
        btn.Click += (_, _) => browser(box, title, saveMode);
        t.Controls.Add(btn, 2, row);
    }

    private static void PlaceAction(TableLayoutPanel t, int row, Button btn)
    {
        t.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        t.Controls.Add(btn, 2, row);
    }

    private static Button PrimaryButton(string text) => new()
    {
        Text = text,
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(0, 120, 215),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Height = 30,
    };

    private void BrowseAnyFile(TextBox target, string title, bool saveMode)
    {
        // 副檔名不限（客戶提供）；同時列出常見偏好供參考
        const string filter =
            "所有檔案 (*.*)|*.*|" +
            "ACROVIEW 工程 (*.apr;*.apj)|*.apr;*.apj|" +
            "IPS 任務 (*.tsk)|*.tsk|" +
            "韌體／資料 (*.bin;*.hex;*.s19;*.mot)|*.bin;*.hex;*.s19;*.mot";

        if (saveMode)
        {
            using var dlg = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                FilterIndex = 1,
                OverwritePrompt = true,
            };
            if (!string.IsNullOrWhiteSpace(_mes.RootFolder))
                dlg.InitialDirectory = _mes.RootFolder;
            if (dlg.ShowDialog(this) == DialogResult.OK)
                target.Text = dlg.FileName;
            return;
        }

        using var open = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            FilterIndex = 1,
            CheckFileExists = true,
            Multiselect = false,
        };
        if (!string.IsNullOrWhiteSpace(_mes.RootFolder))
            open.InitialDirectory = _mes.RootFolder;
        if (open.ShowDialog(this) == DialogResult.OK)
            target.Text = open.FileName;
    }

    private void BrowseMesFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "選擇客戶 MES／檔案伺服器提供的資料夾（檔名副檔名不限）",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK)
            return;

        try
        {
            _mes.SetRoot(dlg.SelectedPath);
            _txtMesFolder.Text = dlg.SelectedPath;
            RefreshMesList();
            AppendLog($"[{DateTime.Now:HH:mm:ss}] MES 資料夾：{dlg.SelectedPath}（{_lstMesFiles.Items.Count} 個檔案）");

            // 便利：偏好填入尚未填的欄位
            _f1Fw.Text = string.IsNullOrWhiteSpace(_f1Fw.Text)
                ? _mes.PreferFile(".bin", ".hex", ".s19") ?? _f1Fw.Text
                : _f1Fw.Text;
            _f1Tsk.Text = string.IsNullOrWhiteSpace(_f1Tsk.Text)
                ? _mes.PreferFile(".tsk") ?? _f1Tsk.Text
                : _f1Tsk.Text;
            _f2Project.Text = string.IsNullOrWhiteSpace(_f2Project.Text)
                ? _mes.PreferFile(".apr", ".apj") ?? _f2Project.Text
                : _f2Project.Text;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "MES 資料夾", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshMesList()
    {
        _lstMesFiles.Items.Clear();
        foreach (var f in _mes.ListFiles())
            _lstMesFiles.Items.Add(f);
    }

    private void UseSelectedMesFile()
    {
        if (_lstMesFiles.SelectedItem is not string path)
            return;

        var active = ActiveControl as TextBox
                     ?? _f1Project;
        // 若焦點不在路徑框，依副檔名導向
        var ext = Path.GetExtension(path);
        if (ActiveControl is not TextBox)
        {
            if (ext.Equals(".tsk", StringComparison.OrdinalIgnoreCase))
                active = _f1Tsk;
            else if (ext.Equals(".apr", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".apj", StringComparison.OrdinalIgnoreCase))
                active = _f2Project;
            else
                active = _f1Fw;
        }

        active.Text = path;
        AppendLog($"[{DateTime.Now:HH:mm:ss}] 已填入：{path}");
    }

    private void ApplyDll()
    {
        try
        {
            _host.ApplyDllDirectory(_txtDll.Text);
            AppendLog($"[{DateTime.Now:HH:mm:ss}] 已套用 DLL 目錄：{_host.DllDirectory}");
            MessageBox.Show(this, "DLL 目錄已套用。", "ProgCtrl", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "套用 DLL 失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task RunSafeAsync(Func<CancellationToken, Task> action)
    {
        if (!_host.IsBound)
        {
            MessageBox.Show(this, "請先「套用」DLL 目錄。", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        UseWaitCursor = true;
        try
        {
            await action(ct);
            AppendLog($"[{DateTime.Now:HH:mm:ss}] 流程完成。");
        }
        catch (OperationCanceledException)
        {
            AppendLog($"[{DateTime.Now:HH:mm:ss}] 已取消。");
        }
        catch (Exception ex)
        {
            AppendLog($"[{DateTime.Now:HH:mm:ss}] ERROR: {ex.Message}");
            MessageBox.Show(this, ex.Message, "執行失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void AppendLog(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(line));
            return;
        }

        _txtLog.AppendText(line + Environment.NewLine);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        try
        {
            if (_host.IsBound && _host.CheckService())
                _host.StopService();
        }
        catch
        {
            // ignore shutdown errors
        }

        base.OnFormClosing(e);
    }
}
