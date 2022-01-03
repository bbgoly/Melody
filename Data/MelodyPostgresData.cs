using System.Collections.Generic;

namespace Melody.Data
{
	public class MelodyUser
	{
		public long Id { get; set; }
		public ulong UserId { get; set; }
		public virtual List<UserPlaylist> Playlists { get; set; }
	}
	
	public class UserPlaylist
	{
		public long Id { get; set; }
		public string Title { get; set; }

		public ulong UserId { get; set; }
		public virtual MelodyUser MelodyUser { get; set; }
		public virtual List<PlaylistTrack> Tracks { get; set; }
	}

	public class PlaylistTrack
	{
		public long Id { get; set; }
		public string TrackUrl { get; set; }
		
		public long UserPlaylistId { get; set; }
		public virtual UserPlaylist Playlist { get; set; }
	}
}