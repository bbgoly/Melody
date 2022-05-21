using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Melody.Data;
using Melody.Data.Attributes;
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

		public override async Task BeforeExecutionAsync(CommandContext ctx)
		{
			await base.BeforeExecutionAsync(ctx);
		}

		[Command("disconnect"), Aliases("dc", "leave")]
		public async Task DisconnectSessionAsync(CommandContext ctx)
		{
			var voiceState = ctx.Guild.CurrentMember.VoiceState;
			await this.SessionService.DisconnectPlayerAsync(ctx.Guild);
			await ctx.SendDefaultEmbedResponseAsync($"{DiscordEmoji.FromName(ctx.Client, ":stop_button:")} Disconnected the player from {voiceState.Channel.Mention}");
		}

		[Command("pause"), Aliases("stop", "hold")]
		public async Task PausePlayerAsync(CommandContext ctx)
		{
			var guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.PauseAsync();
				await ctx.SendDefaultEmbedResponseAsync($"{DiscordEmoji.FromName(ctx.Client, ":pause_button:")} Paused the player!");
			}
		}

		[Command("resume"), Aliases("r", "continue")]
		public async Task ResumePlayerAsync(CommandContext ctx)
		{
			var guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (!guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				MelodyTrack currentTrack = guildSession.SessionInfo.CurrentTrack;
				var resumedPlayingEmbed = currentTrack.RequestingMember.BuildDefaultEmbedComponent(ctx.Guild.CurrentMember)
					.WithTitle("Resumed Playback")
					.WithDescription($"{Formatter.Bold(currentTrack.Track.Title)}\nby {Formatter.Bold(currentTrack.Track.Author)} on **[{currentTrack.SourceProvider}]({currentTrack.TrackUrl} \"{currentTrack.TrackUrl}\")**")
					.WithImageUrl(currentTrack.DefaultThumbnail);
			
				var resumedPlayingEmbed2 = currentTrack.RequestingMember.BuildDefaultEmbedComponent(ctx.Guild.CurrentMember)
					.WithTitle("Resumed Playback")
					.WithDescription($"{Formatter.Bold(currentTrack.Track.Title)}\nby {Formatter.Bold(currentTrack.Track.Author)} on **[{currentTrack.SourceProvider}]({currentTrack.TrackUrl} \"{currentTrack.TrackUrl}\")**")
					.WithThumbnail(currentTrack.DefaultThumbnail);
				await ctx.Channel.SendMessageAsync(resumedPlayingEmbed);
				await ctx.Channel.SendMessageAsync(resumedPlayingEmbed2);
				await guildSession.ResumeAsync();
			}
		}
		
		[Command("skip"), Aliases("s")]
		public async Task SkipTrackAsync(CommandContext ctx)
		{
			var guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.SkipAsync();
				await ctx.SendDefaultEmbedResponseAsync("Skipped currently playing track");
			}
		}

		[Command("nowPlaying"), Aliases("np", "now")]
		public async Task CurrentlyPlayingAsync(CommandContext ctx)
		{
			var guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			var currentTrack = guildSession.SessionInfo.CurrentTrack;
			var currentlyPlayingEmbed2 = currentTrack.RequestingMember
				.BuildDefaultEmbedComponent(ctx.Guild.CurrentMember)
				.WithTitle("Currently Playing")
				.WithImageUrl(currentTrack.DefaultThumbnail)
				.AddField("Title", $"[{currentTrack.Track.Title}] ({currentTrack.TrackUrl} \"{currentTrack.TrackUrl}\")")
				.AddField("Author", $"[{currentTrack.Track.Author} on {currentTrack.SourceProvider}]({currentTrack.AuthorUrl} \"{currentTrack.AuthorUrl}\")", true)
				.AddField("Duration", $"{currentTrack.Duration:h\\:mm\\:ss)}/{currentTrack.Track.Length:h\\:mm\\:ss}", true);

			if (guildSession.SessionInfo.SessionQueue.Count > 1)
			{
				var nextTrack = guildSession.SessionInfo.SessionQueue[1];
				currentlyPlayingEmbed2.AddField("Up Next", $"[{nextTrack.Track.Title}]({nextTrack.TrackUrl} \"{nextTrack.TrackUrl}\")");
				currentlyPlayingEmbed2.AddField("Author", $"[{nextTrack.Track.Author} on {nextTrack.SourceProvider}]({nextTrack.AuthorUrl} \"{nextTrack.AuthorUrl}\")", true);
				currentlyPlayingEmbed2.AddField("Duration", nextTrack.Duration.ToString(@"h\\:mm\\:ss").TrimStart('0', ':'), true);
			}
			await ctx.Channel.SendMessageAsync(currentlyPlayingEmbed2);
		}
	}
}