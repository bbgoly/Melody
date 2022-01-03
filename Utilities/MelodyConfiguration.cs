using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace Melody.Utilities
{
	public sealed class MelodyConfiguration
	{
		[JsonPropertyName("version")] public float Version { get; set; } = 1;
		[JsonPropertyName("discord")] public MelodyDiscordConfiguration DiscordConfiguration { get; set; } = new();
		[JsonPropertyName("lavalink")] public MelodyLavalinkConfiguration LavalinkConfiguration { get; set; } = new();
		[JsonPropertyName("redis")] public MelodyRedisConfiguration RedisConfiguration { get; set; } = new();
		[JsonPropertyName("postgres")] public MelodyPostgresConfiguration PostgresConfiguration { get; set; } = new();
		[JsonPropertyName("youtube")] public MelodyYoutubeConfiguration YoutubeConfiguration { get; set; } = new();
		[JsonPropertyName("spotify")] public MelodySpotifyConfiguration SpotifyConfiguration { get; set; } = new();
	}

	public sealed class MelodyDiscordConfiguration
	{
		[JsonPropertyName("token")] public string Token { get; set; } = string.Empty;
		[JsonPropertyName("mention_prefix")] public bool EnableMentionPrefix { get; set; } = true;
		[JsonPropertyName("default_prefixes")]
		public ImmutableArray<string> DefaultPrefixes { get; set; } = new[] { ">>" }.ToImmutableArray();
	}

	public sealed class MelodyLavalinkConfiguration
	{
		[JsonPropertyName("hostname")] public string Hostname { get; set; }
		[JsonPropertyName("password")] public string Password { get; set; }
		[JsonPropertyName("port")] public int Port { get; set; }
	}

	public sealed class MelodyRedisConfiguration
	{
		[JsonPropertyName("hostname")] public string Hostname { get; set; }
		[JsonPropertyName("password")] public string Password { get; set; }
		[JsonPropertyName("port")] public int Port { get; set; }
		[JsonPropertyName("ssl")] public bool UseEncryption { get; set; }
	}

	public sealed class MelodyPostgresConfiguration
	{
		[JsonPropertyName("hostname")] public string Hostname { get; set; }
		[JsonPropertyName("port")] public int Port { get; set; }
		[JsonPropertyName("database")] public string Database { get; set; }
		[JsonPropertyName("username")] public string Username { get; set; }
		[JsonPropertyName("password")] public string Password { get; set; }
		[JsonPropertyName("ssl")] public bool UseEncryption { get; set; }
		[JsonPropertyName("trust_certificate")] public bool TrustServerCertificate { get; set; }
	}

	public sealed class MelodyYoutubeConfiguration
	{
		[JsonPropertyName("api_key")] public string ApiKey { get; set; }
	}

	public sealed class MelodySpotifyConfiguration
	{
		[JsonPropertyName("hostname")] public string Hostname { get; set; }
		[JsonPropertyName("port")] public int Port { get; set; }
		[JsonPropertyName("client_id")] public string ClientId { get; set; }
 		[JsonPropertyName("client_secret")] public string ClientSecret { get; set; }
	}
}