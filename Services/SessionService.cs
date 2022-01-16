using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Melody.Data;
using Melody.Data.Enums;
using Melody.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Melody.Services
{
	public sealed class SessionService
	{
		private DiscordClient Discord { get; }
		private LavalinkService LavalinkService { get; }
		private RedisClientService RedisService { get; }
		private IDbContextFactory<PostgresClientService> PostgresService { get; }

		private GuildSession CachedGuildSession { get; set; }
		private ConcurrentDictionary<ulong, GuildSession> GuildSessions { get; }

		public SessionService(DiscordClient client, LavalinkService lavalink, RedisClientService redis, IDbContextFactory<PostgresClientService> postgres)
		{
			this.Discord = client;
			this.RedisService = redis;
			this.LavalinkService = lavalink;
			this.PostgresService = postgres;
			this.GuildSessions = new ConcurrentDictionary<ulong, GuildSession>();
		}
		
		private GuildSession GetOrCreateGuildSession(DiscordChannel channel)
		{
			ulong id = channel.Guild.Id;
			GuildSession guildSession = this.CachedGuildSession; //probably remove cachedguildsession and just use getoradd if its cheap enough cuz not thread-safe and bad
			if (guildSession is null || guildSession.GuildId != id)
			{
				guildSession = this.GuildSessions.GetOrAdd(id, new GuildSession(id, this.LavalinkService));
				this.CachedGuildSession = guildSession;
			}
			guildSession.SessionInfo.CommandChannel = channel;
			return guildSession;
		}

		public async Task ConnectPlayerAsync(CommandContext ctx)
		{
			await this.GetOrCreateGuildSession(ctx.Channel).ConnectPlayerAsync(ctx.Member.VoiceState.Channel);
			await ctx.Guild.CurrentMember.SetDeafAsync(true);
		}

		public async Task DisconnectPlayerAsync(DiscordGuild guild)
		{
			if (this.GuildSessions.TryRemove(guild.Id, out GuildSession guildSession))
			{
				this.CachedGuildSession = null;
				await guildSession.DisconnectPlayerAsync();
			}
		}

		public async Task AddTracksAsync(CommandContext ctx, MelodySearchItem responseItem)
		{
			if (responseItem.SourceProvider is MelodySearchProvider.Spotify)
			{
				await ctx.Channel.SendDefaultEmbedMessageAsync("Finding equivalent Spotify tracks on YouTube...");
				Console.WriteLine("bruh.");
				Uri odesliUri = new Uri("https://api.song.link/v1-alpha.1/links?url=" + Uri.EscapeDataString(responseItem.ItemUrl) + "&platform=youtube&type=song");
				HttpWebRequest request = WebRequest.CreateHttp(odesliUri);
				request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				Console.WriteLine("bruh2.");
				
				using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
				Console.WriteLine("res");
				await using Stream responseStream = response.GetResponseStream();
				Console.WriteLine("st");
				using StreamReader streamReader = new StreamReader(responseStream);
				Console.WriteLine("read");
				string test = await streamReader.ReadToEndAsync();
				Console.WriteLine(test);
				await ctx.Channel.SendDefaultEmbedMessageAsync(test);
			}

			LavalinkLoadResult lavalinkResult = await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(responseItem.ItemUrl);
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			await guildSession.AddTracks(new []{lavalinkResult.Tracks.First()}); // TODO: fix this shit
			if (lavalinkResult.LoadResultType == LavalinkLoadResultType.PlaylistLoaded)
			{
				
			}
		}

		public async Task PauseAsync(DiscordChannel commandChannel)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(commandChannel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
				await guildSession.PauseAsync();
		}
	
		public async Task ResumeAsync(DiscordChannel commandChannel)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(commandChannel);
			if (!guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
				await guildSession.ResumeAsync();
		}

		public async Task SkipTrackAsync(DiscordChannel commandChannel)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(commandChannel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
				await guildSession.SkipAsync();
		}
	}
}