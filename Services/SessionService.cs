using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using ConcurrentCollections;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using Melody.Data;
using Melody.Data.Enums;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

#region Postgres Test Data
// _ = Task.Run(async () =>
// {
// 	await using PostgresClientService postgres = this.PostgresService.CreateDbContext();
// 	await postgres.Database.EnsureDeletedAsync();
// 	await postgres.Database.EnsureCreatedAsync();
// 	await postgres.MelodyUsers.AddAsync(new MelodyUser
// 	{
// 		UserId = e.User.Id,
// 		Playlists = new List<UserPlaylist>
// 		{
// 			new()
// 			{
// 				Title = "Favorite songs",
// 				Tracks = new List<PlaylistTrack>
// 				{
// 					new()
// 					{
// 						TrackUrl = "https://www.youtube.com/watch?v=EjkGGMxyxiA"
// 					},
// 					new()
// 					{
// 						TrackUrl = "https://www.youtube.com/watch?v=TzO0pNBNSjs"
// 					},
// 					new()
// 					{
// 						TrackUrl = "https://www.youtube.com/watch?v=f_RtcheIvcg"
// 					}
// 				}
// 			}
// 		}
// 	});
// 	await postgres.SaveChangesAsync();
// 	Console.WriteLine("lol");
// 	Console.WriteLine(postgres.UserPlaylists.Where(x => x.UserId == e.User.Id));
// });
// return Task.CompletedTask;
#endregion

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
			this.Discord.VoiceStateUpdated += Discord_VoiceStateUpdated;
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

		private async Task<LavalinkLoadResult> GetRestTracksAsync(string trackUrl) =>
			await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(new Uri(trackUrl));
		
		private async Task<LavalinkLoadResult> GetRestTracksAsync(string searchTerms, LavalinkSearchType searchType) =>
			await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(searchTerms, searchType);
		
		private Task Discord_VoiceStateUpdated(DiscordClient sender, VoiceStateUpdateEventArgs e)
		{
			if (e.User.IsBot) return Task.CompletedTask;
			if (e.Before is null)
			{
				Task.Run(async () =>
				{
					await using var postgres = this.PostgresService.CreateDbContext();
					foreach (var playlist in postgres.UserPlaylists.Where(playlist => playlist.UserId == e.User.Id).Select(playlist => new { playlist.Title, playlist.UserId, playlist.Tracks }))
					{
						Console.WriteLine("lol");
						Console.WriteLine(playlist.UserId);
						Console.WriteLine(playlist.Title);
						Console.WriteLine(string.Join(", ", playlist.Tracks.Select(track => track.TrackUrl)));
						await this.RedisService.AddSetMember($"{playlist.UserId}:{playlist.Title}", playlist.Tracks.Select(track => track.TrackUrl).ToArray(), CommandFlags.FireAndForget);
					}
				});
			}
			else if (e.After is null)
			{
				// TODO: Remove from redis and update postgres
				Console.WriteLine("lol");
			}
			return Task.CompletedTask;
		}

		public async Task ConnectPlayerAsync(CommandContext ctx)
		{
			await this.GetOrCreateGuildSession(ctx.Channel).ConnectPlayerAsync(ctx.Member.VoiceState.Channel);
			await ctx.Guild.CurrentMember.SetDeafAsync(true);
		}

		public async Task DisconnectPlayerAsync(CommandContext ctx)
		{
			DiscordVoiceState voiceState = ctx.Guild.CurrentMember?.VoiceState;
			if (voiceState is not null && this.GuildSessions.TryRemove(ctx.Guild.Id, out GuildSession guildPlayer))
			{
				await guildPlayer.DisconnectPlayerAsync();
				await ctx.RespondAsync($"Disconnected the player from {voiceState.Channel.Mention} {DiscordEmoji.FromName(ctx.Client, ":stop_button:")}");
			}
		}

		public async Task<bool> AddTracks(CommandContext ctx, MelodySearchItem responseItem)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			int initialQueueCount = guildSession.SessionInfo.SessionQueue.Count;
			switch (responseItem.SourceProvider)
			{
				case MelodySearchProvider.YouTube:
					LavalinkLoadResult lavalinkResult = await this.GetRestTracksAsync(responseItem.ItemUrl);
					await guildSession.AddTracks(lavalinkResult.Tracks);
					break;
				case MelodySearchProvider.Spotify:
					break;
				case MelodySearchProvider.SoundCloud:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(responseItem.SourceProvider), responseItem.SourceProvider, null);
			}
			return initialQueueCount == 0;
		}

		public async Task PauseAsync(CommandContext ctx)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.PauseAsync();
				await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":pause_button:")} Paused the player!");
			}
		}
	
		public async Task ResumeAsync(CommandContext ctx)
		{
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			if (!guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.ResumeAsync();
				await ctx.RespondAsync($"{DiscordEmoji.FromName(ctx.Client, ":play_pause:")} Resumed the player!");
			}
		}

		// TODO: Have Melody be able to determine who added what to the queue
		public async Task SkipTrackAsync(CommandContext ctx)
		{
			await this.GetOrCreateGuildSession(ctx.Channel).SkipAsync();
			await ctx.RespondAsync("Skipped track!");
		}
	}
}