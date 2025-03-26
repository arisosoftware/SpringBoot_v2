using System;
using System.IO;

class Program
{
    static void Main()
    {
        string rootFolder = "rootfolder"; // Root folder to locate first
        string targetPath = "ghost/bin"; // Relative path from root
        string startDir = Directory.GetCurrentDirectory();

        string? foundPath = FindTargetFolder(startDir, rootFolder, targetPath);

        if (foundPath != null)
            Console.WriteLine("Found: " + foundPath);
        else
            Console.WriteLine("Not found.");
    }

    static string? FindTargetFolder(string startDir, string rootFolder, string targetPath)
    {
        DirectoryInfo? dir = new DirectoryInfo(startDir);

        // Step 1: Traverse up to locate the root folder
        while (dir != null && !dir.Name.Equals(rootFolder, StringComparison.OrdinalIgnoreCase))
        {
            dir = dir.Parent;
        }

        if (dir == null)
            return null; // Root folder not found

        // Step 2: Search downward for the target folder from the root folder
        string targetFullPath = Path.Combine(dir.FullName, targetPath);
        return Directory.Exists(targetFullPath) ? Path.GetFullPath(targetFullPath) : null;
    }
}
