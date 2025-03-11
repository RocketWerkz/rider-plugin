# Installing locally

From the release page, download the latest release zip and install it using the `Install plugin from disk` option in the settings.

Detailed instructions can be found here: https://www.jetbrains.com/help/idea/managing-plugins.html#install_plugin_from_disk

# How to Test

In the Rider IDE the configuration MUST be set to the OS platform you are using. For Windows, it would be `Rider (Windows)`, for Unix it is `Rider (Unix)`. Then hit the `Play` icon (`Debug` does not work). This executes a new Rider daemon session where you must create or open an existing Rider project that automatically enables the Plugin for specific that Rider project. This way you can test and experience any code changes before officially deploying to the Rider plugin marketplace.

# Plugin Development

The official Rider [Unity plugin](https://github.com/JetBrains/resharper-unity) and [Godot plugin](https://github.com/JetBrains/godot-support) and their Github repos are a good resource to examine plugin features that we want to replicate for BRUTAL. Developers should be aware that there is a huge lack of official docs to outline the process of developing a Rider plugin therefore relying on existing published plugins that already exist are a good starting point.

The lack of a debug option and breakpoints when testing the plugin means that the developer must rely on Log statements that must be executed on the Root at an error level for them to even show up. For example in `ColorHighlighterProcess`:

```csharp
// Unwind the argument values into a string for logging purposes
var argValues = string.Join(", ", arguments.Select(arg => arg.Value?.GetText()));
Log.Root.Error($"argValues: {argValues}");
```
# Official Plugin

https://plugins.jetbrains.com/plugin/26768-brutal

# Info

https://blog.jetbrains.com/dotnet/2019/02/14/writing-plugins-resharper-rider/

https://www.youtube.com/watch?v=y8adERbgt_M&t=3434s

