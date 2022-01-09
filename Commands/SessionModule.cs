using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
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

		[Command("disconnect"), Aliases("dc", "leave")]
		public async Task DisconnectPlayerAsync(CommandContext ctx)
		{
			DiscordVoiceState voiceState = ctx.Guild.CurrentMember?.VoiceState;
			if (voiceState is not null)
			{
				await this.SessionService.DisconnectPlayerAsync(ctx.Guild);
				await ctx.SendDefaultEmbedResponseAsync($"Disconnected the player from {voiceState.Channel.Mention} {DiscordEmoji.FromName(ctx.Client, ":stop_button:")}");
			}
		}

		[Command("pause"), Aliases("stop", "hold")]
		public async Task PausePlayerAsync(CommandContext ctx)
		{
			await this.SessionService.PauseAsync(ctx.Channel);
			await ctx.SendDefaultEmbedResponseAsync($"{DiscordEmoji.FromName(ctx.Client, ":pause_button:")} Paused the player!");
		}

		[Command("skip"), Aliases("s")]
		public async Task SkipTrackAsync(CommandContext ctx)
		{
			
		}
	}
}