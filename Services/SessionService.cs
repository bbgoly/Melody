using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Melody.Data;
using Melody.Utilities;
using Microsoft.EntityFrameworkCore;

namespace Melody.Services
{
	public sealed class SessionService
	{
		private DiscordClient Discord { get; }
		private LavalinkService LavalinkService { get; }
		private RedisClientService RedisService { get; }
		private ConcurrentDictionary<ulong, GuildSession> GuildSessions { get; }
		private IDbContextFactory<PostgresClientService> PostgresService { get; }

		public SessionService(DiscordClient client, LavalinkService lavalink, RedisClientService redis, IDbContextFactory<PostgresClientService> postgres)
		{
			this.Discord = client;
			this.RedisService = redis;
			this.LavalinkService = lavalink;
			this.PostgresService = postgres;
			this.GuildSessions = new ConcurrentDictionary<ulong, GuildSession>();
		}
		
		public GuildSession GetOrCreateGuildSession(DiscordChannel channel)
		{
			ulong guildId = channel.Guild.Id;
			GuildSession guildSession = this.GuildSessions.GetOrAdd(guildId, new GuildSession(guildId, this.LavalinkService));
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
			if (this.GuildSessions.TryRemove(guild.Id, out var guildSession))
				await guildSession.DisconnectPlayerAsync();
		}

		/* TODO: If only one item exists and queue is not empty (i.e. a track is currently playing), add it to the description
		 * if more than one item exists add them to fields?
		 * if playlist loaded, show how many tracks from playlist added [playlistName] (...numTracks)
		 * Added Track(s) to Queue vs Track(s) Added to Queue
		 * idfk man
		 */
		public async Task AddTracksAsync(CommandContext ctx, MelodySearchItem[] selectedItems)
		{
			Console.WriteLine("constructed");
			MelodyTrack[] tracks = new MelodyTrack[selectedItems.Length];
			for (int i = 0; i < selectedItems.Length; i++)
			{
				var item = selectedItems[i];
				LavalinkLoadResult lavalinkResult = await this.LavalinkService.LavalinkNode.Rest.GetTracksAsync(new Uri(item.ItemUrl));
				Console.WriteLine("found track(s)");
				Console.WriteLine(lavalinkResult.LoadResultType);
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
				Console.WriteLine("added field for " + i);
			}
			
			// TODO: Make playlists work
			GuildSession guildSession = this.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null || selectedItems.Length > 1)
			{
				var queuedTracks = ctx.BuildDefaultEmbedComponent().WithTitle("Track(s) Added to Queue").WithImageUrl(selectedItems[0].DefaultThumbnail);
				foreach (var melodyTrack in tracks)
					queuedTracks.AddField(Formatter.Bold(melodyTrack.Track.Title), $"by {Formatter.Bold(melodyTrack.Track.Author)} on **[{melodyTrack.SourceProvider.ToString()}]({melodyTrack.TrackUrl} \"{melodyTrack.TrackUrl}\")**");
				await ctx.Channel.SendMessageAsync(queuedTracks);
			}
			await guildSession.AddTracksAsync(tracks);
		}
	}
}