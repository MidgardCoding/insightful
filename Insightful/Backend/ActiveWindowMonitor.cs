using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

public class ActiveWindowChangedEventArgs : EventArgs
{
    public string? ExePath { get; }
    public IntPtr HWnd { get; }

    public ActiveWindowChangedEventArgs(IntPtr hwnd, string? exePath)
    {
        HWnd = hwnd;
        ExePath = exePath;
    }
}

public class ActiveWindowMonitor : IDisposable
{
    public event EventHandler<ActiveWindowChangedEventArgs>? ActiveWindowChanged;

    private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);
    private WinEventDelegate? _procDelegate;
    private IntPtr _hook = IntPtr.Zero;

    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);
    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public ActiveWindowMonitor()
    {
        _procDelegate = new WinEventDelegate(WinEventProc);
        _hook = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, _procDelegate, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        Task.Run(() =>
        {
            string? path = GetProcessPathFromHwnd(hwnd);
            ActiveWindowChanged?.Invoke(this, new ActiveWindowChangedEventArgs(hwnd, path));
        });
    }

    public static string? GetProcessPathFromHwnd(IntPtr hwnd)
    {
        if (hwnd == IntPtr.Zero) return null;
        try
        {
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;
            return GetProcessPathByPid((int)pid);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetProcessPathByPid(int pid)
    {
        try
        {
            using var proc = Process.GetProcessById(pid);
            try
            {
                return proc.MainModule?.FileName;
            }
            catch
            {
                return QueryFullProcessImageNameByPid((uint)pid);
            }
        }
        catch
        {
            return null;
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

    private static string? QueryFullProcessImageNameByPid(uint pid)
    {
        IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
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

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWinEvent(_hook);
            _hook = IntPtr.Zero;
        }
        _procDelegate = null;
    }
}
