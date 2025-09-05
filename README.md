# Brutal Rider Plugin

The Brutal Rider Plugin adds specific functionality for the Brutal Framework to the JetBrains Rider IDE.

This plugin currently has the following features:
* Path resource search and completion
* Color wheel and color icon display on internal classes and methods

##  Installation with Rider

> Unforunately the plugin is currently blocked by JetBrains. We are currently waiting for the block to be lifted, as this prevents the plugin from being displayed and updated via the Marketplace. If users have already installed the plugin via Marketplace, they can freely enable and disable it via the Rider IDE, but new users search and install it.

In the Rider IDE -> Settings - > Search for `Brutal` and select Install. After installation it may prompt you to restart the IDE for the plugin to work.

Offical Plugin Link: https://plugins.jetbrains.com/plugin/26768-brutal

## Features

**Path resource search and completion:**

* From the root folder of your project, if a `Content` folder exists, an auto complete box will be displayed showing the possible paths after pressing `/`. This can only be triggered within a string, for example `"Content/Shaders/Line.vert"`

  <img width="662" height="236" alt="pathCompletion" src="https://github.com/user-attachments/assets/ec222f4e-4047-42e5-9664-68672def62fb" />

## Installing locally

From the release page, download the latest release zip and install it using the `Install plugin from disk` option in the settings.

Detailed instructions can be found here: https://www.jetbrains.com/help/idea/managing-plugins.html#install_plugin_from_disk

## How to Test

Building the project alone isn't enough to test code changes to the plugin. Unlike regular C# projects, Rider plugins must be run inside a special Rider host process. This launches a separate instance of the Rider IDE with the plugin automatically attached, this is the only way to test and experience any code changes before official deployment to the Marketplace.

> Recommended: If you already have the plugin enabled via the Marketplace, it is good to turn this off so it does not conflict while testing the plugin.

1. In the Rider IDE the configuration MUST be set to the OS platform you are using:
* `Rider (Windows)` for Windows
*  `Rider (Unix)` for Linux/macOS

2. Click the `Play` icon (`Debug` does not work).

3. Wait for the Rider daemon session to launch, if this is your first time this may take several minutes to launch as it installs several dependencies.

4. A new Rider window opens, from here you can create or open an existing Rider project. Make sure to select a project you want to test the plugin on.

## Plugin Development Tips

* Refer to the official Rider [Unity plugin](https://github.com/JetBrains/resharper-unity) and [Godot plugin](https://github.com/JetBrains/godot-support) for examples of Rider plugin architecture. The Unity one is particularly good for color highlighting as they already have this functionality.

* Debugging is limited - breakpoints do not work when testing the plugin. Instead, `Log` statements must be executed at a Root error level to debug behaviour. For example in `ColorHighlighterProcess`:

```csharp
// Unwind the argument values into a string for logging purposes
var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
Log.Root.Error($"argValues: {argValues}");
```

## Resources

* [JetBrains Blog: Writing Plugins for ReSharper and Rider](https://blog.jetbrains.com/dotnet/2019/02/14/writing-plugins-resharper-rider/)

* [YouTube: Building Extensions for Rider and ReSharper](https://www.youtube.com/watch?v=y8adERbgt_M&t=3434s) (timestamped for the relevant section)

