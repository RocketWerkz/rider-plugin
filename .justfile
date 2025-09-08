set windows-shell := ["powershell.exe", "-NoLogo", "-Command"]

build:
	dotnet build
	cmd /c 'gradlew.bat :runIde -x compileKotlin -x compileDotNet'