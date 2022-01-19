using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Melody.Utilities
{
	public static class DiscordMessageEmbedUtilities
	{
		private static readonly DiscordColor DefaultEmbedColor = new(126, 46, 60);
		
		// TODO: Only have Track(s) Added to Queue appear if queue is not empty or more than one tracks are added
		public static DiscordEmbedBuilder BuildDefaultEmbedComponent(this CommandContext ctx) =>
			BuildInternalDefaultEmbedBuilder(ctx.Guild.CurrentMember)
				.WithFooter($"Requested by {ctx.User.Username}#{ctx.User.Discriminator}", ctx.Member.AvatarUrl)
				.WithTimestamp(DateTimeOffset.Now);

		public static DiscordEmbedBuilder BuildDefaultEmbedComponent(this DiscordMember member, DiscordMember bot) =>
			BuildInternalDefaultEmbedBuilder(bot)
				.WithFooter($"Requested by {member.DisplayName}#{member.Discriminator}", member.AvatarUrl)
				.WithTimestamp(DateTimeOffset.Now);

		public static async Task<DiscordMessage> SendDefaultEmbedResponseAsync(this CommandContext ctx, string content) =>
			await ctx.RespondAsync(BuildInternalDefaultEmbedBuilder(ctx.Guild.CurrentMember).WithDescription(content));

		public static async Task<DiscordMessage> SendDefaultEmbedMessageAsync(this DiscordChannel channel, string content) =>
			await channel.SendMessageAsync(BuildInternalDefaultEmbedBuilder(channel.Guild.CurrentMember).WithDescription(content));

		private static DiscordEmbedBuilder BuildInternalDefaultEmbedBuilder(DiscordMember currentMember) =>
			new DiscordEmbedBuilder().WithColor(currentMember.Color.Value is 0
				? DefaultEmbedColor
				: currentMember.Color);
	}
}