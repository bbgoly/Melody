using Melody.Data;
using Microsoft.EntityFrameworkCore;

namespace Melody.Services
{
	public class PostgresClientService : DbContext
	{
		public DbSet<MelodyUser> MelodyUsers { get; set; }
		public DbSet<UserPlaylist> UserPlaylists { get; set; }

		public PostgresClientService(DbContextOptions<PostgresClientService> options) : base(options) { }
		
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<MelodyUser>()
				.Property(user => user.UserId)
				.IsRequired();

			// HasFilter("[UserId] IS NOT NULL") vs. IsRequired
			// https://docs.microsoft.com/en-us/ef/core/performance/efficient-querying#use-indexes-properly
			// Above article is excellent extremely useful for keeping postgres querying efficient and even briefly
			// talks about a good paging solution
			modelBuilder.Entity<MelodyUser>()
				.HasIndex(user => user.UserId) 
				.IsUnique();
			
			modelBuilder.Entity<MelodyUser>()
				.HasMany(user => user.Playlists)
				.WithOne(playlist => playlist.MelodyUser)
				.HasForeignKey(playlist => playlist.UserId)
				.HasPrincipalKey(playlist => playlist.UserId)
				.IsRequired();
			
			modelBuilder.Entity<UserPlaylist>()
				.Property(playlist => playlist.Title)
				.IsRequired();

			modelBuilder.Entity<UserPlaylist>()
				.HasIndex(playlist => playlist.Title);
			//.IsUnique();
			
			modelBuilder.Entity<UserPlaylist>()
				.HasMany(playlist => playlist.Tracks)
				.WithOne(track => track.Playlist)
				.HasForeignKey(playlist => playlist.UserPlaylistId)
				.IsRequired();

			modelBuilder.Entity<PlaylistTrack>()
				.Property(track => track.TrackUrl)
				.IsRequired();

			modelBuilder.Entity<PlaylistTrack>()
				.HasIndex(track => track.TrackUrl);
			//.IsUnique();
		}
	}
}