namespace Insightful.Model;

public class WindowData
{
    public string? AppTitle { get; set; }
    public List<ShortcutItem>? Shortcuts { get; set; }
    public string? AppSrc { get; set; }
    public List<AppNote>? AppNotes { get; set; }
}

public class ShortcutItem
{
    public string? Name { get; set; }
    public string? KeyCombination { get; set; }
}

public class AppNote
{
    public string? NoteTitle { get; set; }
    public string? NoteContent { get; set; }
}