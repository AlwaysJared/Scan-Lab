using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Libs.Helpers
{
    public static class IOHelpers
    {
        public static Task MoveFileAsync(string source, string destination)
        {
            return Task.Run(() => File.Move(source, destination));
        }

        public static Task DeleteDirAsync(string dir)
        {
            return Task.Run(() => Directory.Delete(dir));
        }

        public static class NetworkPathConverter
        {
            /// <summary>
            /// Converts a Linux GVFS network path (e.g., AFP or SMB) to a Windows UNC path.
            /// </summary>
            /// <param name="linuxPath">The Linux GVFS or mounted path.</param>
            /// <returns>A UNC path if recognizable; otherwise returns null.</returns>

            public static string? ResolvePath(string inputPath)
            {
                if (string.IsNullOrWhiteSpace(inputPath))
                    return null;

                inputPath = inputPath.Trim();

                if (IsValidPath(inputPath, out var resolved)) return resolved;
                if (TryWindowsUncConversions(inputPath, out resolved)) return resolved;
                if (TryGvfsSmbConversion(inputPath, out resolved)) return resolved;
                if (TryGvfsAfpConversion(inputPath, out resolved)) return resolved;
                if (TryMntConversion(inputPath, out resolved)) return resolved;
                if (TryNetConversion(inputPath, out resolved)) return resolved;
                if (TryUncToUnixConversions(inputPath, out resolved)) return resolved;
                if (TryAdminShareFallback(inputPath, out resolved)) return resolved;

                return null;
            }

            private static bool IsValidPath(string path, out string? resolved)
            {
                resolved = null;
                try
                {
                    if (File.Exists(path) || Directory.Exists(path))
                    {
                        resolved = Path.GetFullPath(path);
                        return true;
                    }
                }
                catch { }

                return false;
            }

            private static bool TryWindowsUncConversions(string path, out string? resolved)
            {
                resolved = null;
                if (path.Contains('/'))
                {
                    var converted = path.Replace('/', '\\');
                    return IsValidPath(converted, out resolved);
                }

                return false;
            }

            private static bool TryGvfsSmbConversion(string path, out string? resolved)
            {
                resolved = null;
                var match = Regex.Match(path, @"smb-share:server=([^,]+),share=([^/]+)(/.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
                    var converted = $@"\\{server}\{share}{subPath}";

                    return IsValidPath(converted, out resolved);
                }

                return false;
            }

            private static bool TryGvfsAfpConversion(string path, out string? resolved)
            {
                resolved = null;

                // Extract full volume path from key=value pairs
                var match = Regex.Match(path, @"afp-volume:host=([^,]+),[^:]*volume=([^/]+(?:/[^/]+)*)/?");
                if (match.Success)
                {
                    var host = match.Groups[1].Value;
                    var volumePath = match.Groups[2].Value;

                    // Convert to Windows-style UNC
                    var fullPath = $@"\\{host}\{volumePath.Replace('/', '\\')}";
                    if (IsValidPath(fullPath, out resolved))
                        return true;

                    // Try stripping `.local` from host
                    if (host.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
                    {
                        var altHost = host[..^6];
                        fullPath = $@"\\{altHost}\{volumePath.Replace('/', '\\')}";
                        return IsValidPath(fullPath, out resolved);
                    }
                }

                return false;
            }

            private static bool TryMntConversion(string path, out string? resolved)
            {
                resolved = null;
                var match = Regex.Match(path.Replace('\\', '/'), @"^/mnt/([^/]+)/([^/]+)(/.*)?");
                if (match.Success)
                {
                    var host = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
                    var fullPath = $@"\\{host}\{share}{subPath}";

                    return IsValidPath(fullPath, out resolved);
                }

                return false;
            }

            private static bool TryNetConversion(string path, out string? resolved)
            {
                resolved = null;
                var match = Regex.Match(path.Replace('\\', '/'), @"^/net/([^/]+)(/.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var subPath = match.Groups[2].Success ? match.Groups[2].Value.Replace('/', '\\') : "";
                    var fullPath = $@"\\{server}{subPath}";

                    return IsValidPath(fullPath, out resolved);
                }

                return false;
            }

            private static bool TryUncToUnixConversions(string path, out string? resolved)
            {
                resolved = null;
                var match = Regex.Match(path, @"^\\\\([^\\]+)\\([^\\]+)(\\.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('\\', '/') : "";

                    var mntPath = $@"/mnt/{server}/{share}{subPath}";
                    if (IsValidPath(mntPath, out resolved))
                        return true;

                    var netPath = $@"/net/{server}/{share}{subPath}";
                    return IsValidPath(netPath, out resolved);
                }

                return false;
            }

            private static bool TryAdminShareFallback(string path, out string? resolved)
            {
                resolved = null;

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return false;

                if (!Regex.IsMatch(path, @"^[A-Za-z]:\\"))
                    return false;

                var drive = path.Substring(0, 2).ToUpperInvariant();
                var subPath = path.Substring(2).TrimStart('\\');
                var uncPath = $@"\\localhost\{drive[0]}$\{subPath}";

                return IsValidPath(uncPath, out resolved);
            }

            // public static string? ConvertToUncPath(string linuxPath)
            // {
            //     if (string.IsNullOrWhiteSpace(linuxPath))
            //         return null;

            //     var handlers = new List<Func<string, string?>>
            //     {
            //         TryConvertGvfsAfp,
            //         TryConvertGvfsSmb,
            //         TryConvertMountSmb,
            //         TryConvertMountNfs
            //         // Add more handlers here
            //     };

            //     foreach (var handler in handlers)
            //     {
            //         var unc = handler(linuxPath);
            //         if (!string.IsNullOrEmpty(unc))
            //             return unc;
            //     }

            //     return null;
            // }

            // private static string? TryConvertGvfsAfp(string path)
            // {
            //     // Example: /run/user/1000/gvfs/afp-volume:host=server.local,user=someuser/share/folder
            //     var match = Regex.Match(path, @"gvfs/afp-volume:host=([^,]+)(?:,[^/]*)*/([^/]+)(/.*)?");
            //     if (match.Success)
            //     {
            //         var host = match.Groups[1].Value;
            //         var share = match.Groups[2].Value;
            //         var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
            //         return $@"\\{host}\{share}{subPath}";
            //     }
            //     return null;
            // }

            // private static string? TryConvertGvfsSmb(string path)
            // {
            //     // Example: /run/user/1000/gvfs/smb-share:server=server.local,share=sharename/folder
            //     var match = Regex.Match(path, @"gvfs/smb-share:server=([^,]+),share=([^/]+)(/.*)?");
            //     if (match.Success)
            //     {
            //         var server = match.Groups[1].Value;
            //         var share = match.Groups[2].Value;
            //         var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
            //         return $@"\\{server}\{share}{subPath}";
            //     }
            //     return null;
            // }

            // private static string? TryConvertMountSmb(string path)
            // {
            //     // Example: /mnt/server/sharename/folder
            //     var match = Regex.Match(path, @"/mnt/([^/]+)/([^/]+)(/.*)?");
            //     if (match.Success)
            //     {
            //         var server = match.Groups[1].Value;
            //         var share = match.Groups[2].Value;
            //         var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
            //         return $@"\\{server}\{share}{subPath}";
            //     }
            //     return null;
            // }

            // private static string? TryConvertMountNfs(string path)
            // {
            //     // Example: /net/server/exported/path
            //     var match = Regex.Match(path, @"/net/([^/]+)(/.*)?");
            //     if (match.Success)
            //     {
            //         var server = match.Groups[1].Value;
            //         var exportPath = match.Groups[2].Success ? match.Groups[2].Value.Replace('/', '\\').TrimStart('\\') : "";
            //         return $@"\\{server}\{exportPath}";
            //     }
            //     return null;
            // }

        }

    }
}