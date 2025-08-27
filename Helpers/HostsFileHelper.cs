// Helpers/HostsFileHelper.cs
using System;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace FocusMate.Helpers
{
    public static class HostsFileHelper
    {
        private static readonly string HostsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "drivers", "etc", "hosts");

        public static async Task<bool> BlockSitesAsync(string[] domains, string backupPath = null)
        {
            if (!HasAdminPrivileges())
            {
                throw new SecurityException("Administrator privileges are required to modify the hosts file.");
            }

            // Create backup if requested
            if (!string.IsNullOrEmpty(backupPath))
            {
                await CreateBackupAsync(backupPath);
            }

            try
            {
                var lines = (await File.ReadAllLinesAsync(HostsFilePath)).ToList();

                // Remove existing block entries for these domains
                lines.RemoveAll(line =>
                    !line.TrimStart().StartsWith("#") &&
                    domains.Any(domain => line.Contains(domain)));

                // Add new block entries
                foreach (var domain in domains)
                {
                    lines.Add($"127.0.0.1\t{domain}");
                    lines.Add($"::1\t\t{domain}");
                }

                // Write updated content
                await File.WriteAllLinesAsync(HostsFilePath, lines, Encoding.UTF8);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log error
                return false;
            }
        }

        public static async Task<bool> UnblockSitesAsync(string[] domains = null)
        {
            if (!HasAdminPrivileges())
            {
                throw new SecurityException("Administrator privileges are required to modify the hosts file.");
            }

            try
            {
                var lines = (await File.ReadAllLinesAsync(HostsFilePath)).ToList();

                if (domains == null || domains.Length == 0)
                {
                    // Remove all FocusMate-added entries
                    lines.RemoveAll(line =>
                        line.Contains("# FocusMate") ||
                        (line.Contains("127.0.0.1") && !line.TrimStart().StartsWith("#")));
                }
                else
                {
                    // Remove only specified domains
                    lines.RemoveAll(line =>
                        !line.TrimStart().StartsWith("#") &&
                        domains.Any(domain => line.Contains(domain)));
                }

                await File.WriteAllLinesAsync(HostsFilePath, lines, Encoding.UTF8);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log error
                return false;
            }
        }

        public static async Task<bool> CreateBackupAsync(string backupPath)
        {
            try
            {
                var content = await File.ReadAllTextAsync(HostsFilePath);
                await File.WriteAllTextAsync(backupPath, content);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log error
                return false;
            }
        }

        public static async Task<bool> RestoreBackupAsync(string backupPath)
        {
            if (!HasAdminPrivileges())
            {
                throw new SecurityException("Administrator privileges are required to modify the hosts file.");
            }

            try
            {
                if (!File.Exists(backupPath))
                {
                    return false;
                }

                var content = await File.ReadAllTextAsync(backupPath);
                await File.WriteAllTextAsync(HostsFilePath, content);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Log error
                return false;
            }
        }

        public static bool HasAdminPrivileges()
        {
            try
            {
                // Try to access a protected resource
                using (var fs = File.Open(HostsFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    return true;
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}