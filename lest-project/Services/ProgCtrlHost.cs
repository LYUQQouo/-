using System.ComponentModel;
using System.Runtime.InteropServices;
using LestProject.Native;

namespace LestProject.Services;

/// <summary>
/// 設定 ProgCtrl.dll 搜尋目錄並封裝啟停服務。DLL 不可從安裝目錄拷出（V4.1）。
/// </summary>
internal sealed class ProgCtrlHost
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetDllDirectory(string? lpPathName);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    public string DllDirectory { get; private set; } = @"C:\ACROVIEW\MultiAprog\";

    public bool IsBound { get; private set; }

    public void ApplyDllDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory))
            throw new ArgumentException("DLL 目錄不可為空。");

        var dir = directory.Trim();
        if (!dir.EndsWith(Path.DirectorySeparatorChar) && !dir.EndsWith(Path.AltDirectorySeparatorChar))
            dir += Path.DirectorySeparatorChar;

        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"找不到 DLL 目錄：{dir}");

        var dllPath = Path.Combine(dir, "ProgCtrl.dll");
        if (!File.Exists(dllPath))
            throw new FileNotFoundException($"目錄下找不到 ProgCtrl.dll：{dllPath}");

        if (!SetDllDirectory(dir))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetDllDirectory 失敗");

        var handle = LoadLibrary(dllPath);
        if (handle == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"LoadLibrary 失敗：{dllPath}");

        DllDirectory = dir;
        IsBound = true;
    }

    public bool StartService(bool connectProgrammers)
    {
        EnsureBound();
        return connectProgrammers ? ProgCtrlNative.StartService() : ProgCtrlNative.StartServiceSimple();
    }

    public bool StopService()
    {
        EnsureBound();
        return ProgCtrlNative.StopService();
    }

    public bool CheckService()
    {
        EnsureBound();
        return ProgCtrlNative.CheckService();
    }

    private void EnsureBound()
    {
        if (!IsBound)
            throw new InvalidOperationException("請先套用 DLL 目錄。");
    }
}
