# PhotinoEx

PhotinoEx is a Fork of [Photino](https://github.com/tryphotino) aiming to fix up bugs, add features and move one part of the project from C++ to C#.

I'm hoping this will make it easier to debug and develop on.

Currently do not use this in production. Once feature parity is done, i'll start working on some fixes/features from the old repo.

## Requirements

- Dotnet 10.
- Windows: the Microsoft Edge WebView2 Evergreen Runtime.
- Linux: GTK 4 and WebKitGTK 6 runtime libraries.
- macOS: Apple Silicon or Intel.
- An IDE supporting C# and Dotnet.
    - I will recommend [Rider](https://www.jetbrains.com/rider/) but VisualStudio should also work fine.

## Install

For a Blazor desktop application:

```sh
dotnet add package PhotinoXDX.Blazor --prerelease
```

For direct access to the native window and webview APIs:

```sh
dotnet add package PhotinoXDX.Core --prerelease
```

Packages target .NET 10 and are published for Windows, macOS, and Linux. Remove
`--prerelease` once a stable version is available.

## Build from CLI

```sh
git clone https://github.com/XDX-Org/PhotinoXDX.git
cd PhotinoXDX
dotnet build
```

## Releasing

Push a SemVer tag to publish both packages through NuGet trusted publishing:

```sh
git tag v0.1.0-preview.1
git push origin v0.1.0-preview.1
```

The release workflow builds and tests all projects, validates the generated
packages in clean applications, publishes them to NuGet, and creates a GitHub
release. The GitHub `release` environment must contain the `NUGET_USER` secret.

## Photino
Photino contained three packages:
- [Blazor](https://github.com/tryphotino/photino.Blazor)
- [NET](https://github.com/tryphotino/photino.NET)
- [Native](https://github.com/tryphotino/photino.Native)

The Native Library has been rewritten to C# and the NET lib has been consolidated into Core with that.

## Clipboard

Blazor components can copy text through the native operating-system clipboard:

```razor
@inject PhotinoEx.Blazor.IPhotinoExClipboard Clipboard

<button @onclick="Copy">Copy</button>

@code {
    private Task Copy() => Clipboard.CopyTextAsync("Text to copy").AsTask();
}
```

Files and directories can be copied together. Directories are copied recursively by the operating system when pasted, so no recursive flag is required:

```csharp
await Clipboard.CopyFilesAsync(
    ["/path/to/report.pdf", "/path/to/folder"]
);
```

See [Native Clipboard](docs/clipboard.md) for implementation status, platform formats, and Core API usage.

Planned work is tracked in the phased [implementation roadmap](docs/roadmap.md).

## Contributing

Pull requests are welcome. For major changes, please open an issue first
to discuss what you would like to change.

Please make sure to update tests as appropriate.

## License

[Apache 2.0](https://choosealicense.com/licenses/apache-2.0)
- I have tried to abide to the best of my knowledge, please get in touch if anything is a problem
