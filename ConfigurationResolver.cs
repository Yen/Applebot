using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Applebot
{

    // Likely a temporary solution to be replace with Microsoft.Extensions.DependencyInjection configurations
    static class ConfigurationResolver
    {
        private static DirectoryInfo _ConfigurationsDirectory;
        public static DirectoryInfo ConfigurationsDirectory
        {
            get => _ConfigurationsDirectory ?? throw new InvalidOperationException("Cannot load configuration files until configurations directory has been set by the application");
            set => _ConfigurationsDirectory = value;
        }

        /// <summary>
        /// Loads a configuration file based on <typeparamref name="TService"/> full name.
        /// </summary>
        public static async Task<TConfig> LoadConfigurationAsync<TService, TConfig>()
        {
            return await LoadConfigurationAsync<TConfig>(typeof(TService).FullName);
        }

        /// <summary>
        /// Loads a configuration file based the configuration name.
        /// <c>TestConfig -> Configurations/TestConfig.json</c>
        /// </summary>
        public static async Task<TConfig> LoadConfigurationAsync<TConfig>(string configurationName)
        {
            var path = Path.Combine(ConfigurationsDirectory.FullName, $"{configurationName}.json");
            var json = await File.ReadAllTextAsync(path);
            return JsonConvert.DeserializeObject<TConfig>(json);
        }
    }

}