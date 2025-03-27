using System;
using System.IO;
using System.Linq;

class Program
{
    static void Main()
    {
        string[] inputFilePaths = 
        {
            @"C:\Temp\test1.txt", 
            @"C:\Temp\Example.TXT", 
            @"C:\Temp\NoFile.txt"
        };

        CheckFileNamesCase(inputFilePaths);
    }

    static void CheckFileNamesCase(string[] inputFilePaths)
    {
        if (inputFilePaths.Length == 0)
        {
            Console.WriteLine("No input files provided.");
            return;
        }

        // Group input files by directory
        var filesByDirectory = inputFilePaths
            .GroupBy(Path.GetDirectoryName)
            .Where(g => g.Key != null)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var kvp in filesByDirectory)
        {
            string directory = kvp.Key;
            var inputFiles = kvp.Value;

            if (!Directory.Exists(directory))
            {
                Console.WriteLine($"Directory not found: {directory}");
                continue;
            }

            // Get all actual file names in the directory
            var actualFiles = Directory.GetFiles(directory).Select(Path.GetFileName).ToList();

            foreach (string inputFilePath in inputFiles)
            {
                string inputFileName = Path.GetFileName(inputFilePath);

                // Find a case-insensitive match
                string actualFileName = actualFiles.FirstOrDefault(f => f.Equals(inputFileName, StringComparison.OrdinalIgnoreCase));

                if (actualFileName == null)
                {
                    Console.WriteLine($"❌ File not found: {inputFileName} in {directory}");
                    continue;
                }

                if (!actualFileName.Equals(inputFileName, StringComparison.Ordinal))
                {
                    Console.WriteLine($"⚠️ Warning: File exists but with different case! Expected: {inputFileName}, Found: {actualFileName}");
                }
                else
                {
                    Console.WriteLine($"✅ File name matches exactly: {inputFileName}");
                }
            }
        }
    }
}
