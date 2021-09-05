using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using LiteDB;

namespace VoidBot
{
    public static class LevelingSystem
    {
        private const int ExpPerMsg = 5;
        private const int BaseExpToLevelup = 5;

        
        public static void Init(DiscordClient client)
        {
            client.MessageCreated += ClientOnMessageCreated;
        }

        private static async Task ClientOnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            _ = Task.Run(async () =>
            {
                if (e.Channel is null || e.Channel.IsPrivate || e.Channel.IsNSFW || e.Author.IsBot ||
                    e.Author.IsCurrent || e.Author.IsSystem == true)
                {
                    return;
                }
                
                using var db = new LiteDatabase(@$"{e.Guild.Id}.db");
                var col = db.GetCollection<UserData>("users");

                var userData = col.FindOne(x => x.Id == e.Author.Id);
                if (userData == null)
                {
                    userData = new UserData
                    {
                        Id = e.Author.Id,
                    };
                        
                    col.Insert(userData);
                }

                userData.MessageCount++;

                userData.Exp = ExpPerMsg * userData.MessageCount;
                var newLvl = GetLvlFromExp(BaseExpToLevelup, userData.Exp);

                if( newLvl != userData.Level && newLvl > userData.Level)
                {
                    //await client.SendMessageAsync(e.Channel, $"{e.Author.Mention} you've gained the level {newLvl}");
                }
                userData.Level = newLvl;
                
                col.Update(userData);
            });
        }

        
        
        public static async Task GetLevel(InteractionContext ctx,
            DiscordUser user = null!)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
            var col = db.GetCollection<UserData>("users");
            if (user == null)
            {
                user = ctx.User;
            }
            var userData = col.FindOne(x => x.Id == user.Id);

            if (userData != null)
            {
                var embed = new DiscordEmbedBuilder();
                embed.AddField("Level", userData.Level.ToString());
                embed.AddField("Message Count", userData.MessageCount.ToString());

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            await ctx.DeleteResponseAsync();
        }
        
        
        
        
        public static async Task EditBlacklist(InteractionContext ctx, string option, DiscordChannel channel)
        {
            
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;

            if (!perms)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            using var db = new LiteDatabase(@$"global.db");
            var col = db.GetCollection<ServerSettings>("servers");

            var server = col.FindOne(x => x.Id == ctx.Guild.Id);
            if (server == null)
            {
                server = new ServerSettings()
                {
                    Id = ctx.Guild.Id
                };
                col.Insert(server);

            }

            switch (option)
            {
                case "add":
                    server.ExperienceBlacklistedChannels ??= new List<ulong>();
                    server.ExperienceBlacklistedChannels.Add(channel.Id);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channel.Mention} added"));
                    break;
                case "remove":
                    if (server.ExperienceBlacklistedChannels.Contains(channel.Id))
                    {
                        server.ExperienceBlacklistedChannels.Remove(channel.Id);
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channel.Mention} removed"));
                    }
                    else
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{channel.Mention} not blacklisted"));
                    }
                    break;
            }
            col.Update(server);
        }
        
        public static async Task ViewBlacklist(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;

            if (!perms)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            using var db = new LiteDatabase(@$"global.db");
            var col = db.GetCollection<ServerSettings>("servers");

            var server = col.FindOne(x => x.Id == ctx.Guild.Id);
            if (server == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Server not found"));

                return;
            }

            if (server.ExperienceBlacklistedChannels.Any())
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(string.Join(", ", server.ExperienceBlacklistedChannels.Select(x => $"<#{x}>"))));
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No channels blacklisted"));

            }
            

        }
        
        public static async Task ResetLevel(InteractionContext ctx, DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;

            if (!perms)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
            var col = db.GetCollection<UserData>("users");

            var userData = col.FindOne(x => x.Id == user.Id);

            if (userData != null)
            {
                userData.MessageCount = 0;
                userData.Level = 0;
                col.Update(userData);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Users level reset"));
                return;
            }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("User not found in database"));
        }
        
        public static async Task EditLevelRole(InteractionContext ctx, DiscordRole role, long level)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;

            if (!perms)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            using var db = new LiteDatabase(@$"global.db");
            var col = db.GetCollection<ServerSettings>("servers");

            var server = col.FindOne(x => x.Id == ctx.Guild.Id);
            if (server == null)
            {
                server = new ServerSettings()
                {
                    Id = ctx.Guild.Id
                };
                col.Insert(server);
            }

            if (level == -1)
            {
                if (server.RoleLevels.ContainsKey(role.Id))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role level removed"));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role level does not exist"));
                }
            } else if (level > 0)
            {

                server.RoleLevels ??= new Dictionary<ulong, long>();
                if (server.RoleLevels.TryAdd(role.Id, level))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role level added"));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Role level already exists"));
                }
            }
            else
            {
                await ctx.DeleteResponseAsync();
            }

            col.Update(server);
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetExpToLevel(long baseExp, int lvl)
        {
            //exp = base * lvl * (lvl + 1)
            return baseExp * (lvl) * (lvl + 1);
        }
        private static long GetTotalExpAtLevel(long baseExp, int lvl)
        {
            if( lvl <= 0 )
                return 0;
            if( lvl == 1 )
                return GetExpToLevel(baseExp, lvl);

            return GetExpToLevel(baseExp, lvl) + GetTotalExpAtLevel(baseExp, lvl-1);
        }

        private static int GetLvlFromExp(long baseExp, long currentExp)
        {
            int lvl = 0;
            long expAtLvl = 0;
            while( (expAtLvl += GetExpToLevel(baseExp, ++lvl)) < currentExp );
            return lvl - 1;
        }



    }
}