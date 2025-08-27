// Helpers/FirewallHelper.cs
using System;
using System.Diagnostics;
using System.Security;
using System.Threading.Tasks;

namespace FocusMate.Helpers
{
    public static class FirewallHelper
    {
        public static async Task<bool> CreateBlockRuleAsync(string ruleName, string applicationPath)
        {
            if (!HostsFileHelper.HasAdminPrivileges())
            {
                throw new SecurityException("Administrator privileges are required to modify firewall rules.");
            }

            try
            {
                // Use netsh to create a firewall rule
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=out program=\"{applicationPath}\" action=block",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return false;

                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                // Log error
                return false;
            }
        }

        public static async Task<bool> RemoveBlockRuleAsync(string ruleName)
        {
            if (!HostsFileHelper.HasAdminPrivileges())
            {
                throw new SecurityException("Administrator privileges are required to modify firewall rules.");
            }

            try
            {
                // Use netsh to remove a firewall rule
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    Verb = "runas", // Run as administrator
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return false;

                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                // Log error
                return false;
            }
        }

        public static async Task<bool> RuleExistsAsync(string ruleName)
        {
            try
            {
                // Use netsh to check if a rule exists
                var processInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{ruleName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(processInfo))
                {
                    if (process == null) return false;

                    var output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    return output.Contains(ruleName) && !output.Contains("No rules match");
                }
            }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception || ex is InvalidOperationException)
            {
                // Log error
                return false;
            }
        }
    }
}