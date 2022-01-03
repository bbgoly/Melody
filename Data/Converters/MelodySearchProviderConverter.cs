using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.Entities;
using Melody.Data.Enums;

namespace Melody.Data.Converters
{
	public sealed class MelodySearchProviderConverter : IArgumentConverter<MelodySearchProvider>
	{
		public Task<Optional<MelodySearchProvider>> ConvertAsync(string value, CommandContext ctx)
		{
			//ExtractedResult<string> fuzzyMatch = Process.ExtractOne(value.ToLower(), "soundcloud", "youtube", "spotify");
			//return Task.FromResult(Optional.FromValue(Enum.Parse<MelodySearchProvider>(fuzzyMatch.Value, true)));
			Console.WriteLine("lol converting");
			return Task.FromResult(value.ToLower() switch
			{
				"y" => Optional.FromValue(MelodySearchProvider.YouTube),
				"yt" => Optional.FromValue(MelodySearchProvider.YouTube),
				"youtube" => Optional.FromValue(MelodySearchProvider.YouTube),
				"s" => Optional.FromValue(MelodySearchProvider.Spotify),
				"sp" => Optional.FromValue(MelodySearchProvider.Spotify),
				"spot" => Optional.FromValue(MelodySearchProvider.Spotify),
				"spotify" => Optional.FromValue(MelodySearchProvider.Spotify),
				"sc" => Optional.FromValue(MelodySearchProvider.SoundCloud),
				"sound" => Optional.FromValue(MelodySearchProvider.SoundCloud),
				"soundcloud" => Optional.FromValue(MelodySearchProvider.SoundCloud),
				_ => Optional.FromNoValue<MelodySearchProvider>()
			});
		}
	}
}