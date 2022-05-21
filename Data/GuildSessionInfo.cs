using DSharpPlus.Entities;
using System.Collections.Generic;
using Melody.Data.Enums;

namespace Melody.Data
{
	public class GuildSessionInfo
	{
		public ulong GuildId { get; }
		public int Volume { get; set; }
		public bool CurrentlyPlaying { get; set; }
		public PlaybackMode PlaybackMode { get; set; }
		public MelodyTrack CurrentTrack { get; set; }
		public List<MelodyTrack> SessionQueue { get; }
		public DiscordChannel CommandChannel { get; set; }

		public GuildSessionInfo(ulong guildId)
		{
			this.Volume = 100;
			this.GuildId = guildId;
			this.CurrentTrack = null;
			this.CurrentlyPlaying = false;
			this.PlaybackMode = PlaybackMode.None;
			this.SessionQueue = new List<MelodyTrack>();
		}
	}
}