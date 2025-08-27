// Helpers/ProcessHelper.cs
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FocusMate.Helpers
{
    public static class ProcessHelper
    {
        public static bool IsProcessRunning(string processName)
        {
            return Process.GetProcesses()
                .Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }

        public static Process[] GetProcessesByName(string processName)
        {
            return Process.GetProcesses()
                .Where(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        public static async Task<bool> KillProcessAsync(string processName, int timeoutMs = 5000)
        {
            var processes = GetProcessesByName(processName);
            if (processes.Length == 0) return false;

            var success = true;

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    // Wait for process to exit
                    await Task.Run(() => process.WaitForExit(timeoutMs));

                    if (!process.HasExited)
                    {
                        success = false;
                    }
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is NotSupportedException || ex is System.ComponentModel.Win32Exception)
                {
                    // Log the error if needed
                    success = false;
                }
            }

            return success;
        }

        public static async Task<bool> KillProcessAsync(int processId, int timeoutMs = 5000)
        {
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                await Task.Run(() => process.WaitForExit(timeoutMs));
                return process.HasExited;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException || ex is NotSupportedException || ex is System.ComponentModel.Win32Exception)
            {
                // Process already exited or access denied
                return false;
            }
        }

        public static ProcessInfo[] GetRunningProcesses()
        {
            return Process.GetProcesses()
                .Select(p => new ProcessInfo
                {
                    Id = p.Id,
                    Name = p.ProcessName,
                    MainWindowTitle = p.MainWindowTitle
                })
                .Where(p => !string.IsNullOrEmpty(p.Name))
                .OrderBy(p => p.Name)
                .ToArray();
        }
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string MainWindowTitle { get; set; }
    }
}