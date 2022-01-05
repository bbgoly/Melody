using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Melody.Data.Attributes;
using Melody.Services;

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
		public async Task DisconnectAsync(CommandContext ctx) => await this.SessionService.DisconnectPlayerAsync(ctx);

		[Command("skip"), Aliases("s")]
		public async Task SkipAsync(CommandContext ctx) => await this.SessionService.SkipTrackAsync(ctx);
	}
}