using System.Collections.Generic;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace Melody.Commands
{
	public sealed class HelpFormatter : BaseHelpFormatter
	{
		private StringBuilder StringBuilder { get; }
		private DiscordEmbedBuilder DiscordEmbedBuilder { get; }
		
		public HelpFormatter(CommandContext ctx) : base(ctx)
		{
			this.DiscordEmbedBuilder = new DiscordEmbedBuilder
			{
				
				Title = "Help",
				Description = "bruh"
			};
			this.StringBuilder = new StringBuilder();
		}

		public override BaseHelpFormatter WithCommand(Command command)
		{
			this.DiscordEmbedBuilder.AddField(command.Name, command.Description);
			this.StringBuilder.AppendLine($"{command.Name} - {command.Description}");
			return this;
		}

		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
		{
			foreach (Command command in subcommands)
			{
				this.DiscordEmbedBuilder.AddField(command.Name, command.Description);
				this.StringBuilder.AppendLine($"{command.Name} - {command.Description}");
			}
			return this;
		}

		public override CommandHelpMessage Build()
		{
			throw new System.NotImplementedException();
		}
	}
}