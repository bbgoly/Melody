using System;
using DSharpPlus.Lavalink;

namespace Melody.Data
{
	public sealed class MelodyPlaylist
	{
		public LavalinkTrack[] PlaylistTracks { get; init; }
	}
}