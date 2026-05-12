<div align="center">
  <h1>Insightful 🧠</h1>
  <p>A minimal, glassmorphic HUD that shows you the <i>right information at the right time</i> - right above your active application.</p>
  <br>
  <img width="888" height="421" alt="Zrzut ekranu 2026-05-12 183534" src="https://github.com/user-attachments/assets/1116d3ca-def3-4ea5-a035-e2af97e0d6d8" />
  <br>
  <b>Insightful</b> is a lightweight Windows overlay built with <b>WPF</b> and <b>.NET</b>.
  Instead of guessing what an app does or digging through menus, Insightful displays custom “Insight Packages” - plain JSON files created by you - with shortcuts, notes, system usage stats and a fully themable appearance!
</div>

## ✨ Features

- 🪟 **Always-on-top HUD** - stays visible above other windows but can be made click-through.
- 📦 **JSON-powered packages** - define what appears for any application (`code.exe`, `blender.exe`...).
- 🎨 **Custom themes (upcoming)** - per-app colour, font size, window width and more.
- ⌨ **Keyboard shortcuts at a glance** - never forget a hotkey again.
- 📝 **Quick notes (upcoming)** - context-sensitive reminders you write once.
- 🖥 **System monitoring (upcoming)** - CPU, RAM, GPU and disk usage from Task Manager.
- 🌫 **Acrylic / glassmorphism design** - modern, semi-transparent background with blur.
- 🧩 **Extensible layout** - placeholder for images, dynamic grids and more.

## 🚀 Getting Started

### Prerequisites
- Windows 10/11 (build 1803+ for acrylic blur)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet)

> [!NOTE]
> Package Creator was recently added making it easier to create packages! Try it out!

## 📄 Package Format (NOW with a proper PACKAGE GENERATOR!)

A minimal example (`taskmgr.insight`):

```
{
  "cmd": [
    {
      "AppTitle": "cmd",
      "AppSrc": "C:\\Windows\\System32\\cmd.exe",
      "Shortcuts": [
        {
          "Name": "Kill Script",
          "KeyCombination": "Ctrl+C"
        }
      ],
      "AppNotes": [
        {
          "NoteTitle": "Wow, a CMD!",
          "NoteContent": "Let's pretend to be a hacker!"
        }
      ]
    }
  ]
}
```

More properties soon.

## 🖥 Tech Stack

| Area               | Technology                          |
|--------------------|-------------------------------------|
| UI Framework       | WPF (.NET 10)                      |
| Window Management  | P/Invoke (`user32.dll`, DWM)       |
| JSON Parsing       | `Newtonsoft.Json`  |
| Process Monitoring | `System.Diagnostics` + future `LibreHardwareMonitor` |
| Package Editing    | In-app WPF editors (finished!)        |

## 🛣️ Roadmap

- [x] Core HUD window (always on top, acrylic)
- [x] JSON package loading
- [x] Dynamic layout with shortcuts grid
- [x] Built-in package editor GUI (Package Creator)
- [ ] CPU / RAM / GPU usage display
- [ ] Conditional triggers (show only when window title matches)
- [ ] Dynamic status lines (`"RAM: {ram_usage}%"`)
- [ ] Tray icon & settings UI
- [ ] Multi-monitor support

## 🤝 Contributing

This project started as a learning journey in C# + WPF. Ideas, bug reports and pull requests are more than welcome!

To contribute:
1. Fork the repository.
2. Create a branch for your feature.
3. Open a PR describing your changes.

Please check the open issues before starting.

## 📝 License

Apache 2.0 © [MidgardCoding] - feel free to use, modify and learn from the code.
