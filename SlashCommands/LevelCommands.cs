using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace VoidBot.Commands
{
    public class LevelCommands : ApplicationCommandModule
    {
        [SlashCommandGroup("levels", "level commands")]
        public class Levels
        {
            [SlashCommand("get", "Retrieve a users level")]
            public async Task GetLevel(InteractionContext ctx,
                [Option("user", "the user")] DiscordUser user = null!) => await LevelingSystem.GetLevel(ctx, user);


            [SlashCommand("blacklist", "Blacklist a channel")]
            public async Task BlacklistChannel(InteractionContext ctx, 
                [Choice("add", "add")]
                [Choice("remove", "remove")]
                [Option("option", "option")] string option,
                [Option("channel", "channel")] DiscordChannel channel) => await LevelingSystem.EditBlacklist(ctx, option , channel);

            [SlashCommand("view-blacklist", "Blacklist a channel")]
            public async Task BlacklistView(InteractionContext ctx) => await LevelingSystem.ViewBlacklist(ctx);

                
            [SlashCommand("reset", "Reset a users level")]
            public async Task ResetLevel(InteractionContext ctx,
                [Option("user", "the user")] DiscordUser user) => await LevelingSystem.ResetLevel(ctx, user);
            
            
            [SlashCommand("role", "edits a role level")]
            public async Task AddLevelRole(InteractionContext ctx,
                [Option("role", "role to give")] DiscordRole role, [Option("level", "level to give role at")]  long level) => await LevelingSystem.EditLevelRole(ctx, role, level);

                
                
        }

    }
}