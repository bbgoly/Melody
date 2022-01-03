using System.Threading.Tasks;
using Google.Apis;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Melody.Utilities;

namespace Melody.Services
{
	public class YoutubeService
	{
		private YouTubeService YoutubeClient { get; }
		
		public YoutubeService(MelodyYoutubeConfiguration config)
		{
			this.YoutubeClient = new YouTubeService(new BaseClientService.Initializer()
			{
				ApplicationName = "Melody",
				ApiKey = config.ApiKey
			});
		}

		public async Task<SearchListResponse> SearchYoutube(string searchTerm)
		{
			var searchRequest = this.YoutubeClient.Search.List("id,snippet");
			searchRequest.ETagAction = ETagAction.IfNoneMatch;
			searchRequest.Q = searchTerm;
			searchRequest.MaxResults = 5;
			searchRequest.Fields = "items(snippet/title,snippet/channelTitle,snippet/channelId,snippet/thumbnails/medium/url,id/kind,id/playlistId,id/videoId)";
			return await searchRequest.ExecuteAsync();
		}

		public async Task<VideoListResponse> FetchVideoDetails(string[] videoIds)
		{
			var fetchRequest = this.YoutubeClient.Videos.List("snippet,id,contentDetails");
			fetchRequest.ETagAction = ETagAction.IfNoneMatch;
			fetchRequest.Id = videoIds;
			fetchRequest.Fields = "items(snippet/channelId,snippet/thumbnails/medium/url,snippet/liveBroadcastContent,snippet/localized,contentDetails/contentRating/ytRating)";
			return await fetchRequest.ExecuteAsync();
		}
		
		//public async Task<PlaylistListResponse> FetchPlaylistDetails(string[] playlistIds)
	}
}