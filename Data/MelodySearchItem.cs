using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DSharpPlus;
using DSharpPlus.Entities;
using Melody.Data.Enums;

namespace Melody.Data
{
	public sealed class MelodySearchItem
	{
		public string Id { get; init; }
		public string Kind { get; init; }
		public string Title
		{
			get => _title;
			init
			{
				Console.WriteLine("setting title thing: " + value);
				_title = HttpUtility.HtmlDecode(value);
			}
		}
		public string Author
		{
			get => _author;
			init
			{
				Console.WriteLine("setting author thing: " + value);
				_author = HttpUtility.HtmlDecode(value);
			}
		}
		public string AuthorId { get; init; }
		public string AuthorUrl { get; init; }
		public string ItemUrl { get; init; }
		public TimeSpan ItemDuration { get; init; }
		public string DefaultThumbnail { get; init; }
		public MelodySearchProvider SourceProvider { get; init; }

		private readonly string _title;
		private readonly string _author;
	}
	
	public static class MelodySearchItemExtensions
	{
		public static DiscordMessageBuilder BuildMessageComponents(this MelodySearchItem[] items, DiscordEmbedBuilder embedBuilder)
		{
			var selectComponentOptions = new List<DiscordSelectComponentOption>();
			for (int i = 0; i < items.Length; i++)
			{
				MelodySearchItem item = items[i];
				selectComponentOptions.Add(new DiscordSelectComponentOption(item.Title, i.ToString(), item.ItemDuration.Equals(default) ? item.Author : $"{item.Author}\u00B7{item.ItemDuration:h:mm:ss}"));
				embedBuilder.AddField($"{i + 1}. {Formatter.Bold(item.Title)}", $"by **[{item.Author}]({item.AuthorUrl})** on **[{item.SourceProvider.ToString()}]({item.ItemUrl})**"); // MelodySearchProvider.SoundCloud => "https://soundcloud.com/"
			}
			
			return new DiscordMessageBuilder()
				.WithEmbed(embedBuilder)
				.AddComponents(new DiscordSelectComponent("selectTracks", "Select a track", selectComponentOptions, false, 1, 5));
		}
	}
}