using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace Melody.Data.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
	public sealed class RequireVoiceChannelAttribute : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			Console.WriteLine(ctx.Member is null);
			Console.WriteLine(ctx.Member.VoiceState is null);
			Console.WriteLine("vs");
			Console.WriteLine(ctx.Member.VoiceState.Channel is null);
			return Task.FromResult<bool>(ctx.Member?.VoiceState?.Channel is not null && (ctx.Command.Name is "play" ||
				ctx.Command.Parent?.Name is "play" || ctx.Guild.CurrentMember?.VoiceState?.Channel is not null));
		}
	}
}