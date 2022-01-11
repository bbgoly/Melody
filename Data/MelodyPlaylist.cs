using System;
using DSharpPlus.Lavalink;

namespace Melody.Data
{
	public sealed class MelodyPlaylist
	{
		public Uri PlaylistUri { get; init; }
		public string PlaylistThumbnail { get; init; }
		public LavalinkTrack[] PlaylistTracks { get; init; }
	}
}