// See https://aka.ms/new-console-template for more information

using MarkdownStaticWebsite.Entities;
using MarkdownStaticWebsite.Services;

Console.WriteLine("Hello, World!");

//Configuration.GetDefaultConfiguration();
//Console.WriteLine(Configuration.GetConfiguration().BasePath);
//Console.WriteLine(Configuration.GetConfiguration().DbFile);
//Console.WriteLine(Configuration.GetConfiguration().TemplatePath);
//Console.WriteLine(Configuration.GetConfiguration().SourcePath);
//Console.WriteLine(Configuration.GetConfiguration().BuildOutputPath);
//Console.WriteLine(Configuration.GetConfiguration().ConfigurationFile);

//Configuration.GetDefaultConfiguration(shouldCreateFile: true);
//Console.WriteLine(

//ConfigurationService.GetConfigurationService().

Console.WriteLine(ConfigurationService.GetConfigurationService().Configuration.BasePath);
Console.WriteLine(ConfigurationService.GetConfigurationService(true).Configuration.DbFile);
Console.WriteLine(ConfigurationService.GetConfigurationService().Configuration.TemplatePath);
Console.WriteLine(ConfigurationService.GetConfigurationService().Configuration.SourcePath);
Console.WriteLine(ConfigurationService.GetConfigurationService().Configuration.BuildOutputPath);
//Console.WriteLine(ConfigurationService.GetConfigurationService().Configuration.ConfigurationFile);
