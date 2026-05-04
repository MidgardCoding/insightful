using Insightful.Backend;
using Insightful.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Insightful.ViewModel;

public sealed class MainWindowViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly HudRegistry _registry;
    private readonly ActiveWindowHelper _activeMonitor;
    private WindowData _currentWindowData;

    public MainWindowViewModel(string configPath)
    {
        _registry = HudRegistry.LoadFromFile(configPath);
        _currentWindowData = _registry.ResolveForExecutable(ActiveWindowHelper.GetActiveProcessPath());

        _activeMonitor = new ActiveWindowHelper();
        _activeMonitor.ActiveProcessPathChanged += OnActiveProcessPathChanged;
        _activeMonitor.StartMonitoring();
    }

    public WindowData CurrentWindowData
    {
        get => _currentWindowData;
        private set
        {
            _currentWindowData = value;
            OnPropertyChanged();
            AreNotesAvailable();
        }
    }

    private void OnActiveProcessPathChanged(string? path)
    {
        CurrentWindowData = _registry.ResolveForExecutable(path);
    }

    public void Dispose()
    {
        _activeMonitor.ActiveProcessPathChanged -= OnActiveProcessPathChanged;
        _activeMonitor.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public string NoteAvailability { set; get; } = "Tap on a note to read more";

    public void AreNotesAvailable()
    {
        NoteAvailability = _currentWindowData?.AppNotes?.Count > 0 ? "Tap on a note to read more" : "No notes for this application";
        OnPropertyChanged(nameof(NoteAvailability));
    }
}
