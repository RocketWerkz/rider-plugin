set windows-shell := ["powershell.exe", "-NoLogo", "-Command"]

rider-version := "2025.2"

# Workaround for running gradlew on Windows with PowerShell
[windows]
@build:
	dotnet build
	cmd /c 'gradlew.bat :runIde -x compileKotlin -x compileDotNet'

# Installs the plugin to the default Rider plugins directory
[windows]
@install:
	./gradlew.bat buildPlugin
	unzip -o output/*.zip -d "$env:APPDATA/JetBrains/Rider{{rider-version}}/plugins"
	just post-install

[macos]
@install:
	./gradlew buildPlugin
	unzip -o output/*.zip -d - ~/Library/Application Support/JetBrains/Rider{{rider-version}}/plugins
	just post-install

[linux]
@install:
	./gradlew buildPlugin
	unzip -o output/*.zip -d ~/.local/share/JetBrains/Rider{{rider-version}}/plugins
	just post-install

[private]
@post-install:
	echo "Plugin installed. Please restart Rider if it was running."
