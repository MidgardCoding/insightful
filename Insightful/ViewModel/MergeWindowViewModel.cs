using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Insightful.Model;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Insightful.ViewModel;

public sealed class MergeWindowViewModel : INotifyPropertyChanged
{
    private readonly string _packageJsonPath;
    private readonly Action? _closeWindow;
    private MergeSourcePackageViewModel? _selectedImportPackage;
    private List<WindowData> _workingApps = [];
    private WindowData _workingDefault = new();

    public MergeWindowViewModel(string packageJsonPath, Action? closeWindow = null)
    {
        _packageJsonPath = packageJsonPath;
        _closeWindow = closeWindow;

        AddFilesCommand = new RelayCommand(_ => AddFiles());
        MergeCommand = new RelayCommand(_ => MergeSelected(), _ => SelectedImportPackage != null);
        RemoveSelectedAppsCommand = new RelayCommand(_ => RemoveSelectedApps(), _ => MainApps.Any(a => a.IsSelected));
        SaveCommand = new RelayCommand(_ => Save(silent: false));
        SaveAndCloseCommand = new RelayCommand(_ =>
        {
            if (Save(silent: false))
                _closeWindow?.Invoke();
        });
        HelpCommand = new RelayCommand(_ => ShowHelp());

        LoadPackageJson();
        RefreshCommandStates();
    }

    public ObservableCollection<MergeSourcePackageViewModel> ImportPackages { get; } = new();

    public ObservableCollection<PackageJsonAppRowViewModel> MainApps { get; } = new();

    public MergeSourcePackageViewModel? SelectedImportPackage
    {
        get => _selectedImportPackage;
        set
        {
            if (ReferenceEquals(_selectedImportPackage, value))
                return;
            _selectedImportPackage = value;
            OnPropertyChanged();
            RaiseMergeCommand();
        }
    }

    public ICommand AddFilesCommand { get; }
    public ICommand MergeCommand { get; }
    public ICommand RemoveSelectedAppsCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAndCloseCommand { get; }
    public ICommand HelpCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void RaiseMergeCommand()
    {
        if (MergeCommand is RelayCommand r)
            r.RaiseCanExecuteChanged();
    }

    private void RaiseRemoveCommand()
    {
        if (RemoveSelectedAppsCommand is RelayCommand r)
            r.RaiseCanExecuteChanged();
    }

    private void RefreshCommandStates()
    {
        RaiseMergeCommand();
        RaiseRemoveCommand();
    }

