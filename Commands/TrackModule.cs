﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink.EventArgs;
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
	[Group("play"), Aliases("p", "search"), RequireVoiceChannel]
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
			if (botMember.VoiceState?.Channel is null || botMember.VoiceState?.Channel != ctx.Member.VoiceState.Channel) // ctx.Command.Name is "play" && ()
			{
				await this.SessionService.ConnectPlayerAsync(ctx);
				await ctx.RespondAsync(ctx.BuildDefaultEmbedComponent(false)
					.WithDescription("Joined " + ctx.Guild.CurrentMember.VoiceState.Channel.Mention + "!"));
			}
			await base.BeforeExecutionAsync(ctx);
		}

		[GroupCommand, Command("youtube"), Aliases("yt")]
		public async Task PlayYouTubeAsync(CommandContext ctx, [RemainingText] string searchParams)
		{
			if (searchParams.Length == 0) return;
			var ytResponse = await this.YoutubeService.SearchYoutube(searchParams);
			await this.InternalSearchResolver(ctx, ytResponse.Items.Select(item => new MelodySearchItem
			{
				Id = item.Id.Kind == "youtube#playlist" ? item.Id.PlaylistId : item.Id.VideoId,
				Kind = item.Id.Kind,
				Title = item.Snippet.Title,
				Authors = new [] { item.Snippet.ChannelTitle },
				AuthorIds = new [] { item.Snippet.ChannelId },
				ItemUrl = "https://www.youtube.com/" + (item.Id.Kind == "youtube#playlist"
					? "playlist?list=" + item.Id.PlaylistId
					: "watch?v=" + item.Id.VideoId),
				DefaultThumbnail = item.Snippet.Thumbnails.Medium.Url,
				SourceProvider = MelodySearchProvider.YouTube
			}).ToArray());
		}

		[GroupCommand]
		public async Task PlayFromUriAsync(CommandContext ctx, Uri trackUri)
		{
			Console.WriteLine("received request from discord");
			await ctx.SendDefaultEmbedResponseAsync(trackUri.AbsoluteUri);
		}

		[Command("spotify"), Aliases("sp", "spot")]
		public async Task PlaySpotifyAsync(CommandContext ctx, [RemainingText] string searchParams)
		{
			if (searchParams.Length == 0) return;
			using var scope = this.ServiceScopeFactory.CreateScope();
			var spotifyService = scope.ServiceProvider.GetService<SpotifyService>();
			if (spotifyService is not null)
			{
				var spotifyClient = await spotifyService.BuildSpotifyClient();
				var spotifyResponse = await spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, searchParams));
				
				if (spotifyResponse.Tracks.Items is null || spotifyResponse.Tracks.Items.Count == 0)
					throw new TrackNotFoundException(searchParams, MelodySearchProvider.Spotify);
				
				await this.InternalSearchResolver(ctx, spotifyResponse.Tracks.Items.Select(track =>
					new MelodySearchItem
					{
						Id = track.Id,
						Kind = "spotify#track",
						Title = track.Name,
						Authors = track.Artists.Select(artist => artist.Name).ToArray(),
						AuthorIds = track.Artists.Select(artist => artist.Id).ToArray(),
						ItemUrl = track.Uri,
						DefaultThumbnail = track.Album.Images.ElementAt(0).Url,
						SourceProvider = MelodySearchProvider.Spotify
					}).ToArray());
			}
		}
		
		private async Task InternalSearchResolver(CommandContext ctx, MelodySearchItem[] searchItems)
		{
			var promptSelectMessage = await searchItems.BuildMessageComponents(ctx.BuildDefaultEmbedComponent()
				.WithTitle("Search Results Found!")
				.WithDescription("Search results for your provided search parameters were found on " +
				                 searchItems[0].SourceProvider + "!\n\nSelect one or more of the options below.")
				.WithThumbnail(searchItems[0].DefaultThumbnail)).SendAsync(ctx.Channel);

			var interactivityExtension = ctx.Client.GetInteractivity();
			var selectedTracks = await interactivityExtension.WaitForSelectAsync(promptSelectMessage, ctx.User, "selectTracks", null);
			if (selectedTracks.TimedOut || ctx.Guild.CurrentMember?.VoiceState?.Channel is null)
			{
				await ctx.SendDefaultEmbedResponseAsync("Track selection timed out, no tracks were added to queue.");
				return;
			}

			await selectedTracks.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
			await selectedTracks.Result.Interaction.DeleteOriginalResponseAsync();

			List<string> trackTitles = new List<string>(selectedTracks.Result.Values.Length);
			foreach (string resultValue in selectedTracks.Result.Values)
			{
				int itemIndex = int.Parse(resultValue);
				bool firstTrack = await this.SessionService.AddTracks(ctx, searchItems[itemIndex]);
				if (!firstTrack) trackTitles.Add(searchItems[itemIndex].Title);
			}

			if (trackTitles.Count > 0)
				await ctx.SendDefaultEmbedMessageAsync($"Added track(s) **\"{string.Join("\"**, **\"", trackTitles.Select(HttpUtility.HtmlDecode))}\"** to queue");
		}
	}
}