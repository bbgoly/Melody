using System;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Melody.Utilities;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Melody.Services
{
	public class RedisClientService
	{
		private DiscordClient Discord { get; }
		private MelodyRedisConfiguration Configuration { get; }
		private ConnectionMultiplexer RedisMultiplexer { get; set; }

		public IDatabase RedisCache => this.RedisMultiplexer.GetDatabase();
		
		public RedisClientService(DiscordClient client, MelodyRedisConfiguration config)
		{
			this.Discord = client;
			this.Configuration = config;
			this.Discord.Ready += Discord_Ready;
		}

		private Task Discord_Ready(DiscordClient sender, ReadyEventArgs e)
		{
			Task.Run(async () =>
			{
				this.RedisMultiplexer = await ConnectionMultiplexer.ConnectAsync(new ConfigurationOptions
				{
					ClientName = "Melody",
					Password = this.Configuration.Password,
					//Ssl = this.Configuration.UseEncryption,
					EndPoints = { new DnsEndPoint(this.Configuration.Hostname, this.Configuration.Port) }
				});
				sender.Logger.LogInformation($"[{sender.CurrentUser.Username}] [Redis Connected] - Connected to {this.Configuration.Hostname}:{this.Configuration.Port}");
			});
			return Task.CompletedTask;
		}
	}
}