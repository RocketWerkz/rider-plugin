using System;
using JetBrains.Application;
using JetBrains.Application.Parts;
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.OnlineHelp;
using JetBrains.ReSharper.Psi;

namespace RW.Brutal;

[ShellComponent(Instantiation.DemandAnyThreadSafe)]
public class OnlineHelpProvider : IOnlineHelpProvider
{
    // For now there are no other providers like this one
    public int Priority => 20;
    public bool ShouldValidate => false;

    public Uri GetUrl(IDeclaredElement element)
    {
        var info = GetFullPath(element);

        if (!info.Valid())
            return null;
        
        if (!info.Namespace.StartsWith("Brutal"))
            return null;

        var url = BrutalUrlUtils.GetPackageIdFromNamespace(info.Namespace);

        if (string.IsNullOrEmpty(url))
            return null;

        var endPoint = info.FullName;
        var scope = ParseXmlDocId(element);
        var link = $"{url}api/{endPoint}#{scope}";
        return new Uri(link);
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

    private static string ParseXmlDocId(IDeclaredElement element)
    {
        var id = element.GetType()
            .GetProperty("XMLDocId")
            ?.GetValue(element)
            .ToString();
        
        if (string.IsNullOrEmpty(id))
            return null;
        
        // remove start of string
        id = id.Substring(2, id.Length - 2);
        
        // replace invalid characters
        id = id
            .Replace(".", "_")
            .Replace(",", "_")
            .Replace("(", "_")
            .Replace(")", "_");
        
        return id;
    }
    
    private static Info GetFullPath(IDeclaredElement element)
    {
        return element switch
        {
            INamespace nsElem => new Info
            {
                FullName = nsElem.QualifiedName,
                Namespace = nsElem.QualifiedName
            },
            ITypeElement type => new Info
            {
                FullName = type.GetClrName().FullName,
                Namespace = type.GetClrName().GetNamespaceName(),
            },
            ITypeMember member => new Info
            {
                FullName = member.ContainingType?.GetClrName().FullName,
                Namespace = member.ContainingType?.GetClrName().GetNamespaceName(),
            },
            _ => default
        };
    }
    
    private struct Info
    {
        public string FullName;
        public string Namespace;

        public bool Valid()
        {
            return FullName is not null || Namespace is not null;
        }
    }
}