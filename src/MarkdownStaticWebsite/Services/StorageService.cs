
using MarkdownStaticWebsite.Repositories;

namespace MarkdownStaticWebsite.Services
{
    public class StorageService
    {
        private static StorageService? Instance { get; set; }

        //private SQLiteConnection Connection { get; set; }

        private StorageService() { }

        public static StorageService GetStorageService()
        {
            Instance ??= new StorageService();
            return Instance;
        }

        public void PrepareDatabase()
        {
            WebsiteData.PrepareDatabase(ConfigurationService.GetService().Configuration.DatabaseFile);
        }
    }
}
