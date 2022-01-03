using DSharpPlus.Lavalink;
using DSharpPlus.Entities;
using System.Collections.Generic;
using Melody.Data.Enums;

namespace Melody.Data
{
	public class GuildSessionInfo
	{
		public int Volume { get; set; }
		public bool CurrentlyPlaying { get; set; }
		public PlaybackMode PlaybackMode { get; set; }
		public LavalinkTrack CurrentTrack { get; set; }
		public List<LavalinkTrack> SessionQueue { get; }
		public DiscordChannel CommandChannel { get; set; }

		public GuildSessionInfo(int volume = 100)
		{
			this.Volume = volume;
			this.CurrentTrack = null;
			this.CurrentlyPlaying = false;
			this.PlaybackMode = PlaybackMode.None;
			this.SessionQueue = new List<LavalinkTrack>();
		}
	}
}