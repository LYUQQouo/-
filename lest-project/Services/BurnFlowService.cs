using System.Text;
using LestProject.Native;

namespace LestProject.Services;

internal enum ProgramMode
{
    BlankCheck = 1,
    Verify = 2,
    Program = 3,
    Erase = 4,
    Secure = 5,
}

internal sealed class BurnFlowService
{
    private readonly ProgCtrlHost _host;
    private readonly Action<string> _log;

    public BurnFlowService(ProgCtrlHost host, Action<string> log)
    {
        _host = host;
        _log = log;
    }

    /// <summary>完整燒錄：建立工程 → StartProject → 報告。</summary>
    public async Task FullProgrammingFlowAsync(
        string projectFile,
        string chipInfo,
        string firmwareFile,
        int quantity,
        ProgramMode mode,
        CancellationToken ct)
    {
        ValidatePath(projectFile, "工程檔");
        ValidatePath(firmwareFile, "韌體檔");
        if (string.IsNullOrWhiteSpace(chipInfo))
            throw new ArgumentException("晶片資訊不可為空。");

        EnsureService();
        Log($"CreateProject → {projectFile}");
        if (!ProgCtrlNative.CreateProject(projectFile, chipInfo.Trim(), firmwareFile, quantity, (int)mode))
            throw new InvalidOperationException("CreateProject 受理失敗。");

        await WaitResultOkAsync("CreateProject", ct).ConfigureAwait(false);

        Log("StartProject");
        if (!ProgCtrlNative.StartProject())
            throw new InvalidOperationException("StartProject 失敗。");

        await WaitResultOkAsync("StartProject", ct).ConfigureAwait(false);
        await DumpReportAsync(ct).ConfigureAwait(false);
    }

    /// <summary>載入既有工程並燒錄。</summary>
    public async Task LoadAndProgramAsync(string projectFile, int? targetPassCount, CancellationToken ct)
    {
        ValidatePath(projectFile, "工程檔");
        EnsureService();

        Log($"LoadProject → {projectFile}");
        if (!ProgCtrlNative.LoadProject(projectFile))
            throw new InvalidOperationException("LoadProject 受理失敗。");

        await WaitResultOkAsync("LoadProject", ct).ConfigureAwait(false);

        Log("StartProject");
        if (!ProgCtrlNative.StartProject())
            throw new InvalidOperationException("StartProject 失敗。");

        await WaitResultOkAsync("StartProject", ct).ConfigureAwait(false);

        if (targetPassCount is > 0)
            await WaitUntilPassCountAsync(targetPassCount.Value, ct).ConfigureAwait(false);

        await DumpReportAsync(ct).ConfigureAwait(false);
    }

    /// <summary>僅建立工程並取 Checksum（不 StartProject）。</summary>
    public async Task ChecksumOnlyFlowAsync(
        string projectFile,
        string chipInfo,
        string firmwareFile,
        CancellationToken ct)
    {
        ValidatePath(projectFile, "工程檔輸出路徑");
        ValidatePath(firmwareFile, "韌體檔");
        if (string.IsNullOrWhiteSpace(chipInfo))
            throw new ArgumentException("晶片資訊不可為空。");

        // 僅建工程可用 StartServiceSimple（不必連編程器）
        if (!_host.CheckService())
        {
            Log("StartServiceSimple（Checksum 流程可不連編程器）");
            if (!_host.StartService(connectProgrammers: false))
                throw new InvalidOperationException("StartServiceSimple 失敗。");
        }

        Log($"CreateProject → {projectFile}");
        if (!ProgCtrlNative.CreateProject(projectFile, chipInfo.Trim(), firmwareFile, 1, (int)ProgramMode.Program))
            throw new InvalidOperationException("CreateProject 受理失敗。");

        await WaitResultOkAsync("CreateProject", ct).ConfigureAwait(false);

        var buf = new byte[8];
        if (!ProgCtrlNative.GetChecksum(buf))
            throw new InvalidOperationException("GetChecksum 失敗。");

        // 小端 8 bytes
        ulong value = 0;
        for (int i = 0; i < 8; i++)
            value |= (ulong)buf[i] << (8 * i);

        Log($"Checksum = 0x{value:X16}");
    }

