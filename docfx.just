# Launching DocFX website
@serve:
    DocFxTocGenerator -d ./docs -s --folderRef None
    docfx docfx.json --serve --open-browser

# Launching DocFX website
@build:
    DocFxTocGenerator -d ./docs -s --folderRef None
    docfx docfx.json

# Deletes _site, api folders
@clean:
    rm -rf _site
    rm -rf api 