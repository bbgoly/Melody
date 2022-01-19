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

		/* TODO: If only one item exists and queue is not empty (i.e. a track is currently playing), add it to the description
		 * if more than one item exists add them to fields?
		 * if playlist loaded, show how many tracks from playlist added [playlistName] (...numTracks)
		 * Added Track(s) to Queue vs Track(s) Added to Queue
		 * idfk man
		 */
		public async Task AddTracksAsync(CommandContext ctx, MelodySearchItem[] selectedItems)
		{
			var queuedTracks = ctx.BuildDefaultEmbedComponent()
				.WithTitle("Track(s) Added to Queue")
				.WithThumbnail(selectedItems[0].DefaultThumbnail);
			Console.WriteLine("constructed");
			MelodyTrack[] tracks = new MelodyTrack[selectedItems.Length];
			for (int i = 0; i < selectedItems.Length; i++)
			{
				var item = selectedItems[i];
				LavalinkLoadResult lavalinkResult = await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(new Uri(item.ItemUrl));
				Console.WriteLine("found track(s)");
				Console.WriteLine(lavalinkResult.LoadResultType);
				Console.WriteLine(lavalinkResult.Tracks.First());
				tracks[i] = lavalinkResult.LoadResultType switch
				{
					LavalinkLoadResultType.PlaylistLoaded => new MelodyTrack
					{
						Track = lavalinkResult.Tracks.ElementAt(lavalinkResult.PlaylistInfo.SelectedTrack),
						LavalinkResultType = lavalinkResult.LoadResultType,
						DefaultThumbnail = item.DefaultThumbnail,
						SourceProvider = item.SourceProvider,
						RequestingMember = ctx.Member,
						Playlist = new MelodyPlaylist
						{
							PlaylistUrl = item.ItemUrl, PlaylistTracks = lavalinkResult.Tracks.ToArray()
						},
						TrackUrl = item.ItemUrl
					},
					LavalinkLoadResultType.TrackLoaded => new MelodyTrack
					{
						Track = lavalinkResult.Tracks.First(),
						LavalinkResultType = lavalinkResult.LoadResultType,
						DefaultThumbnail = item.DefaultThumbnail,
						SourceProvider = item.SourceProvider,
						RequestingMember = ctx.Member,
						TrackUrl = item.ItemUrl
					},
					_ => throw new ArgumentOutOfRangeException()
				};
				queuedTracks.AddField($"{i + 1}. {Formatter.Bold(item.Title)}", $"by {Formatter.Bold(item.Author)} on **[{item.SourceProvider.ToString()}]({item.ItemUrl} \"{item.ItemUrl}\")**");
				Console.WriteLine("added field for " + i);
			}
			await ctx.Channel.SendMessageAsync(queuedTracks);
			
			// TODO: Make playlists work
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			await guildSession.AddTracksAsync(tracks);
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