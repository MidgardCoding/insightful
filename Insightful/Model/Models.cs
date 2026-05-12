using System.Collections.Generic;

public class Shortcut
{
    public string Name { get; set; }
    public string KeyCombination { get; set; }
}

public class AppEntry
{
    public string AppTitle { get; set; }
    public string AppSrc { get; set; }
    public List<Shortcut> Shortcuts { get; set; }
}
