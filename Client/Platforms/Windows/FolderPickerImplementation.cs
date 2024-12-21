using Client.Interfaces;
using Windows.Storage.Pickers;
using System.Threading.Tasks;

[assembly: Dependency(typeof(Client.Platforms.Windows.FolderPickerImplementation))]
namespace Client.Platforms.Windows
{
    public class FolderPickerImplementation : IFolderPicker
    {
        public async Task<string> PickFolderAsync(string initialDirectory)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };

            var folder = await picker.PickSingleFolderAsync();

            return folder?.Path;
        }
    }
}
