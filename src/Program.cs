using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Commands;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.TextCommands;
using DSharpPlus.Commands.Processors.TextCommands.Parsing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using @RepositoryOwner.@RepositoryName.Configuration;
using @RepositoryOwner.@RepositoryName.Events;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using DSharpPlusDiscordConfiguration = DSharpPlus.DiscordConfiguration;
using SerilogLoggerConfiguration = Serilog.LoggerConfiguration;

namespace @RepositoryOwner.@RepositoryName
{
    public sealed class Program
    {
        public static async Task Main(string[] args)
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton(serviceProvider =>
            {
                ConfigurationBuilder configurationBuilder = new();
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddJsonFile("config.json", true, true);
#if DEBUG
                configurationBuilder.AddJsonFile("config.debug.json", true, true);
#endif
                configurationBuilder.AddEnvironmentVariables("@RepositoryName__");
                configurationBuilder.AddCommandLine(args);

                IConfiguration configuration = configurationBuilder.Build();
                @RepositoryNameConfiguration? @RepositoryNameCamel = configuration.Get<@RepositoryNameConfiguration>();
                if (@RepositoryNameCamel is null)
                {
                    Console.WriteLine("No configuration found! Please modify the config file, set environment variables or pass command line arguments. Exiting...");
                    Environment.Exit(1);
                }

                return @RepositoryNameCamel;
            });

            services.AddLogging(logging =>
            {
                IServiceProvider serviceProvider = logging.Services.BuildServiceProvider();
                @RepositoryNameConfiguration @RepositoryNameCamel = serviceProvider.GetRequiredService<@RepositoryNameConfiguration>();
                SerilogLoggerConfiguration serilogLoggerConfiguration = new();
                serilogLoggerConfiguration.MinimumLevel.Is(@RepositoryNameCamel.Logger.LogLevel);
                serilogLoggerConfiguration.WriteTo.Console(
                    formatProvider: CultureInfo.InvariantCulture,
                    outputTemplate: @RepositoryNameCamel.Logger.Format,
                    theme: AnsiConsoleTheme.Code
                );

                serilogLoggerConfiguration.WriteTo.File(
                    formatProvider: CultureInfo.InvariantCulture,
                    path: $"{@RepositoryNameCamel.Logger.Path}/{@RepositoryNameCamel.Logger.FileName}.log",
                    rollingInterval: @RepositoryNameCamel.Logger.RollingInterval,
                    outputTemplate: @RepositoryNameCamel.Logger.Format
                );

                // Sometimes the user/dev needs more or less information about a speific part of the bot
                // so we allow them to override the log level for a specific namespace.
                if (@RepositoryNameCamel.Logger.Overrides.Count > 0)
                {
                    foreach ((string key, LogEventLevel value) in @RepositoryNameCamel.Logger.Overrides)
                    {
                        serilogLoggerConfiguration.MinimumLevel.Override(key, value);
                    }
                }

                logging.AddSerilog(serilogLoggerConfiguration.CreateLogger());
            });

            services.AddSingleton((serviceProvider) =>
            {
                DiscordEventManager eventManager = new(serviceProvider);
                eventManager.GatherEventHandlers(typeof(Program).Assembly);
                return eventManager;
            });

            services.AddSingleton(serviceProvider =>
            {
                @RepositoryNameConfiguration @RepositoryNameCamel = serviceProvider.GetRequiredService<@RepositoryNameConfiguration>();
                if (@RepositoryNameCamel.Discord is null || string.IsNullOrWhiteSpace(@RepositoryNameCamel.Discord.Token))
                {
                    serviceProvider.GetRequiredService<ILogger<Program>>().LogCritical("Discord token is not set! Exiting...");
                    Environment.Exit(1);
                }

                DiscordShardedClient discordClient = new(new DSharpPlusDiscordConfiguration
                {
                    Token = @RepositoryNameCamel.Discord.Token,
                    Intents = TextCommandProcessor.RequiredIntents | SlashCommandProcessor.RequiredIntents | DiscordIntents.GuildVoiceStates | DiscordIntents.MessageContents,
                    LoggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>(),
                });

                return discordClient;
            });

            // Almost start the program
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            @RepositoryNameConfiguration @RepositoryNameCamel = serviceProvider.GetRequiredService<@RepositoryNameConfiguration>();
            DiscordShardedClient discordClient = serviceProvider.GetRequiredService<DiscordShardedClient>();
            DiscordEventManager eventManager = serviceProvider.GetRequiredService<DiscordEventManager>();

            // Register extensions here since these involve asynchronous operations
            IReadOnlyDictionary<int, CommandsExtension> commandsExtensions = await discordClient.UseCommandsAsync(new CommandsConfiguration()
            {
                ServiceProvider = serviceProvider,
                DebugGuildId = @RepositoryNameCamel.Discord.GuildId
            });

            // Iterate through each Discord shard
            foreach (CommandsExtension commandsExtension in commandsExtensions.Values)
            {
                // Add all commands by scanning the current assembly
                commandsExtension.AddCommands(typeof(Program).Assembly);

                // Add text commands (h!ping) with a custom prefix, keeping all the other processors in their default state
                await commandsExtension.AddProcessorsAsync(new TextCommandProcessor(new()
                {
                    PrefixResolver = new DefaultPrefixResolver(@RepositoryNameCamel.Discord.Prefix).ResolvePrefixAsync
                }));

                // Register event handlers for the commands extension
                eventManager.RegisterEventHandlers(commandsExtension);
            }

            // Register event handlers for the main Discord client
            eventManager.RegisterEventHandlers(discordClient);

            // Connect to Discord
            await discordClient.StartAsync();

            // Wait for commands
            await Task.Delay(-1);
        }
    }
}
