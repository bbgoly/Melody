using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;

namespace Melody.Utilities
{
	public static class CommandContextExtensions
	{
		private static readonly DiscordColor DefaultEmbedColor = new(126, 46, 60);
		
		public static DiscordEmbedBuilder BuildDefaultEmbedComponent(this CommandContext ctx, bool includeFooter = true)
		{
			var defaultEmbedBuilder = new DiscordEmbedBuilder().WithColor(ctx.Guild.CurrentMember.Color.Value is 0
				? DefaultEmbedColor
				: ctx.Guild.CurrentMember.Color);
			return !includeFooter ? defaultEmbedBuilder : new DiscordEmbedBuilder(defaultEmbedBuilder)
			{
				Footer = new DiscordEmbedBuilder.EmbedFooter
				{
					Text = "Requested by " + ctx.User.Username,
					IconUrl = ctx.Message.Author.AvatarUrl
				},
				Timestamp = DateTimeOffset.Now
			};
		}

		public static async Task<DiscordMessage> SendDefaultEmbedResponseAsync(this CommandContext ctx, string content) =>
			await ctx.RespondAsync(ctx.BuildDefaultEmbedComponent(false).WithDescription(content));

		public static async Task<DiscordMessage> SendDefaultEmbedMessageAsync(this CommandContext ctx, string content) =>
			await ctx.Channel.SendMessageAsync(ctx.BuildDefaultEmbedComponent(false).WithDescription(content));
	}
}