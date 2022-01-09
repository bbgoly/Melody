using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Melody.Data;
using Melody.Data.Enums;
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
			GuildSession guildSession = this.CachedGuildSession;
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
			if (this.GuildSessions.TryRemove(guild.Id, out GuildSession guildPlayer))
				await guildPlayer.DisconnectPlayerAsync();
		}

		public async Task<bool> AddTracksAsync(CommandContext ctx, MelodySearchItem responseItem)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			LavalinkLoadResult lavalinkResult = await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(responseItem.ItemUrl);
			bool isQueueEmpty = guildSession.SessionInfo.SessionQueue.Count == 0;
			switch (responseItem.SourceProvider)
			{
				case MelodySearchProvider.YouTube:
					await guildSession.AddTracks(lavalinkResult.Tracks);
					break;
				case MelodySearchProvider.Spotify:
					break;
				case MelodySearchProvider.SoundCloud:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(responseItem.SourceProvider), responseItem.SourceProvider, null);
			}
			return isQueueEmpty;
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