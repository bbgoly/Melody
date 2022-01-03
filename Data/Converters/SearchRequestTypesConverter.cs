using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using SpotifyAPI.Web;

namespace Melody.Data.Converters
{
	public sealed class SearchRequestTypesConverter : IArgumentConverter<SearchRequest.Types>
	{
		public Task<Optional<SearchRequest.Types>> ConvertAsync(string value, CommandContext ctx) =>
			value.ToLower() switch
			{
				"t" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Track)),
				"track" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Track)),
				"tracks" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Track)),
				"p" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Playlist)),
				"playlist" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Playlist)),
				"playlists" => Task.FromResult(Optional.FromValue(SearchRequest.Types.Playlist)),
				_ => Task.FromResult(Optional.FromValue(SearchRequest.Types.Track | SearchRequest.Types.Playlist))
			};
	}
}