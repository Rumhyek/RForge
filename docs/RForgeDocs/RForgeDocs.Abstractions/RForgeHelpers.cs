using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RForgeDocs.Abstractions;
public static class RForgeHelpers
{

    public const string RForgeDocsRepoUrl = "https://github.com/Rumhyek/RForge/tree/main/docs/RForgeDocs/RForgeDocs";

    public static string RepoDocsUrl(string filePath)
    {
        if (filePath.StartsWith('/') == false)
            return $"{RForgeDocsRepoUrl}/{filePath}";

        return $"{RForgeDocsRepoUrl}{filePath}";
    }

}
