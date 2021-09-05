using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using LiteDB;
using VoidBot.Helpers;

namespace VoidBot.SlashCommands
{
    public class ModerationCommands : ApplicationCommandModule
    {
        [RequireUserPermissions(Permissions.BanMembers)]
        [SlashCommand("massban", "Bans multiple users")]
        public async Task MassBan(InteractionContext ctx,
            [Option("users", "space separated list of ids to ban")]
            string users, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

            var userList = users.Split(new Char [] {',' , '\n', ' ' }, 
                    StringSplitOptions.RemoveEmptyEntries).Select(ulong.Parse).ToList();

            var failCount = 0;

            foreach (var user in userList)
                try
                {
                    await ctx.Guild.BanMemberAsync(user, 7, reason);
                }
                catch (Exception e)
                {
                    failCount++;
                }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                $"{userList.Count - failCount} users have been banned. {(failCount > 0 ? $"Failed to ban {failCount} users" : "")} "));
        }

        [RequireUserPermissions(Permissions.BanMembers)]
        [SlashCommand("ban", "Bans user")]
        public async Task Ban(InteractionContext ctx,
            [Option("user", "User to ban")] DiscordUser user, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;


            if (!perms || user.Id == ctx.User.Id)
            {
                await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                    $"{ctx.User.Mention} tried to use the ban command on {user.Mention} for reason {reason} but was denied\n(►˛◄’!)");
                await ctx.DeleteResponseAsync();

                return;
            }


            try
            {
                await ctx.Guild.BanMemberAsync(user.Id, 7, reason);
            }
            catch (Exception e)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Failed to ban {user.Mention}."));
                return;
            }

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"{user.Mention} has been banned."));
        }

        [RequireUserPermissions(Permissions.BanMembers)]
        [SlashCommand("unban", "Bans user")]
        public async Task UnBan(InteractionContext ctx,
            [Option("user", "User to unban")] DiscordUser user, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            
            try
            {
                await ctx.Guild.UnbanMemberAsync(user.Id, reason);
            }
            catch (Exception e)
            {
                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"Failed to unban {user.Mention}."));
                return;
            }

            await ctx.EditResponseAsync(
                new DiscordWebhookBuilder().WithContent($"{user.Mention} has been unbanned."));
        }


        [SlashCommandGroup("warning", "warning commands")]
        public class Warnings
        {
            [SlashCommand("list", "List infractions for a user")]
            public async Task ListWarnings(InteractionContext ctx,
                [Option("user", "The user to warn")] DiscordUser user = null!,
                [Option("silent", "Hide response")] bool silent = true)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(silent));


                if (user == null)
                {
                    user = ctx.User;
                }
                else
                {
                    var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
                    var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.KickMembers) != 0;

                    if (!perms)
                    {
                        await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                            $"{ctx.User.Mention} tried to list warnings for {user.Mention} but was denied\n(►˛◄’!)");
                        await ctx.DeleteResponseAsync();
                        return;
                    }
                }


                using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
                var col = db.GetCollection<Warning>("warnings");

                var warnings = col.Find(x => x.UserId == user.Id);

                if (warnings.Any())
                {
                    var warningsDesc = string.Join("\n", warnings.Select(x => $"{x.Id}: {x.Reason} - {x.Date:g}"));

                    var username = user.Username ?? user.Id.ToString();

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = $"Warnings for {username}",
                        Description = warningsDesc,
                        Url = null,
                        Color = default,
                        Timestamp = null,
                        ImageUrl = null,
                        Author = null,
                        Footer = null,
                        Thumbnail = null
                    };

                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                }
                else
                {
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent($"{user.Mention} user has no warnings"));
                }
            }

            [RequireUserPermissions(Permissions.KickMembers)]
            [SlashCommand("add", "Give a user a warning")]
            public async Task AddWarnings(InteractionContext ctx,
                [Option("user", "The user to warn")] DiscordUser user,
                [Option("reason", "the reason for the warning")]
                string reason,
                [Option("silent", "whether to enform the user of the warning")]
                bool silent = false
            )
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                
                using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
                var col = db.GetCollection<Warning>("warnings");

                var warning = new Warning
                {
                    UserId = user.Id,
                    Reason = reason,
                    Date = DateTime.UtcNow,
                    Enforcer = ctx.User.Id
                };
                col.EnsureIndex(x => x.Id, true);
                col.Insert(warning);


                if (!silent)
                    try
                    {
                        var member = await ctx.Guild.GetMemberAsync(user.Id);
                        if (member != null)
                            await member.SendMessageAsync(
                                $"You were given a warning in the {ctx.Guild.Name} for {reason}");
                    }
                    catch (Exception e)
                    {
                        await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                            $"Could not DM user {user.Mention}, Either discord has fucked up or they have DMs turned off ");
                    }

                await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                    $"{ctx.User.Username} warned {user.Mention} for reason: \"{reason}\" (►˛◄’!)");


                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().WithContent($"{user.Mention} has been warned"));
            }

            [RequireUserPermissions(Permissions.KickMembers)]

            [SlashCommand("remove", "Remove a users warning")]
            public async Task RemoveWarnings(InteractionContext ctx,
                [Option("id", "id of the warning")] long warningId)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
                
                using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
                var col = db.GetCollection<Warning>("warnings");

                col.Delete(warningId);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("warning removed"));
            }
        }
    }
}