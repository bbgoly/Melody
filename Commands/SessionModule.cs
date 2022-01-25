using System;
using System.Threading.Tasks;
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
			Console.WriteLine(ctx.RawArgumentString.Length);
			await base.BeforeExecutionAsync(ctx);
		}

		[Command("disconnect"), Aliases("dc", "leave")]
		public async Task DisconnectSessionAsync(CommandContext ctx)
		{
			DiscordVoiceState voiceState = ctx.Guild.CurrentMember?.VoiceState;
			if (voiceState is not null)
			{
				await this.SessionService.DisconnectPlayerAsync(ctx.Guild);
				await ctx.SendDefaultEmbedResponseAsync($"{DiscordEmoji.FromName(ctx.Client, ":stop_button:")} Disconnected the player from {voiceState.Channel.Mention}");
			}
		}

		[Command("pause"), Aliases("stop", "hold")]
		public async Task PausePlayerAsync(CommandContext ctx)
		{
			GuildSession guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.PauseAsync();
				await ctx.SendDefaultEmbedResponseAsync($"{DiscordEmoji.FromName(ctx.Client, ":pause_button:")} Paused the player!");
			}
		}

		[Command("resume"), Aliases("r", "continue")]
		public async Task ResumePlayerAsync(CommandContext ctx)
		{
			GuildSession guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (!guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.ResumeAsync();
				await ctx.SendDefaultEmbedResponseAsync("Resumed the player");
			}
		}
		
		[Command("skip"), Aliases("s")]
		public async Task SkipTrackAsync(CommandContext ctx)
		{
			GuildSession guildSession = this.SessionService.GetOrCreateGuildSession(ctx.Channel);
			if (guildSession.SessionInfo.CurrentlyPlaying && guildSession.SessionInfo.CurrentTrack is not null)
			{
				await guildSession.SkipAsync();
				await ctx.SendDefaultEmbedResponseAsync("Skipped currently playing track");
			}
		}
	}
}