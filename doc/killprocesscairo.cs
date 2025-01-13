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
