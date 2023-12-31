using MarkdownStaticWebsite.Entities;
using System.Text.Json;

namespace MarkdownStaticWebsite.Services
{
    public class ConfigurationService
    {
        private static readonly string DefaultBasePath = AppContext.BaseDirectory ?? @"C:\dev\website\";
        private static readonly string DefaultConfigurationName = "defaultConfig.json";
        private static readonly string DefaultConfigurationFile = DefaultBasePath + @$"{DefaultConfigurationName}";

        public string ConfigurationFile { get; set; } = DefaultConfigurationFile;

        private static ConfigurationService? Instance { get; set; }
        public Configuration Configuration { get; set; }

        private ConfigurationService()
        {
            Configuration = GetConfiguration(ConfigurationFile);
        }

        private ConfigurationService(string fileFullPath, bool shouldCreateFile = false)
        {
            ConfigurationFile = fileFullPath;
            Configuration = GetConfiguration(ConfigurationFile, shouldCreateFile);
        }

        public static ConfigurationService GetConfigurationService()
        {
            Instance ??= new ConfigurationService();
            return Instance;
        }

        public static ConfigurationService GetConfigurationService(
            bool shouldCreateFile = false)
        {
            var fileFullPath = Instance?.ConfigurationFile ?? DefaultConfigurationFile;
            Instance = new ConfigurationService(fileFullPath, shouldCreateFile);
            return Instance;
        }

        public static ConfigurationService GetConfigurationService(
            string fileFullPath, bool shouldCreateFile = false)
        {
            Instance = new ConfigurationService(fileFullPath, shouldCreateFile);
            return Instance;
        }

        //private static Configuration GetDefaultConfiguration(bool shouldCreateFile = false)
        //{
        //    return GetConfiguration(DefaultConfigurationFile, shouldCreateFile);
        //}

        private static Configuration GetConfiguration(string fileFullPath, bool shouldCreateFile = false)
        {
            var fileExists = File.Exists(fileFullPath);

            if (shouldCreateFile && !fileExists)
            {
                return CreateDefaultConfigurationFile(fileFullPath);
            }

            if (fileExists)
            {
                return FromFile(fileFullPath);
            }

            return new Configuration();
        }

        //public static Configuration ToFile(string fileFullPath)
        //{
        //    return ToFile(fileFullPath, ConfigurationService.GetConfigurationService().Configuration);
        //}

        private static Configuration ToFile(string fileFullPath, Configuration configuration)
        {
            var json = ToJson(configuration);
            File.WriteAllText(fileFullPath, json);
            return configuration;
        }

        private static Configuration FromFile(string fileFullPath)
        {
            string json = File.ReadAllText(fileFullPath);
            return FromJson(json);
        }

        //private static string ToJson()
        //{
        //    return ToJson(ConfigurationService.GetConfigurationService().Configuration);
        //}

        private static string ToJson(Configuration configuration)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(configuration, options);
        }

        private static Configuration FromJson(string json)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var configuration = JsonSerializer.Deserialize<Configuration>(json, options);
            return configuration ?? new Configuration();
        }

        private static Configuration CreateDefaultConfigurationFile(string fileFullPath)
        {
            var defaultConfiguration = new Configuration();
            ToFile(fileFullPath, defaultConfiguration);
            return defaultConfiguration;
        }
    }
}
