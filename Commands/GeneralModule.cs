using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Melody.Utilities;

namespace Melody.Commands
{
	public class GeneralCommands : BaseCommandModule
	{
		private ImmutableArray<string> DefaultPrefixes { get; }
		
		public GeneralCommands(MelodyDiscordConfiguration discordConfiguration)
		{
			this.DefaultPrefixes = discordConfiguration.DefaultPrefixes;
		}

		[Command("ping")]
		public async Task PingAsync(CommandContext ctx) =>
			await ctx.RespondAsync(DiscordEmoji.FromName(ctx.Client, ":hourglass:") + ctx.Client.Ping + " ms");

		[Command("prefix")]
		public async Task PrefixAsync(CommandContext ctx) =>
			await ctx.RespondAsync($"I only respond to these prefixes: {string.Join(", ", this.DefaultPrefixes)}!" +
			                       $"\n\nAlternatively, you can also {ctx.Guild.CurrentMember.Mention} or use slash commands (not yet implemented)!");

		[Command("purge"), RequireOwner]
		public async Task PurgeAsync(CommandContext ctx, int max) =>
			await ctx.Channel.DeleteMessagesAsync(await ctx.Channel.GetMessagesAsync(max));

		[Command("purgeBefore"), RequireOwner]
		public async Task PurgeBeforeAsync(CommandContext ctx, ulong messageBeforeId, int max)
		{
			IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesBeforeAsync(messageBeforeId, max);
			await ctx.Channel.DeleteMessagesAsync(messages);
		}
	}
}