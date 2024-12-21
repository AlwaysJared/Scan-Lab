namespace Client.Interfaces
{
    public interface IFolderPicker
    {
        Task<string> PickFolderAsync(string initialDirectory);
    }
}