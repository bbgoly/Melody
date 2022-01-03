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
		public string Title { get; init; }
		public string[] Authors { get; init; }
		public string[] AuthorIds { get; init; }
		public string ItemUrl { get; init; }
		public string DefaultThumbnail { get; init; }
		public MelodySearchProvider SourceProvider { get; init; }
	}
	
	public static class MelodySearchItemExtensions
	{
		public static DiscordMessageBuilder BuildMessageComponents(this MelodySearchItem[] arr, DiscordEmbedBuilder embedBuilder)
		{
			var selectComponentOptions = new List<DiscordSelectComponentOption>();
			for (int i = 0; i < arr.Length; i++)
			{
				MelodySearchItem item = arr[i];
				string trackTitle = HttpUtility.HtmlDecode(item.Title);
				string[] trackAuthors = item.Authors.Select(HttpUtility.HtmlDecode).ToArray();
				selectComponentOptions.Add(new DiscordSelectComponentOption(trackTitle, i.ToString(), string.Join(", ", trackAuthors)));
				embedBuilder.AddField($"{i + 1}. {Formatter.Bold(trackTitle)}", $"by [{trackAuthors.Select(Formatter.Bold)}](" +
					item.SourceProvider switch
					{
						MelodySearchProvider.YouTube => "https://www.youtube.com/channel/",
						MelodySearchProvider.Spotify => "https://open.spotify.com/artist/",
						MelodySearchProvider.SoundCloud => "https://soundcloud.com/",
						_ => throw new ArgumentOutOfRangeException()
					} + string.Join(", ", item.AuthorIds) + ") on " + Formatter.Bold(item.SourceProvider.ToString()));
			}

			return new DiscordMessageBuilder()
				.WithEmbed(embedBuilder)
				.AddComponents(new DiscordSelectComponent("selectTracks", "Select a track", selectComponentOptions, false, 1, 5));
		}
	}
}