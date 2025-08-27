set windows-shell := ["powershell.exe", "-NoLogo", "-Command"]

# Build DocFX website
build:
    docfx metadata
    docfx build

# Build DocFX website + serve locally + open browser
serve:
    docfx serve _site

# Deletes _site, api folders
clean:
    rm -rf _site
    rm -rf api 
