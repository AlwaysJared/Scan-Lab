using Client.Interfaces;
using System.Threading.Tasks;

[assembly: Dependency(typeof(Client.Platforms.iOS.FolderPickerImplementation))]
namespace Client.Platforms.iOS
{
    public class FolderPickerImplementation : IFolderPicker
    {
        public async Task<string> PickFolderAsync(string initialDirectory)
        {
            // iOS-specific folder picker logic (note that iOS is more restrictive about folders)
            return initialDirectory;
        }
    }
}
