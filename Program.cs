using System;
using System.IO;
using System.Threading.Tasks;
using Melody.Utilities;

namespace Melody
{
	public static class Program
	{
		private static async Task Main(string[] args)
		{
			bool runningInDockerContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
			var fileInfo = new FileInfo(runningInDockerContainer ? "config.docker.json" : "config.json");
			var configuration = await MelodyConfigurationLoader.LoadConfigurationAsync(fileInfo);
			await new Melody(configuration).StartAsync();
		}
	}
}