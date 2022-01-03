using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Melody.Utilities;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Melody.Services
{
	public sealed class SpotifyService
	{
		private EmbedIOAuthServer AuthServer { get; set; }
		private SpotifyClientConfig SpotifyClientConfig { get; }
		private MelodySpotifyConfiguration Configuration { get; }

		public SpotifyService(MelodySpotifyConfiguration config, SpotifyClientConfig spotifyClientConfig)
		{
			this.Configuration = config;
			this.SpotifyClientConfig = spotifyClientConfig;
		}

		public async Task<SpotifyClient> BuildSpotifyClient()
		{
			var request = new ClientCredentialsRequest(this.Configuration.ClientId, this.Configuration.ClientSecret);
			var response = await new OAuthClient(this.SpotifyClientConfig).RequestToken(request);
			return new SpotifyClient(this.SpotifyClientConfig.WithToken(response.AccessToken));
		}

		public async Task<Uri> AuthenticateUser()
		{
			this.AuthServer = new EmbedIOAuthServer(new Uri($"http://{this.Configuration.Hostname}:{this.Configuration.Port}/callback"), this.Configuration.Port);
			this.AuthServer.AuthorizationCodeReceived += AuthServer_AuthorizationCodeReceived;
			this.AuthServer.ErrorReceived += AuthServer_ErrorReceived;
			
			await this.AuthServer.Start();
			return new LoginRequest(this.AuthServer.BaseUri, this.Configuration.ClientId, LoginRequest.ResponseType.Code)
			{
				Scope = new List<string> { Scopes.PlaylistReadCollaborative, Scopes.PlaylistReadPrivate, Scopes.PlaylistModifyPrivate, Scopes.PlaylistModifyPublic, Scopes.UgcImageUpload }
			}.ToUri();
		}
		
		private async Task AuthServer_AuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
		{
			await this.AuthServer.Stop();
			this.AuthServer.AuthorizationCodeReceived -= AuthServer_AuthorizationCodeReceived;
			this.AuthServer.ErrorReceived -= AuthServer_ErrorReceived;
			this.AuthServer.Dispose();

			var tokenResponse = await new OAuthClient(SpotifyClientConfig.CreateDefault()).RequestToken(
				new AuthorizationCodeTokenRequest(
					this.Configuration.ClientId, this.Configuration.ClientSecret, response.Code,
					new Uri($"http://{this.Configuration.Hostname}:{Configuration.Port}/callback")
				)
			);

			var spotify = new SpotifyClient(tokenResponse.AccessToken);
			PrivateUser me = await spotify.UserProfile.Current();
			Console.WriteLine($"User {me.DisplayName}:{me.Id} authenticated");

			IList<SimplePlaylist> playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
			Console.WriteLine("All playlists in your account: " + string.Join(", ", playlists.Select(playlist => playlist.Name)));
		}
		
		private async Task AuthServer_ErrorReceived(object sender, string error, string state)
		{
			Console.WriteLine(state + "Authorization aborted: " + error);
			await this.AuthServer.Stop();
		}
	}
}