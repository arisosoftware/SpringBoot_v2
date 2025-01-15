using System;
using System.Diagnostics;
using System.Linq;

public class ProcessKiller
{
    
    public static void KillProcessesContainingCairo()
    {
        try
        {
            // Get the current process ID
            int currentProcessId = Process.GetCurrentProcess().Id;

            // Get all processes running on the current user
            var allProcesses = Process.GetProcesses();

            foreach (var process in allProcesses)
            {
                try
                {
                    // Exclude the current process and check if the process name contains "CAIRO"
                    if (process.Id != currentProcessId && process.ProcessName.IndexOf("CAIRO", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Kill the process
                        process.Kill();
                        Console.WriteLine($"Killed process: {process.ProcessName} (PID: {process.Id})");
                    }
                }
                catch (Exception ex)
                {
                    // Handle any access permission issues or other errors
                    Console.WriteLine($"Error with process {process.ProcessName} (PID: {process.Id}): {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while killing processes: {ex.Message}");
        }
    }
}


using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;

public class FileDeleter
{
    public static void ForceDelete(string filepath)
    {
        try
        {
            // First, attempt to delete the file
            DeleteFile(filepath);
        }
        catch (IOException ex)
        {
            // If the file is being used by another process, catch the exception
            Console.WriteLine($"Error: {ex.Message}");

            // Find the process locking the file
            var processInfo = GetFileLockingProcess(filepath);

            if (processInfo != null)
            {
                // Kill the process (be careful with this step, it could terminate a critical process)
                KillProcess(processInfo.ProcessId);

                // Retry deletion after process termination
                Thread.Sleep(1000); // Wait for a moment to ensure process has terminated
                DeleteFile(filepath);
            }
            else
            {
                Console.WriteLine("No process found locking the file.");
            }
        }
    }

    private static void DeleteFile(string filepath)
    {
        if (File.Exists(filepath))
        {
            File.Delete(filepath);
            Console.WriteLine($"File {filepath} deleted successfully.");
        }
        else
        {
            Console.WriteLine("File does not exist.");
        }
    }

    private static (int ProcessId, string ProcessName)? GetFileLockingProcess(string filepath)
    {
        try
        {
            var query = new ManagementObjectSearcher(
                $"SELECT * FROM Win32_Process WHERE Name = 'explorer.exe'");

            foreach (var process in query.Get())
            {
                string executablePath = process["ExecutablePath"]?.ToString();
                if (executablePath != null && executablePath.Contains(filepath))
                {
                    int processId = Convert.ToInt32(process["ProcessId"]);
                    string processName = process["Name"]?.ToString();
                    return (processId, processName);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetFileLockingProcess: {ex.Message}");
        }

        return null;
    }

    private static void KillProcess(int processId)
    {
        try
        {
            Process process = Process.GetProcessById(processId);
            Console.WriteLine($"Terminating process {process.ProcessName} with ID {processId}");
            process.Kill();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error killing process: {ex.Message}");
        }
    }
}
