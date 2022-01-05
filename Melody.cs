using System;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using Melody.Data.Attributes;
using Melody.Utilities;
using Melody.Data.Converters;
using Melody.Data.Exceptions;
using Melody.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SpotifyAPI.Web;

namespace Melody
{
	public sealed class Melody
	{
		private DiscordClient Discord { get; }
		private IServiceProvider Services { get; }
		private CommandsNextExtension CommandsNext { get; }

		public Melody(MelodyConfiguration config)
		{
			this.Discord = new DiscordClient(new DiscordConfiguration
			{
				TokenType = TokenType.Bot,
				Token = config.DiscordConfiguration.Token,
				Intents = DiscordIntents.Guilds | DiscordIntents.GuildMessages | DiscordIntents.GuildVoiceStates
			});
			
			this.Discord.GuildAvailable += this.Discord_GuildAvailable;
			this.Discord.GuildDownloadCompleted += this.Discord_GuildDownloadCompleted;

			this.Services = new ServiceCollection()
				.AddSingleton(this.Discord)
				.AddSingleton(config.DiscordConfiguration)
				.AddSingleton(config.LavalinkConfiguration)
				.AddSingleton(config.PostgresConfiguration)
				.AddSingleton(config.RedisConfiguration)
				.AddSingleton(config.SpotifyConfiguration)
				.AddSingleton(new YoutubeService(config.YoutubeConfiguration))
				.AddSingleton(SpotifyClientConfig.CreateDefault().WithRetryHandler(new SimpleRetryHandler()
				{
					RetryAfter = TimeSpan.FromSeconds(1)
				}))
				.AddScoped<SpotifyService>()
				.AddDbContextFactory<PostgresClientService>(options
					=> options
						//.UseLazyLoadingProxies()
						.UseNpgsql(new NpgsqlConnectionStringBuilder
						{
							Host = config.PostgresConfiguration.Hostname,
							Port = config.PostgresConfiguration.Port,
							Database = config.PostgresConfiguration.Database,
							Username = config.PostgresConfiguration.Username,
							Password = config.PostgresConfiguration.Password,
							//SslMode = config.PostgresConfiguration.UseEncryption ? SslMode.Require : SslMode.Disable,
							//TrustServerCertificate = config.PostgresConfiguration.TrustServerCertificate
						}.ConnectionString).LogTo(Console.WriteLine, LogLevel.Information))
				.AddDbContext<PostgresClientService>()
				.AddSingleton<RedisClientService>()
				.AddSingleton<LavalinkService>()
				.AddSingleton<SessionService>()
				.BuildServiceProvider(true);
			
			this.CommandsNext = this.Discord.UseCommandsNext(new CommandsNextConfiguration
			{
				EnableDms = false,
				Services = this.Services,
				//EnableDefaultHelp = false,
				StringPrefixes = config.DiscordConfiguration.DefaultPrefixes,
				EnableMentionPrefix = config.DiscordConfiguration.EnableMentionPrefix,
			});
			
			this.CommandsNext.CommandExecuted += CommandsNext_CommandExecuted;
			this.CommandsNext.CommandErrored += CommandsNext_CommandError;
			
			this.CommandsNext.RegisterConverter(new SearchRequestTypesConverter());
			this.CommandsNext.RegisterConverter(new MelodySearchProviderConverter());
			this.CommandsNext.RegisterCommands(Assembly.GetExecutingAssembly());
			//this.CommandsNext.SetHelpFormatter<HelpFormatter>();

			this.Discord.UseInteractivity();
			this.Discord.UseLavalink();
		}

		public async Task StartAsync()
		{
			this.Discord.Logger.LogInformation("Starting Melody...");
			await this.Discord.ConnectAsync();
			await Task.Delay(-1);
		}

		private Task Discord_GuildDownloadCompleted(DiscordClient sender, GuildDownloadCompletedEventArgs e)
		{
			sender.Logger.LogInformation($"[{sender.CurrentUser.Username}] [Guild Download] - All guilds have been successfully downloaded! ({e.Guilds.Count})");
			return Task.CompletedTask;
		}

