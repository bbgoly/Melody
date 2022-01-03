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
using Melody.Services;
using Melody.Utilities;

namespace Melody.Commands
{
	[RequireVoiceChannel]
	public sealed class SessionModule : BaseCommandModule
	{
		private SessionService SessionService { get; }

		public SessionModule(SessionService sessionService)
		{
			this.SessionService = sessionService;
		}

		// [Command("play"), Aliases("p"), Priority(0)]
		// public async Task PlayAsync(CommandContext ctx, [RemainingText] string searchParams) =>
		// 	await this.PlayAsync(ctx, MelodySearchProvider.YouTube, searchParams + " from default overload");
		//
		// [Command("play"), Priority(1)]
		// public async Task PlayAsync(CommandContext ctx, Uri trackUrl) =>
		// 	await ctx.RespondAsync("lol funny uri be like hehuehuehueh\nservice provider:" +
		// 	                       $"{FuzzySharp.Process.ExtractOne(trackUrl.AbsoluteUri.ToLower(), "soundcloud", "youtube", "spotify")}\nuri:{trackUrl}");
		//
		// [Command("play"), Priority(2)]
		// public async Task PlayAsync(CommandContext ctx, MelodySearchProvider searchProvider, [RemainingText] string searchParams)
		// {
		// 	if (searchParams.Length == 0) return;
		// 	MelodySearchItem[] responseItems = await this.SessionService.SearchTrackAsync(ctx, searchProvider, searchParams);
		// 	var promptSelectMessage = await responseItems.BuildMessageComponents(ctx.BuildDefaultEmbedComponent()
		// 		.WithTitle("Search Results Found!")
		// 		.WithDescription("Results for term \"" + searchParams + "\" were found on " +
		// 		                 responseItems[0].SourceProvider + "!\n\nSelect one or more of the options below.")
		// 		.WithImageUrl(responseItems[0].DefaultThumbnail)).SendAsync(ctx.Channel);
		// 	
		// 	var interactivityExtension = ctx.Client.GetInteractivity();
		// 	var selectedTracks = await interactivityExtension.WaitForSelectAsync(promptSelectMessage, ctx.User, "selectTracks", null);
		// 	if (selectedTracks.TimedOut || ctx.Guild.CurrentMember?.VoiceState?.Channel is null)
		// 	{
		// 		Console.WriteLine("timed out: " + selectedTracks.TimedOut);
		// 		await ctx.RespondAsync(ctx.BuildDefaultEmbedComponent(false)
		// 			.WithDescription("Track selection timed out, no tracks were added to queue."));
		// 		return;
		// 	}
		// 	await selectedTracks.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
		// 	await selectedTracks.Result.Interaction.DeleteOriginalResponseAsync();
		//
		// 	List<string> trackTitles = new List<string>(selectedTracks.Result.Values.Length);
		// 	foreach (string resultValue in selectedTracks.Result.Values)
		// 	{
		// 		int itemIndex = int.Parse(resultValue);
		// 		bool firstTrack = await this.SessionService.AddTracks(ctx, responseItems[itemIndex]);
		// 		if (!firstTrack) trackTitles.Add(responseItems[itemIndex].Title);
		// 	}
		//
		// 	if (trackTitles.Count > 0)
		// 		await ctx.Channel.SendMessageAsync(ctx.BuildDefaultEmbedComponent(false)
		// 			.WithDescription($"Added track(s) **\"{string.Join("\"**, **\"", trackTitles.Select(HttpUtility.HtmlDecode))}\"** to queue"));
		// }

		[Command("disconnect"), Aliases("dc", "leave")]
		public async Task DisconnectAsync(CommandContext ctx) => await this.SessionService.DisconnectPlayerAsync(ctx);

		[Command("skip"), Aliases("s")]
		public async Task SkipAsync(CommandContext ctx) => await this.SessionService.SkipTrackAsync(ctx);
	}
}