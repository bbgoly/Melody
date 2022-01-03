using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Melody.Utilities
{
	public static class MelodyConfigurationLoader
	{
		
		public static async Task<MelodyConfiguration> LoadConfigurationAsync(FileInfo file)
		{
			if (file is null || !file.Exists || file.Extension != ".json")
				throw new ArgumentException("Specified bot configuration file is invalid", nameof(file));

			await using var stream = file.OpenRead();
			return await JsonSerializer.DeserializeAsync<MelodyConfiguration>(stream);
		}

		public static async Task SaveConfigurationAsync(FileInfo file, MelodyConfiguration configuration)
		{
			if (file is null || file.Extension != ".json")
				throw new ArgumentException("Specified bot configuration file is invalid", nameof(file));

			if (configuration is null || configuration.GetType().GetProperties().Any(prop => prop.GetValue(configuration) is null))
				throw new ArgumentNullException(nameof(configuration), "Bot configuration data is null");

			await using var stream = file.Create();
			await JsonSerializer.SerializeAsync<MelodyConfiguration>(stream, configuration);
		}
	}
}