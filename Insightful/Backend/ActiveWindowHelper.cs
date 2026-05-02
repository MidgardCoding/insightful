using Insightful;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Threading;

public class ActiveWindowHelper
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public static string GetActiveProcessPath()
    {
        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero) return null;

        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0) return null;

        try
        {
            using (var proc = Process.GetProcessById((int)pid))
            {
                return GetMainModuleFileName(proc);
            }
        }
        catch
        {
            return null;
        }
    }

    private static string GetMainModuleFileName(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch
        {
            return QueryFullProcessImageNameByPid(process.Id);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private static string QueryFullProcessImageNameByPid(int pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)pid);
        if (hProcess == IntPtr.Zero) return null;
        try
        {
            var sb = new StringBuilder(1024);
            int size = sb.Capacity;
            if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                return sb.ToString();
            return null;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }

    DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
    string _lastPath = null;

    public void MonitorActiveWindows()
    {
        _timer.Tick += (s, e) =>
        {
            var path = ActiveWindowHelper.GetActiveProcessPath();
            if (string.IsNullOrEmpty(path)) return;
            path = Path.GetFullPath(path).ToLowerInvariant();
            if (path != _lastPath)
            {
                _lastPath = path;
                return;
            }
        };
        _timer.Start();
    }
}
