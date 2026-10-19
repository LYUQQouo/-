using System.Runtime.InteropServices;
using System.Text;

namespace LestProject.Native;

/// <summary>
/// ProgCtrl.dll (舊名 StdMes.dll) P/Invoke。匯出符號以 AC_ 開頭，呼叫約定 stdcall。
/// 對照《標準燒錄軟體控制介面設計與實現 V4.1》。
/// </summary>
internal static class ProgCtrlNative
{
    private const string DllName = "ProgCtrl.dll";

    [DllImport(DllName, EntryPoint = "AC_StartService", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StartService();

    [DllImport(DllName, EntryPoint = "AC_StartServiceSimple", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StartServiceSimple();

    [DllImport(DllName, EntryPoint = "AC_StopService", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StopService();

    [DllImport(DllName, EntryPoint = "AC_CheckService", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CheckService();

    [DllImport(DllName, EntryPoint = "AC_CreateProject", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CreateProject(string projectFile, string chipInfo, string dataFile, int quantity, int funcMode);

    [DllImport(DllName, EntryPoint = "AC_LoadProject", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LoadProject(string projectFile);

    [DllImport(DllName, EntryPoint = "AC_StartProject", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StartProject();

    [DllImport(DllName, EntryPoint = "AC_StopProject", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool StopProject();

    [DllImport(DllName, EntryPoint = "AC_GetResult", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetResult(StringBuilder buffer, int size);

    [DllImport(DllName, EntryPoint = "AC_GetChecksum", CallingConvention = CallingConvention.StdCall)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetChecksum(byte[] buffer);

    [DllImport(DllName, EntryPoint = "AC_GetStatus_Json", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetStatusJson(StringBuilder buffer, int size);

    [DllImport(DllName, EntryPoint = "AC_GetReport_Json", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetReportJson(StringBuilder buffer, int size);

    [DllImport(DllName, EntryPoint = "AC_GetProgrammerInfo_Json", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetProgrammerInfoJson(StringBuilder buffer, int size);

    [DllImport(DllName, EntryPoint = "AC_LoadTaskDataToAutomatic", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool LoadTaskDataToAutomatic(uint taskType, string strTaskData);

    [DllImport(DllName, EntryPoint = "AC_SetProjectParameter", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetProjectParameter(string domain, string parameter);
}
