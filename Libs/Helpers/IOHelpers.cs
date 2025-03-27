using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}