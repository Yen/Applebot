using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Applebot
{

    // Likely a temporary solution to be replace with Microsoft.Extensions.DependencyInjection configurations
    static class ConfigurationResolver
    {
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
            var json = await File.ReadAllTextAsync($"Configurations/{configurationName}.json");
            return JsonConvert.DeserializeObject<TConfig>(json);
        }
    }

}