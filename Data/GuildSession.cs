﻿using System;
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
		public GuildSessionInfo SessionInfo { get; }
		private LavalinkService LavalinkService { get; }
		private LavalinkGuildConnection LavalinkPlayer { get; set; }
		
		private readonly object _lock = new object();
		
		public GuildSession(ulong guildId, LavalinkService lavalinkService)
		{
			this.SessionInfo = new GuildSessionInfo(guildId);
			this.LavalinkService = lavalinkService;
		}

		public async Task ConnectPlayerAsync(DiscordChannel channel)
		{
			Console.WriteLine(this.LavalinkPlayer is not null && this.LavalinkPlayer.Channel != channel);
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
			if (this.LavalinkPlayer?.Channel is null)
			{
				Console.WriteLine("\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"RARE SHIT\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"\"");
				await this.DisconnectPlayerAsync();
			}
			if (this.SessionInfo.CurrentTrack is null && this.SessionInfo.SessionQueue.Count > 0)
			{
				MelodyTrack nextTrack = this.SessionInfo.SessionQueue[0];
				this.SessionInfo.CurrentTrack = nextTrack;
				this.SessionInfo.CurrentlyPlaying = true;
				nextTrack.TimeElapsed = DateTime.UtcNow;
				await this.LavalinkPlayer.PlayAsync(nextTrack.Track);
				
				var nowPlayingEmbed = nextTrack.RequestingMember.BuildDefaultEmbedComponent(this.LavalinkPlayer.Guild.CurrentMember)
					.WithTitle("Now Playing")
					.WithDescription($"{Formatter.Bold(nextTrack.Track.Title)}\nby {Formatter.Bold(nextTrack.Track.Author)} on **[{nextTrack.SourceProvider}]({nextTrack.TrackUrl} \"{nextTrack.TrackUrl}\")**")
					.WithImageUrl(nextTrack.DefaultThumbnail);
				await this.SessionInfo.CommandChannel.SendMessageAsync(nowPlayingEmbed);
			}

			if (!this.SessionInfo.CurrentlyPlaying)
			{
				await this.ResumeAsync();
			}
		}
		
		private async Task Lavalink_PlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs e)
		{
			this.SessionInfo.CurrentTrack = null;
			this.SessionInfo.CurrentlyPlaying = false;
			if (this.SessionInfo.SessionQueue.Count > 0 && this.SessionInfo.PlaybackMode is PlaybackMode.None or PlaybackMode.Shuffle)
			{
				lock (_lock) this.SessionInfo.SessionQueue.RemoveAt(0);
				if (this.SessionInfo.SessionQueue.Count == 0)
					await this.SessionInfo.CommandChannel.SendDefaultEmbedMessageAsync("There are no tracks left in queue, add some more!");
			}
			await this.PlayNextTrackAsync();
		}

		private async Task Lavalink_TrackException(LavalinkGuildConnection sender, TrackExceptionEventArgs e)
			=> await this.SessionInfo.CommandChannel.SendMessageAsync($"A problem with the player occurred while playing {Formatter.InlineCode(e.Track.Title)}:\n\n"
			                                                          + Formatter.InlineCode($"{e.Error}\n\nTrack Information\nTrack title: {e.Track.Title}\nTrack author: {e.Track.Author}\nTrack position: {e.Track.Position}\nTrack length: {e.Track.Length}\nTrack uri: {e.Track.Uri}"));

		public async Task AddTracksAsync(MelodyTrack[] tracks)
		{
			lock (_lock) this.SessionInfo.SessionQueue.AddRange(tracks);
			await this.PlayNextTrackAsync();
		}

		public async Task RemoveTrack(int index)
		{
			if (index > 0)
			{
				lock (_lock)
				{
					this.SessionInfo.SessionQueue.RemoveAt(index);
				}
			}
			else
			{
				await this.SkipAsync();
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
			if (this.LavalinkPlayer is not null && this.LavalinkPlayer.IsConnected)
			{
				this.SessionInfo.CurrentlyPlaying = true;
				await this.LavalinkPlayer.ResumeAsync();	
			}
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