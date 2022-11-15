using System.IO;
using System.Text.RegularExpressions;

namespace Plang;

public class CheckFileValidity
{
    #region Functions to check if the commandline inputs are legal

    public static bool IsLegalProjectName(string projectName)
    {
        return Regex.IsMatch(projectName, "^[A-Za-z_][A-Za-z_0-9]*$");
    }

    public static bool IsLegalPFile(string fileName, out FileInfo file)
    {
        file = null;
        if (fileName.Length <= 2 || !fileName.EndsWith(".p") || !File.Exists(Path.GetFullPath(fileName)))
        {
            return false;
        }

        var path = Path.GetFullPath(fileName);
        file = new FileInfo(path);

        return true;
    }

    public static bool IsLegalPProjFile(string fileName, out FileInfo file)
    {
        file = null;
        if (fileName.Length <= 2 || !fileName.EndsWith(".pproj") || !File.Exists(Path.GetFullPath(fileName)))
        {
            return false;
        }

        var path = Path.GetFullPath(fileName);
        file = new FileInfo(path);

        return true;
    }

    #endregion Functions to check if the commandline inputs are legal
}