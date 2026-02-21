# PhotinoEx

PhotinoEx is a Fork of [Photino](https://github.com/tryphotino) aiming to fix up bugs, add features and move one part of the project from C++ to C#.

I'm hoping this will make it easier to debug and develop on.

Currently do not use this in production. Once feature parity is done, i'll start working on some fixes/features from the old repo.

## RoadMap
- [x] Linux support.
    - [x] Update to GTK4.
    - [x] Update to Webkit6
    - [x] Window controls
    - [x] Webview controls
    - [x] Folder/File/Message opening
    - [x] Toast for system
    - [ ] Tray menu/icon
    - [x] Application Icons (use .dekstop files)
- [ ] Windows support.
    - [x] Window controls
    - [x] Webview controls
    - [x] Dark Mode
    - [ ] OS automatic Dark Mode theming
    - [x] Folder/File/Message opening
    - [x] Application Icons
- [ ] Mac support. (currently dont have an environment to test)
- [x] Update to Dotnet 10
- [ ] Toast System for Windows
- [ ] System Tray Icon and menu
- [ ] Setup Nugets
- [ ] A tag navigation - allowing it to open on browser eternally
- [ ] implement AreBrowserAcceleratorKeysEnabled across platforms
- [ ] Use Ilogger for logging - instead of just Console.WriteLine
- [ ] Decide if I want to support chrome/no titlebar dragging, Custom Titlebar
- [ ] SingleInstanceMode whilst being configurable
- [ ] Android/Ios Support?

## Requirements

- Dotnet 10.
- An IDE supporting C# and Dotnet.
    - I will recommend [Rider](https://www.jetbrains.com/rider/) but VisualStudio should also work fine.

## Build from CLI

```
git clone https://github.com/PhotinoEx/PhotinoEx.git PhotinoEx
cd PhotinoEx
dotnet build
```

## Photino
Photino contained three packages:
- [Blazor](https://github.com/tryphotino/photino.Blazor)
- [NET](https://github.com/tryphotino/photino.NET)
- [Native](https://github.com/tryphotino/photino.Native)

The Native Library has been rewritten to C# and the NET lib has been consolidated into Core with that.

## Contributing

Pull requests are welcome. For major changes, please open an issue first
to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License

[Apache 2.0](https://choosealicense.com/licenses/apache-2.0)
- I have tried to abide to the best of my knowledge, please get in touch if anything is a problem
