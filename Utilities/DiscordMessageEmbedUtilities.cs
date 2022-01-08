using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Melody.Utilities
{
	public static class DiscordMessageEmbedUtilities
	{
		private static readonly DiscordColor DefaultEmbedColor = new(126, 46, 60);
		
		public static DiscordEmbedBuilder BuildDefaultEmbedComponent(this CommandContext ctx) =>
			InternalDefaultEmbedBuilder(ctx.Guild.CurrentMember)
				.WithFooter("Requested by " + ctx.User.Username, ctx.Member.GuildAvatarUrl)
				.WithTimestamp(DateTimeOffset.Now);

		public static async Task<DiscordMessage> SendDefaultEmbedResponseAsync(this CommandContext ctx, string content) =>
			await ctx.RespondAsync(InternalDefaultEmbedBuilder(ctx.Guild.CurrentMember).WithDescription(content));

		public static async Task<DiscordMessage> SendDefaultEmbedMessageAsync(this DiscordChannel channel, string content) =>
			await channel.SendMessageAsync(InternalDefaultEmbedBuilder(channel.Guild.CurrentMember).WithDescription(content));

		private static DiscordEmbedBuilder InternalDefaultEmbedBuilder(DiscordMember currentMember) =>
			new DiscordEmbedBuilder().WithColor(currentMember.Color.Value is 0
				? DefaultEmbedColor
				: currentMember.Color);
	}
}