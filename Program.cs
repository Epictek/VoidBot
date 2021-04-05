using DSharpPlus;
using DSharpPlus.EventArgs;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Humanizer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using VoidBot.Commands;

namespace VoidBot
{
    public class Program
    {
        static CommandsNextExtension _commands;
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
                
            });

            _commands = _discord.UseCommandsNext(new CommandsNextConfiguration
            {
                EnableDms = true,
                EnableMentionPrefix = true,
                StringPrefixes = new []{discordConfig["prefix"]},
            });

            _discord.UseInteractivity(new InteractivityConfiguration
            {
            });

            var slash = _discord.UseSlashCommands();
            slash.RegisterCommands<XbpsSlashCommands>(323558395414183936);

            await _discord.ConnectAsync();

            _discord.Ready += (sender, eventArgs) =>
            {
                _ = Task.Run(UpdateStatus);
                return Task.CompletedTask;
            };

            
            LevelingSystem.Init(_discord);
            
            await Task.Delay(-1);
        }

        private static async Task UpdateStatus()
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