		private Task Discord_GuildAvailable(DiscordClient sender, GuildCreateEventArgs e)
		{
			sender.Logger.LogInformation($"[{sender.CurrentUser.Username}] [Guild Available] - {e.Guild.Name} ({e.Guild.Id})");
			return Task.CompletedTask;
		}
		
		private static Task CommandsNext_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
		{
			sender.Client.Logger.LogInformation($"[{e.Context.User.Username}#{e.Context.User.Discriminator}] [{e.Context.User.Id}] [Command Executed] - {e.Command.QualifiedName} in #{e.Context.Channel.Name} ({e.Context.Channel.Id})");
			return Task.CompletedTask;
		}
		
		private static async Task CommandsNext_CommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
		{
			Exception ex = e.Exception;
			while (ex is AggregateException)
			{
				ex = ex.InnerException;
			}

			if (ex is NotImplementedException)
				await e.Context.RespondAsync(e.Context.BuildDefaultEmbedComponent()
					.WithTitle("Command Not Implemented")
					.WithDescription("Sorry, this command (or a portion of the command) has not yet been fully- implemented"));

			if (ex is TrackNotFoundException trackNotFoundException)
				await e.Context.RespondAsync(e.Context.BuildDefaultEmbedComponent()
					.WithTitle("Track Not Found")
					.WithDescription(trackNotFoundException.Message));

			if (ex is ChecksFailedException failedException)
			{
				var embed = e.Context.BuildDefaultEmbedComponent()
					.WithTitle("Command Checks Failed")
					.WithDescription(Formatter.InlineCode(e.Command.Name) + " threw the following exceptions:");

				foreach (CheckBaseAttribute failedCheck in failedException.FailedChecks)
				{
					embed.AddField(failedCheck.TypeId.ToString(), failedCheck switch
					{
						CooldownAttribute cooldownAttribute => $"Command can only be executed up to {Formatter.InlineCode(cooldownAttribute.MaxUses.ToString())} times before going on a {cooldownAttribute.Reset.TotalSeconds} second cooldown! ({cooldownAttribute.GetRemainingCooldown(e.Context).TotalSeconds} seconds remain)",
						RequireVoiceChannelAttribute requireVoiceChannelAttribute => $"{DiscordEmoji.FromName(failedException.Context.Client, ":no_entry:")} I can't find a voice channel to execute that command!\n\nPlease make sure that you are in a voice channel that I can see and join, then execute the command again!", 
						RequireBotPermissionsAttribute requireBotPermissionsAttribute => $"The following bot permissions are required for the execution of this command:\n{string.Join(",\n", requireBotPermissionsAttribute.Permissions)}",
						RequireDirectMessageAttribute requireDirectMessageAttribute => "This command can only be used by direct messaging me",
						RequireGuildAttribute requireGuildAttribute => "This command can only be used in a guild",
						RequireNsfwAttribute requireNsfwAttribute => "This command can only be used within NSFW-enabled channels!",
						RequireOwnerAttribute requireOwnerAttribute => "This command is restricted only to the owner of the bot",
						RequirePermissionsAttribute requirePermissionsAttribute => $"The following permissions are required for the execution of this command:\n{string.Join(",\n", requirePermissionsAttribute.Permissions)}\n\nPlease make sure that both you and the bot have the above permissions.",
						RequirePrefixesAttribute requirePrefixesAttribute => $"Usage of this command is only permitted with the following prefixes:\n{string.Join(", ", requirePrefixesAttribute.Prefixes)}",
						RequireRolesAttribute requireRolesAttribute => $"The following roles are required for the execution of this command:\n{string.Join(", ", requireRolesAttribute.RoleNames)}",
						RequireUserPermissionsAttribute requireUserPermissionsAttribute => $"You must have the following permissions in order to execute this command:\n{string.Join(",\n", requireUserPermissionsAttribute.Permissions)}",
						_ => "this message should never appear, if you somehow come across this message... how?"
					});
				}
				await e.Context.RespondAsync(embed);
			}
		}
	}
}