    /// <summary>
    /// StartProject 成功後，把 IPS 製作的 .tsk 路徑交給自動機（ProgCtrl 通道）。
    /// 真正執行 .tsk 仍是 IPS；此處僅傳路徑與托盤參數。
    /// </summary>
    public void LoadTskToAutomatic(
        string tskPath,
        int targetOkCount,
        bool supplyFromStart = true,
        int supplyX = 0,
        int supplyY = 0)
    {
        ValidatePath(tskPath, ".tsk");
        var startFlags = supplyFromStart ? 0 : 1;
        // TaskType=1；字串格式見 V4.1 LoadTaskDataToAutomatic
        var str =
            $"3,{targetOkCount},{tskPath},{startFlags},{supplyX},{supplyY},0,0,0,0,0,0,0,,,,,,,";
        Log($"LoadTaskDataToAutomatic → {tskPath}");
        if (!ProgCtrlNative.LoadTaskDataToAutomatic(1, str))
            throw new InvalidOperationException("LoadTaskDataToAutomatic 失敗。");
    }

    private void EnsureService()
    {
        if (_host.CheckService())
            return;
        Log("StartService（連編程器）");
        if (!_host.StartService(connectProgrammers: true))
            throw new InvalidOperationException("StartService 失敗。請確認編程器連線與 Multi MES/Automatic 設定。");
    }

    private async Task WaitResultOkAsync(string step, CancellationToken ct)
    {
        var sb = new StringBuilder(256);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            sb.Clear();
            if (!ProgCtrlNative.GetResult(sb, sb.Capacity))
                throw new InvalidOperationException($"{step}: GetResult 呼叫失敗。");

            var r = sb.ToString().Trim();
            Log($"{step} GetResult = {r}");
            if (r.Equals("OK", StringComparison.OrdinalIgnoreCase))
                return;
            if (r.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"{step} 失敗：{r}");
            await Task.Delay(200, ct).ConfigureAwait(false);
        }
    }

    private async Task WaitUntilPassCountAsync(int target, CancellationToken ct)
    {
        var sb = new StringBuilder(8192);
        while (true)
        {
            ct.ThrowIfCancellationRequested();
            sb.Clear();
            if (ProgCtrlNative.GetStatusJson(sb, sb.Capacity))
            {
                var json = sb.ToString();
                Log($"Status: {Truncate(json, 200)}");
                // 輕量解析 Pass
                var pass = TryReadIntField(json, "Pass");
                if (pass >= target)
                {
                    Log($"已達目標 Pass 數：{pass}");
                    return;
                }
            }

            var result = new StringBuilder(64);
            if (ProgCtrlNative.GetResult(result, result.Capacity))
            {
                var r = result.ToString().Trim();
                if (r.Equals("OK", StringComparison.OrdinalIgnoreCase))
                    return;
                if (r.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"燒錄失敗：{r}");
            }

            await Task.Delay(500, ct).ConfigureAwait(false);
        }
    }

    private async Task DumpReportAsync(CancellationToken ct)
    {
        await Task.Yield();
        var sb = new StringBuilder(16384);
        if (ProgCtrlNative.GetReportJson(sb, sb.Capacity))
            Log($"Report: {Truncate(sb.ToString(), 500)}");
        else
            Log("GetReport_Json 無資料或失敗（可忽略）。");
    }

    private static int TryReadIntField(string json, string name)
    {
        var key = $"\"{name}\"";
        var i = json.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return -1;
        i = json.IndexOf(':', i);
        if (i < 0) return -1;
        var end = i + 1;
        while (end < json.Length && (char.IsWhiteSpace(json[end]) || json[end] == '\"'))
            end++;
        var start = end;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-'))
            end++;
        return int.TryParse(json.AsSpan(start, end - start), out var v) ? v : -1;
    }

    private static void ValidatePath(string path, string label)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException($"{label} 路徑不可為空。");
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";

    private void Log(string msg) => _log($"[{DateTime.Now:HH:mm:ss}] {msg}");
}
