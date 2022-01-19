using System;
using System.Collections.Generic;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Melody.Data.Enums;

/* TODO Track Add
 * Upon creating a MelodySearchItem determine whether the item is a playlist or a track, so that a MelodyPlaylist
 * component can be made up of it
 *
 * Upon adding the MelodyTrack to queue, check if it has a MelodyPlaylist component and add them to the "titles" list
 */

/* TODO Tracks
 Now playing embed:
 Title: Now Playing <music emojis?>
 Description: CurrentTrack Title
 ThumbnailUrl: MusicPlaying gif
 Fields:
	Sourced From:
	[PlaylistLink on] SourceProvider
	
	Up Next:
	NextTrack Title
 ImageUrl: CurrentTrack Thumbnail
 Footer: Requested by <user>
 */

/* Track Description
 Parse YouTube track description for a list of timestamps and when the playback duration reaches a point specified
 by the description, send the content past the timestamp
 - Parse out [], -, and newline characters (fit text onto one line)
 - Use Arcane OST Full Album by Cinema Ost Club as an example
 
 Add YouTube Chapters support
 */

/*
 * Check if >>p spotify works for resume then executes spotify command
 * Ask in DSharpPlus Discord server on how to cancel commands in BeforeExecutionAsync because return only ends BeforeExecutionAsync
 * and proceeds on with the command
 */

namespace Melody.Data
{
	public sealed class MelodyTrack
	{
		// TODO: Determine whether Track property is needed when a playlist exists
		public string TrackUrl { get; init; }
		public LavalinkTrack Track { get; init; }
		public string DefaultThumbnail { get; init; }
		public MelodyPlaylist? Playlist { get; init; }
		public DiscordMember RequestingMember { get; init; }
		public MelodySearchProvider SourceProvider { get; init; }
		public LavalinkLoadResultType LavalinkResultType { get; init; }
		public IReadOnlyDictionary<TimeSpan, string>? VideoChapters { get; init; }
	}
}