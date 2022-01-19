using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Melody.Data.Enums;
using Melody.Services;
using Melody.Utilities;

namespace Melody.Data
{
	public sealed class GuildSession
	{
		public ulong GuildId { get; }
		public GuildSessionInfo SessionInfo { get; }
		private LavalinkService LavalinkService { get; }
		private LavalinkGuildConnection LavalinkPlayer { get; set; }
		
		private readonly object _lock = new object();
		
		public GuildSession(ulong guildId, LavalinkService lavalinkService)
		{
			this.GuildId = guildId;
			this.SessionInfo = new GuildSessionInfo();
			this.LavalinkService = lavalinkService;
		}

		public async Task ConnectPlayerAsync(DiscordChannel channel)
		{
			if (this.LavalinkPlayer is not null && this.LavalinkPlayer.Channel != channel) await this.DisconnectPlayerAsync(false);
			if (this.LavalinkPlayer is null || !this.LavalinkPlayer.IsConnected)
			{
				this.LavalinkPlayer = await this.LavalinkService.LavalinkNode.ConnectAsync(channel);
				this.LavalinkPlayer.PlaybackFinished += Lavalink_PlaybackFinished;
				this.LavalinkPlayer.TrackException += Lavalink_TrackException;
			}
		}

		public async Task DisconnectPlayerAsync(bool shouldDestroy = true)
		{
			Console.WriteLine(shouldDestroy);
			if (shouldDestroy) await this.ClearTracksAsync();
			await this.LavalinkPlayer.DisconnectAsync(shouldDestroy);
			this.LavalinkPlayer.PlaybackFinished -= this.Lavalink_PlaybackFinished;
			this.LavalinkPlayer.TrackException -= this.Lavalink_TrackException;
			this.LavalinkPlayer = null;
		}
		
		private async Task PlayNextTrackAsync()
		{
			if (this.LavalinkPlayer?.Channel is null) await this.DisconnectPlayerAsync();
			if (!this.SessionInfo.CurrentlyPlaying && this.SessionInfo.SessionQueue.Count > 0)
			{
				MelodyTrack nextTrack = this.SessionInfo.SessionQueue[0];
				this.SessionInfo.CurrentTrack = nextTrack;
				this.SessionInfo.CurrentlyPlaying = true;
				await this.LavalinkPlayer.PlayAsync(nextTrack.Track);

				var nowPlayingEmbed = nextTrack.RequestingMember.BuildDefaultEmbedComponent(this.LavalinkPlayer.Guild.CurrentMember)
					.WithTitle("Now Playing")
					.WithDescription($"{Formatter.Bold(nextTrack.Track.Title)}\nby {Formatter.Bold(nextTrack.Track.Author)} on **[{nextTrack.SourceProvider}]({nextTrack.TrackUrl} \"{nextTrack.TrackUrl}\")**")
					.WithThumbnail(nextTrack.DefaultThumbnail);
				await this.SessionInfo.CommandChannel.SendMessageAsync(nowPlayingEmbed);
				Console.WriteLine(this.LavalinkPlayer.CurrentState.PlaybackPosition);
			}
		}
		
		private async Task Lavalink_PlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
		{
			this.SessionInfo.CurrentTrack = null;
			this.SessionInfo.CurrentlyPlaying = false;
			if (this.SessionInfo.SessionQueue.Count > 0 && this.SessionInfo.PlaybackMode is PlaybackMode.None or PlaybackMode.Shuffle)
			{
				lock (_lock) this.SessionInfo.SessionQueue.RemoveAt(0);
			}
			await this.PlayNextTrackAsync();
		}

		private async Task Lavalink_TrackException(LavalinkGuildConnection sender, TrackExceptionEventArgs e)
			=> await this.SessionInfo.CommandChannel.SendMessageAsync(
				$"A problem with the player occurred while playing {Formatter.InlineCode(Formatter.Sanitize(e.Track.Title))}:\n\n" +
				Formatter.InlineCode($"{e.Error}\n\nTrack Information\nTrack title: {e.Track.Title}\nTrack author: {e.Track.Author}\nTrack position: {e.Track.Position}\nTrack length: {e.Track.Length}\nTrack uri: {e.Track.Uri}"));

		public async Task AddTracksAsync(MelodyTrack[] tracks)
		{
			lock (_lock) this.SessionInfo.SessionQueue.AddRange(tracks);
			await this.PlayNextTrackAsync();
		}

		public async Task RemoveTrack(int index)
		{
			if (index == 0)
			{
				if (this.SessionInfo.PlaybackMode is not PlaybackMode.None or PlaybackMode.Shuffle)
					this.SessionInfo.SessionQueue.RemoveAt(0);
				await this.LavalinkPlayer.StopAsync();
			}
			else
			{
				lock (_lock) this.SessionInfo.SessionQueue.RemoveAt(index);
			}
		}
		
		public async Task ClearTracksAsync()
		{
			lock (_lock) this.SessionInfo.SessionQueue.Clear();
			Console.WriteLine("cleared");
			if (this.LavalinkPlayer is not null && this.LavalinkPlayer.IsConnected && this.SessionInfo.CurrentlyPlaying)
			{
				this.SessionInfo.CurrentTrack = null;
				this.SessionInfo.CurrentlyPlaying = false;
				this.SessionInfo.PlaybackMode = PlaybackMode.None;
				await this.LavalinkPlayer.StopAsync();
				Console.WriteLine("ugh");
			}
		}

		public async Task PauseAsync()
		{
			this.SessionInfo.CurrentlyPlaying = false;
			await this.LavalinkPlayer.PauseAsync();
		}

		public async Task ResumeAsync()
		{
			this.SessionInfo.CurrentlyPlaying = true;
			await this.LavalinkPlayer.ResumeAsync();
		}

		public async Task<int> SetPlayerVolumeAsync(int volume)
		{
			int currentPlayerVolume = this.SessionInfo.Volume;
			if (currentPlayerVolume != volume)
			{
				this.SessionInfo.Volume = volume;
				await this.LavalinkPlayer.SetVolumeAsync(volume);
			}
			return currentPlayerVolume;
		}

		public Task SetPlaybackModeAsync(PlaybackMode playbackMode)
		{
			this.SessionInfo.PlaybackMode = this.SessionInfo.PlaybackMode == playbackMode ? PlaybackMode.None : playbackMode;
			if (playbackMode is PlaybackMode.Shuffle)
			{
				// Shuffle if this.PlayerInfo.PlaybackMode == playbackMode, unshuffle otherwise.
			}
			return Task.CompletedTask;
		}

		public async Task SkipAsync() => await this.LavalinkPlayer.StopAsync();
	}
}