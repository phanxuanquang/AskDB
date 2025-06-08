using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace AskDB.App.Helpers
{
    public static class FileHelper
    {
        public static async Task<string> ReadFileAsync(string relativePath)
        {
            try
            {
                StorageFolder installedLocation = Windows.ApplicationModel.Package.Current.InstalledLocation;

                string folderPath = Path.GetDirectoryName(relativePath);
                string fileName = Path.GetFileName(relativePath);

                StorageFolder subFolder = installedLocation;
                if (!string.IsNullOrEmpty(folderPath))
                {
                    subFolder = await installedLocation.GetFolderAsync(folderPath);
                }

                StorageFile file = await subFolder.GetFileAsync(fileName);
                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                throw new FileNotFoundException(ex.Message, ex.InnerException);
            }
        }
    }
}
