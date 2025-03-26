using System;
using System.IO;

class Program
{
    static void Main()
    {
        string inputFolder = "targetFolder"; // Change this to the folder name you're searching for
        string rootFolder = "root"; // Define the stopping condition
        string startDir = Directory.GetCurrentDirectory();
        
        string? foundPath = FindFolder(startDir, inputFolder, rootFolder);

        if (foundPath != null)
            Console.WriteLine("Found: " + Path.GetFullPath(foundPath));
        else
            Console.WriteLine("Not found.");
    }

    static string? FindFolder(string startDir, string inputFolder, string rootFolder)
    {
        DirectoryInfo? dir = new DirectoryInfo(startDir);
        
        // Step 1: Traverse up until we find the "root" folder or reach the top
        while (dir != null)
        {
            if (dir.Name.Equals(rootFolder, StringComparison.OrdinalIgnoreCase))
            {
                return null; // Stop searching if "root" is found
            }

            // Step 2: Check if the input folder exists in the current directory
            string potentialMatch = Path.Combine(dir.FullName, inputFolder);
            if (Directory.Exists(potentialMatch))
            {
                return Path.GetFullPath(potentialMatch);
            }

            dir = dir.Parent;
        }
        
        return null; // Not found
    }
}
