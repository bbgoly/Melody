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

		public async Task<RedisValue[]> GetSetMembers(string key) =>
			await this.RedisMultiplexer.GetDatabase().SetMembersAsync(new RedisKey(key));

		public async Task AddSetMember(string key, string[] values, CommandFlags commandFlags = CommandFlags.None)
		{
			
			RedisValue[] redisValues = values.ToRedisValueArray();
			await this.RedisMultiplexer.GetDatabase().SetAddAsync(new RedisKey(key), redisValues, commandFlags);
		}

		public async Task RemoveSetMember(string key, string[] members)
		{
			RedisValue[] redisMembers = members.ToRedisValueArray();
			await this.RedisMultiplexer.GetDatabase().SetRemoveAsync(new RedisKey(key), redisMembers, CommandFlags.FireAndForget);
		}

		public async Task<RedisValue> GetHashValue(string key, string field) =>
			await this.RedisMultiplexer.GetDatabase().HashGetAsync(new RedisKey(key), new RedisValue(field));

		public async Task<RedisValue[]> GetHashValues(string key, string[] fields)
		{
			RedisValue[] redisFields = fields.ToRedisValueArray();
			return await this.RedisMultiplexer.GetDatabase().HashGetAsync(new RedisKey(key), redisFields);
		}

		public async Task<HashEntry[]> GetAllHashValues(string key) =>
			await this.RedisMultiplexer.GetDatabase().HashGetAllAsync(new RedisKey(key));

		public async Task SetHashFields(string key, HashEntry[] fields) =>
			await this.RedisMultiplexer.GetDatabase().HashSetAsync(new RedisKey(key), fields);

		public async Task RemoveHashFields(string key, string[] fields)
		{
			RedisValue[] redisFields = fields.ToRedisValueArray();
			await this.RedisMultiplexer.GetDatabase().HashDeleteAsync(new RedisKey(key), redisFields);
		}

		public async Task DeleteKey(string key) =>
			await this.RedisMultiplexer.GetDatabase().KeyDeleteAsync(new RedisKey(key), CommandFlags.FireAndForget);

		public async Task<bool> KeyExists(string key) =>
			await this.RedisMultiplexer.GetDatabase().KeyExistsAsync(new RedisKey(key));
	}
}