    private void LoadPackageJson()
    {
        _workingApps = [];
        _workingDefault = new WindowData
        {
            AppTitle = "No HUD for this application",
            AppSrc = "",
            Shortcuts = [new ShortcutItem { Name = "Copy", KeyCombination = "Ctrl+C" }],
            AppNotes = []
        };

        if (!File.Exists(_packageJsonPath))
        {
            RebuildMainAppRows();
            return;
        }

        try
        {
            var root = JObject.Parse(File.ReadAllText(_packageJsonPath));
            if (root["apps"] is JArray arr)
                _workingApps = arr.ToObject<List<WindowData>>() ?? [];

            if (root["default"] != null)
            {
                var d = root["default"]!.ToObject<WindowData>();
                if (d != null)
                    _workingDefault = d;
            }
        }
        catch
        {
            MessageBox.Show(
                "Could not read package.json. Starting with an empty app list until you save a valid file.",
                "Load warning",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        RebuildMainAppRows();
    }

    private void RebuildMainAppRows()
    {
        foreach (var row in MainApps)
            row.SelectionChanged -= OnMainAppSelectionChanged;

        MainApps.Clear();
        foreach (var app in _workingApps)
        {
            var row = new PackageJsonAppRowViewModel(app);
            row.SelectionChanged += OnMainAppSelectionChanged;
            MainApps.Add(row);
        }

        RaiseRemoveCommand();
    }

    private void OnMainAppSelectionChanged() => RaiseRemoveCommand();

    private void AddFiles()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Add package files",
            Filter = "JSON and Insight packages (*.json;*.insight)|*.json;*.insight|JSON (*.json)|*.json|Insight (*.insight)|*.insight|All files (*.*)|*.*",
            Multiselect = true
        };

        if (dlg.ShowDialog() != true)
            return;

        var existing = new HashSet<string>(ImportPackages.Select(p => p.DedupeKey), StringComparer.OrdinalIgnoreCase);

        foreach (var path in dlg.FileNames)
        {
            try
            {
                foreach (var pkg in MergePackageImporter.LoadPackagesFromFile(path))
                {
                    if (!existing.Add(pkg.DedupeKey))
                        continue;
                    ImportPackages.Add(pkg);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Could not read \"{Path.GetFileName(path)}\": {ex.Message}",
                    "Import error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        RefreshCommandStates();
    }

    private void MergeSelected()
    {
        var sel = SelectedImportPackage;
        if (sel == null || sel.Apps.Count == 0)
        {
            MessageBox.Show("Select a package that contains at least one application.", "Merge", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        foreach (var app in sel.Apps)
        {
            var clone = JsonConvert.DeserializeObject<WindowData>(JsonConvert.SerializeObject(app));
            if (clone != null)
                _workingApps.Add(clone);
        }

        RebuildMainAppRows();
        RefreshCommandStates();
    }

    private void RemoveSelectedApps()
    {
        var selected = MainApps.Where(a => a.IsSelected).ToList();
        if (selected.Count == 0)
            return;

        var msg = selected.Count == 1
            ? $"Remove \"{selected[0].AppTitle}\" from package.json? This cannot be undone except by re-importing or restoring a backup."
            : $"Remove {selected.Count} applications from package.json? This cannot be undone except by re-importing or restoring a backup.";

        if (MessageBox.Show(msg, "Confirm removal", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        foreach (var row in selected)
            _workingApps.Remove(row.Data);

        RebuildMainAppRows();
        RefreshCommandStates();
    }

    private bool Save(bool silent)
    {
        try
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(_packageJsonPath));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            JObject doc;
            if (File.Exists(_packageJsonPath))
            {
                try
                {
                    doc = JObject.Parse(File.ReadAllText(_packageJsonPath));
                }
                catch
                {
                    doc = new JObject();
                }
            }
            else
            {
                doc = new JObject();
            }

            doc["apps"] = JArray.Parse(JsonConvert.SerializeObject(_workingApps));
            doc["default"] = JObject.Parse(JsonConvert.SerializeObject(_workingDefault));

            File.WriteAllText(_packageJsonPath, doc.ToString(Formatting.Indented));

            if (!silent)
            {
                MessageBox.Show("package.json has been saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not save package.json: {ex.Message}", "Save error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private static void ShowHelp()
    {
        MessageBox.Show(
            "Add one or more .json or .insight files. Each listed package shows a title from the file.\n\n" +
            "Select a package and click Merge to append its applications to the apps list in the main Insight Package.\n\n" +
            "Below, choose applications you want to remove, then use Remove selected (you will be asked to confirm).\n\n",
            "Merge packages - help",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}

public sealed class MergeSourcePackageViewModel
{
    public MergeSourcePackageViewModel(string displayTitle, string filePath, string? dictionaryKey, IReadOnlyList<WindowData> apps)
    {
        DisplayTitle = displayTitle;
        FilePath = filePath;
        DictionaryKey = dictionaryKey;
        Apps = apps;
        DedupeKey = string.IsNullOrEmpty(dictionaryKey)
            ? $"{filePath}\0{displayTitle}"
            : $"{filePath}\0{dictionaryKey}";
    }

    public string DisplayTitle { get; }
    public string FilePath { get; }
    public string? DictionaryKey { get; }
    public IReadOnlyList<WindowData> Apps { get; }
    internal string DedupeKey { get; }

    public string FileLabel => Path.GetFileName(FilePath);
    public string AppsSummary => Apps.Count == 1 ? "1 application" : $"{Apps.Count} applications";
}

public sealed class PackageJsonAppRowViewModel : INotifyPropertyChanged
{
    private bool _isSelected;

    public PackageJsonAppRowViewModel(WindowData data) => Data = data;

    public WindowData Data { get; }

    public string AppTitle => string.IsNullOrWhiteSpace(Data.AppTitle) ? "(no title)" : Data.AppTitle!;

    public string AppSrcDisplay => string.IsNullOrWhiteSpace(Data.AppSrc) ? "-" : Data.AppSrc!;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            OnPropertyChanged();
            SelectionChanged?.Invoke();
        }
    }

    public event Action? SelectionChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

internal static class MergePackageImporter
{
    public static IReadOnlyList<MergeSourcePackageViewModel> LoadPackagesFromFile(string filePath)
    {
        var text = File.ReadAllText(filePath);
        var root = JObject.Parse(text);
        var list = new List<MergeSourcePackageViewModel>();
        var fileTitle = Path.GetFileNameWithoutExtension(filePath);

        if (root["apps"] is JArray appsArray)
        {
            var apps = appsArray.ToObject<List<WindowData>>() ?? [];
            apps = apps.Where(a => !string.IsNullOrWhiteSpace(a.AppTitle) || !string.IsNullOrWhiteSpace(a.AppSrc)).ToList();
            if (apps.Count > 0)
                list.Add(new MergeSourcePackageViewModel($"{fileTitle} (apps)", filePath, null, apps));
        }

        foreach (var prop in root.Properties())
        {
            if (string.Equals(prop.Name, "default", StringComparison.OrdinalIgnoreCase))
                continue;
            if (string.Equals(prop.Name, "apps", StringComparison.OrdinalIgnoreCase))
                continue;
            if (prop.Value is not JArray arr)
                continue;

            var chunk = arr.ToObject<List<WindowData>>() ?? [];
            chunk = chunk.Where(a => !string.IsNullOrWhiteSpace(a.AppTitle) || !string.IsNullOrWhiteSpace(a.AppSrc)).ToList();
            if (chunk.Count == 0)
                continue;

            list.Add(new MergeSourcePackageViewModel(prop.Name, filePath, prop.Name, chunk));
        }

        return list;
    }
}
