using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using Melody.Utilities;
using Microsoft.Extensions.Logging;

namespace Melody.Services
{
	public class LavalinkService
	{
		private DiscordClient Discord { get; }
		private MelodyLavalinkConfiguration Configuration { get; }
		public LavalinkNodeConnection LavalinkNode { get; private set; }

		public LavalinkService(DiscordClient client, MelodyLavalinkConfiguration config)
		{
			this.Discord = client;
			this.Configuration = config;
			this.Discord.Ready += Discord_Ready;
		}

		private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
		{
			Task.Run(async () =>
			{
				this.LavalinkNode = await sender.GetLavalink().ConnectAsync(new LavalinkConfiguration
				{
					Password = this.Configuration.Password,
					SocketEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port),
					RestEndpoint = new ConnectionEndpoint(this.Configuration.Hostname, this.Configuration.Port)
				});
				sender.Logger.LogInformation($"[{sender.CurrentUser.Username}] [Lavalink Connected] - Connected to {this.Configuration.Hostname}:{this.Configuration.Port}");
			});
			sender.Logger.LogInformation($"[{sender.CurrentUser.Username}] [Compiled Successfully] - Ready to process subscribed events");
			return Task.CompletedTask;
		}
	}
}