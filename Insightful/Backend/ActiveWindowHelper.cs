using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Threading;

namespace Insightful.Backend;

/// <summary>
/// Polls the foreground window and exposes the owning process executable path.
/// </summary>
public sealed class ActiveWindowHelper : IDisposable
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

    private const uint ProcessQueryLimitedInformation = 0x1000;

    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(400) };
    private string? _lastPath;
    private bool _disposed;

    public event Action<string?>? ActiveProcessPathChanged;

    public ActiveWindowHelper()
    {
        _timer.Tick += OnTick;
    }

    public void StartMonitoring()
    {
        _timer.Start();
        OnTick(null, EventArgs.Empty);
    }

    public void StopMonitoring()
    {
        _timer.Stop();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var path = GetActiveProcessPath();
        if (string.IsNullOrEmpty(path))
            return;

        if (IsCurrentProcess(path))
            return;

        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            return;
        }

        if (string.Equals(path, _lastPath, StringComparison.OrdinalIgnoreCase))
            return;

        _lastPath = path;
        ActiveProcessPathChanged?.Invoke(path);
    }

    /// <inheritdoc cref="Dispose"/>
    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _timer.Tick -= OnTick;
        _timer.Stop();
    }

    private static bool IsCurrentProcess(string path)
    {
        var self = Environment.ProcessPath;
        if (string.IsNullOrEmpty(self))
            return false;
        try
        {
            return string.Equals(Path.GetFullPath(self), Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string? GetActiveProcessPath()
    {
        IntPtr hWnd = GetForegroundWindow();
        if (hWnd == IntPtr.Zero)
            return null;

        GetWindowThreadProcessId(hWnd, out uint pid);
        if (pid == 0)
            return null;

        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return GetMainModuleFileName(proc);
        }
        catch
        {
            return null;
        }
    }

    private static string? GetMainModuleFileName(Process process)
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

    private static string? QueryFullProcessImageNameByPid(int pid)
    {
        IntPtr hProcess = OpenProcess(ProcessQueryLimitedInformation, false, (uint)pid);
        if (hProcess == IntPtr.Zero)
            return null;
        try
        {
            var sb = new StringBuilder(1024);
            int size = sb.Capacity;
            return QueryFullProcessImageName(hProcess, 0, sb, ref size) ? sb.ToString() : null;
        }
        finally
        {
            CloseHandle(hProcess);
        }
    }
}
