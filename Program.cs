using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Data;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Humanizer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VoidBot.Commands;
using VoidBot.SlashCommands;

namespace VoidBot
{
    public class Program
    {
        static DiscordClient _discord;
        private static Timer StatusTimer;
        public static async Task Main(string[] args)
        {
            var discordConfig = Config.AppSetting.GetSection("DiscordSettings");
            
            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = discordConfig["token"],
                TokenType = TokenType.Bot,
                MinimumLogLevel = LogLevel.Debug,
                Intents = DiscordIntents.All
            });
            

            var services = new ServiceCollection()
                .AddHttpClient()
                .BuildServiceProvider();

            _discord.UseInteractivity(new InteractivityConfiguration
            {
            });
            
            var slash = _discord.UseSlashCommands(new SlashCommandsConfiguration()
            {
                Services = services
            });
            
             slash.RegisterCommands<XbpsSlashCommands>(323558395414183936);
             slash.RegisterCommands<LevelCommands>(323558395414183936);
             slash.RegisterCommands<ModerationCommands>(323558395414183936);
             slash.RegisterCommands<MiscCommands>(323558395414183936);

            await _discord.ConnectAsync();

            slash.SlashCommandErrored += async (sender, e) => _discord.Logger.LogError(e.Exception.ToString());

            _discord.Ready += (sender, eventArgs) =>
            {
                _ = Task.Run(UpdateBotStatus);
                return Task.CompletedTask;
            };

            
            LevelingSystem.Init(_discord);
            DiscordLogger.Init(_discord);
            await Task.Delay(-1);
        }

        private static async Task UpdateBotStatus()
        {
            StatusTimer = new Timer(async _ =>
                {
                    using var proc = Process.GetCurrentProcess();
                    await _discord.UpdateStatusAsync(new DiscordActivity(
                        $"Floating in the void for {DateTime.Now.Subtract(proc.StartTime).Humanize()}." +
                              $" Eating {proc.PrivateMemorySize64.Bytes().Humanize("0")}"));
                },
                null,
                TimeSpan.FromSeconds(1), //time to wait before executing the timer for the first time (set first status)
                TimeSpan.FromMinutes(3)
            );
        }

        
    }
    
}