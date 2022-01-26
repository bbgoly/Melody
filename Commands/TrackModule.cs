using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Melody.Data;
using Melody.Data.Attributes;
using Melody.Data.Enums;
using Melody.Data.Exceptions;
using Melody.Services;
using Melody.Utilities;
using Microsoft.Extensions.DependencyInjection;
using SpotifyAPI.Web;

namespace Melody.Commands
{
	[Group("play"), Aliases("p", "search", "query"), RequireVoiceChannel]
	public sealed class TrackModule : BaseCommandModule
	{
		private SessionService SessionService { get; }
		private YoutubeService YoutubeService { get; }
		private IServiceScopeFactory ServiceScopeFactory { get; }

		public TrackModule(SessionService sessionService, YoutubeService youtubeService, IServiceScopeFactory serviceScopeFactory)
		{
			this.SessionService = sessionService;
			this.YoutubeService = youtubeService;
			this.ServiceScopeFactory = serviceScopeFactory;
		}
		
		public override async Task BeforeExecutionAsync(CommandContext ctx)
		{
			DiscordMember botMember = ctx.Guild.CurrentMember;
			if (botMember.VoiceState?.Channel is null || botMember.VoiceState?.Channel != ctx.Member.VoiceState.Channel)
			{
				await this.SessionService.ConnectPlayerAsync(ctx);
				await ctx.SendDefaultEmbedResponseAsync($"Joined {ctx.Guild.CurrentMember.VoiceState.Channel.Mention}!");
			}
			
			if (ctx.RawArgumentString.Length == 0)
			{
				//await this.SessionService.ResumeAsync(ctx.Channel);
				await ctx.SendDefaultEmbedResponseAsync($"Resumed the player! {DiscordEmoji.FromName(ctx.Client, ":play_pause:")}");
			}
			await base.BeforeExecutionAsync(ctx);
		}

		[GroupCommand, Priority(1)]
		public async Task PlayFromUriAsync(CommandContext ctx, Uri trackUri)
		{
			Console.WriteLine("received request from discord");
			await ctx.SendDefaultEmbedResponseAsync(trackUri.AbsoluteUri + " uri");
		}
		
		[GroupCommand, Command("youtube"), Aliases("yt"), Priority(0)]
		public async Task PlayYouTubeAsync(CommandContext ctx, [RemainingText] string query)
		{
			Console.WriteLine("youtube command used");
			if (query.Length == 0) return;
			Console.WriteLine("query has content: " + query);
			var ytResponse = await this.YoutubeService.SearchYoutube(query);
			await this.InternalSearchResolver(ctx, ytResponse.Items.Select(item => new MelodySearchItem
			{
				Id = item.Id.Kind == "youtube#playlist" ? item.Id.PlaylistId : item.Id.VideoId,
				Kind = item.Id.Kind == "youtube#playlist" ? "playlist" : "track",
				Title = item.Snippet.Title,
				Author = item.Snippet.ChannelTitle,
				AuthorId = item.Snippet.ChannelId,
				AuthorUrl = "https://www.youtube.com/channel/" + item.Snippet.ChannelId,
				ItemUrl = "https://www.youtube.com/" + (item.Id.Kind == "youtube#playlist"
					? "playlist?list=" + item.Id.PlaylistId
					: "watch?v=" + item.Id.VideoId),
				ItemDuration = TimeSpan.Zero,
				DefaultThumbnail = item.Snippet.Thumbnails.Medium.Url,
				SourceProvider = MelodySearchProvider.YouTube
			}).ToArray());
		}

		[Command("spotify"), Aliases("sp", "spot")]
		public async Task PlaySpotifyAsync(CommandContext ctx, [RemainingText] string query)
		{
			Console.WriteLine("spotify command used");
			if (query.Length == 0) return;
			Console.WriteLine("query has content: " + query);
			using var scope = this.ServiceScopeFactory.CreateScope();
			var spotifyService = scope.ServiceProvider.GetService<SpotifyService>();
			if (spotifyService is not null)
			{
				Console.WriteLine("spotifyservice is not null");
				var spotifyClient = await spotifyService.BuildSpotifyClient();
				var spotifyResponse = await spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, query)); // | SearchRequest.Types.Playlist
				Console.WriteLine("spotify search did not error");
				if (spotifyResponse.Tracks.Items is null || spotifyResponse.Tracks.Items.Count == 0)
					throw new TrackNotFoundException(query, MelodySearchProvider.Spotify);
				Console.Write("checking if image is 640x640: ");
				Console.WriteLine(spotifyResponse.Tracks.Items[0].Album.Images.ElementAt(0).Height == 640);
				Console.WriteLine("spotify command should be working, reached end");
				await this.InternalSearchResolver(ctx, spotifyResponse.Tracks.Items.Select(track => new MelodySearchItem
				{
					Id = track.Id,
					Kind = "track",
					Title = track.Name,
					Author = track.Artists[0].Name,
					AuthorId = track.Artists[0].Id,
					AuthorUrl = "https://open.spotify.com/artist/" + track.Artists[0].Id,
					ItemUrl = track.Uri,
					ItemDuration = TimeSpan.FromSeconds((double)track.DurationMs / 1000),
					DefaultThumbnail = track.Album.Images.ElementAt(0).Url,
					SourceProvider = MelodySearchProvider.Spotify
				}).ToArray());
			}
		}

		[Command("soundcloud"), Aliases("sc", "sound")]
		public async Task PlaySoundCloudAsync(CommandContext ctx, [RemainingText] string query)
		{
			if (query.Length == 0) return;
		}
		
		private async Task InternalSearchResolver(CommandContext ctx, MelodySearchItem[] searchItems)
		{
			Console.WriteLine("running internal search resolver, checking for valid first entry..");
			Console.WriteLine(searchItems[0]);
			Console.WriteLine(searchItems[0].SourceProvider);
			Console.WriteLine(searchItems[0].DefaultThumbnail);
			var promptSelectMessage = await searchItems.BuildMessageComponents(ctx.BuildDefaultEmbedComponent()
				.WithTitle("Search Results Found!")
				.WithDescription("Search results related to your provided search query were found on " +
				                 searchItems[0].SourceProvider + "!\n\nSelect one or more of the options below.")
				.WithThumbnail(searchItems[0].DefaultThumbnail)).SendAsync(ctx.Channel);
			
			var interactivityExtension = ctx.Client.GetInteractivity();
			var selectedTracks = await interactivityExtension.WaitForSelectAsync(promptSelectMessage, ctx.User, "selectTracks", null);
			Console.WriteLine("waiting for interactivity");
			if (selectedTracks.TimedOut || ctx.Guild.CurrentMember?.VoiceState?.Channel is null)
			{
				await ctx.SendDefaultEmbedResponseAsync("Track selection timed out, no tracks were added to queue");
				return;
			}
			await selectedTracks.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
			await selectedTracks.Result.Interaction.DeleteOriginalResponseAsync();
			Console.WriteLine("passed time out");
			MelodySearchItem[] selectedItems = new MelodySearchItem[selectedTracks.Result.Values.Length];
			for (int i = 0; i < selectedTracks.Result.Values.Length; i++)
			{
				int itemIndex = int.Parse(selectedTracks.Result.Values[i]);
				selectedItems[i] = searchItems[itemIndex];
				Console.WriteLine("Added " + selectedItems[i].Title);
			}
			Console.WriteLine("sent");
			await this.SessionService.AddTracksAsync(ctx, selectedItems);
			Console.WriteLine("done");
		}
	}
}