using System;
using DSharpPlus.Lavalink;

namespace Melody.Data
{
	public sealed class MelodyPlaylist
	{
		public string PlaylistUrl { get; init; }
		public LavalinkTrack[] PlaylistTracks { get; init; }
	}
}