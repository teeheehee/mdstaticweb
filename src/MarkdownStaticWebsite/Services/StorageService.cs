
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

        public static void PrepareDatabase()
        {
            WebsiteData.PrepareDatabase(ConfigurationService.GetConfigurationService().Configuration.DbFile);
            //WebsiteData.PrepareDatabase(Configuration.GetConfiguration().DbFile);
        }
    }
}
