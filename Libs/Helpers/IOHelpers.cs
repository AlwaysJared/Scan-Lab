using System;
using System.Collections.Generic;
using System.Linq;
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
            public static string? ConvertToUncPath(string linuxPath)
            {
                if (string.IsNullOrWhiteSpace(linuxPath))
                    return null;

                var handlers = new List<Func<string, string?>>
                {
                    TryConvertGvfsAfp,
                    TryConvertGvfsSmb,
                    TryConvertMountSmb,
                    TryConvertMountNfs
                    // Add more handlers here
                };

                foreach (var handler in handlers)
                {
                    var unc = handler(linuxPath);
                    if (!string.IsNullOrEmpty(unc))
                        return unc;
                }

                return null;
            }

            private static string? TryConvertGvfsAfp(string path)
            {
                // Example: /run/user/1000/gvfs/afp-volume:host=server.local,user=someuser/share/folder
                var match = Regex.Match(path, @"gvfs/afp-volume:host=([^,]+)(?:,[^/]*)*/([^/]+)(/.*)?");
                if (match.Success)
                {
                    var host = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
                    return $@"\\{host}\{share}{subPath}";
                }
                return null;
            }

            private static string? TryConvertGvfsSmb(string path)
            {
                // Example: /run/user/1000/gvfs/smb-share:server=server.local,share=sharename/folder
                var match = Regex.Match(path, @"gvfs/smb-share:server=([^,]+),share=([^/]+)(/.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
                    return $@"\\{server}\{share}{subPath}";
                }
                return null;
            }

            private static string? TryConvertMountSmb(string path)
            {
                // Example: /mnt/server/sharename/folder
                var match = Regex.Match(path, @"/mnt/([^/]+)/([^/]+)(/.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var share = match.Groups[2].Value;
                    var subPath = match.Groups[3].Success ? match.Groups[3].Value.Replace('/', '\\') : "";
                    return $@"\\{server}\{share}{subPath}";
                }
                return null;
            }

            private static string? TryConvertMountNfs(string path)
            {
                // Example: /net/server/exported/path
                var match = Regex.Match(path, @"/net/([^/]+)(/.*)?");
                if (match.Success)
                {
                    var server = match.Groups[1].Value;
                    var exportPath = match.Groups[2].Success ? match.Groups[2].Value.Replace('/', '\\').TrimStart('\\') : "";
                    return $@"\\{server}\{exportPath}";
                }
                return null;
            }
        }

    }
}