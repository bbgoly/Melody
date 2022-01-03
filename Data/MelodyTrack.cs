using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Melody.Data.Enums;

namespace Melody.Data
{
	public sealed class MelodyTrack
	{
		public LavalinkTrack Track { get; init; }
		public string DefaultThumbnail { get; init; }
		public LavalinkTrack[] PlaylistTracks { get; init; }
		public DiscordMember RequestingMember { get; init; }
		public MelodySearchProvider SourceProvider { get; init; }
		public LavalinkLoadResultType LavalinkResultType { get; init; }
	}
}