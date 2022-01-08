using System.IO;
using System.Threading.Tasks;
using Melody.Utilities;

namespace Melody
{
	public static class Program
	{
		private static async Task Main(string[] args)
		{
			var configuration = await MelodyConfigurationLoader.LoadConfigurationAsync(new FileInfo("config.json"));
			await new Melody(configuration).StartAsync();
		}
	}
}