using System;
using System.IO;
using System.Threading.Tasks;
using Emzi0767.Utilities;
using Melody.Utilities;

namespace Melody
{
	public static class Program
	{
		private static void Main(string[] args) => new AsyncExecutor().Execute(MainAsync());
        
		private static async Task MainAsync()
		{
			var configuration = await MelodyConfigurationLoader.LoadConfigurationAsync(new FileInfo("config.json"));
			await new Melody(configuration).StartAsync();
		}
	}
}