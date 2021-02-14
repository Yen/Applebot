using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Applebot
{

    // Likely a temporary solution to be replace with Microsoft.Extensions.DependencyInjection configurations
    static class ResourceResolver
    {
        private static DirectoryInfo _ConfigurationsDirectory;
        public static DirectoryInfo ConfigurationsDirectory
        {
            get => _ConfigurationsDirectory ?? throw new InvalidOperationException("Cannot load configuration files until configurations directory has been set by the application");
            set => _ConfigurationsDirectory = value;
        }

        private static DirectoryInfo _RuntimeDataDirectory;
        public static DirectoryInfo RuntimeDataDirectory
        {
            get => _RuntimeDataDirectory ?? throw new InvalidOperationException("Cannot load runtime data until runtime data directory has been set by the application");
            set => _RuntimeDataDirectory = value;
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

        /// <summary>
        /// Creates if needed and returns the directory for runtime data based on <typeparamref name="TService"/> full name.
        /// </summary>
        public static async Task<DirectoryInfo> GetRuntimeDataDirectoryAsync<TService>()
        {
            return await GetRuntimeDataDirectoryAsync(typeof(TService).FullName);
        }

        /// <summary>
        /// Creates if needed and returns the directory for runtime data based on the directory name.
        /// <c>SomeDir -> RuntimeData/SomeDir</c>
        /// </summary>
        public static async Task<DirectoryInfo> GetRuntimeDataDirectoryAsync(string directoryName)
        {
            var path = Path.Combine(RuntimeDataDirectory.FullName, directoryName);
            var info = new DirectoryInfo(path);
            await Task.Run(() => info.Create());
            return info;
        }
    }

}