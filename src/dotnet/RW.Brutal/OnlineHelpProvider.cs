using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Psi;

namespace RW.Brutal;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class OnlineHelpProvider : IOnlineHelpProvider
{
    public Uri GetUrl(IDeclaredElement element)
    {
        if (!IsAvailable(element)) return null;
        
        // When pressing F1 link to Brutal docs
        return new Uri("https://brutal.rocketwerkz.com/");
    }
    
    public string GetPresentableName(IDeclaredElement element)
    {
        return element?.ShortName ?? "<unknown>";
    }

    public bool IsAvailable(IDeclaredElement element)
    {
        // Check if valid declared symbol
        return element is not null;
    }

    // For now there are no other providers like this one
    public int Priority => 20;
    public bool ShouldValidate => false;
}