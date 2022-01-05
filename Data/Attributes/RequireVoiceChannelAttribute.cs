using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Melody.Data.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
	public sealed class RequireVoiceChannelAttribute : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help) =>
			Task.FromResult<bool>(ctx.Member?.VoiceState?.Channel is not null && (ctx.Command.Name is "play" ||
				ctx.Command.Parent?.Name is "play" || ctx.Guild.CurrentMember?.VoiceState?.Channel is not null));
	}
}