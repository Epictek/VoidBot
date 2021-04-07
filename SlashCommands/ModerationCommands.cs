using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace VoidBot.Commands
{
    public class ModerationCommands : SlashCommandModule
    {
        [SlashCommand("massban", "Bans multiple users")]
        public async Task MassBan(InteractionContext ctx,
            [Option("users", "space separated list of ids to ban")]
            string users, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;


            if (!perms || users.Contains(ctx.User.Id.ToString()))
            {
                await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                    $"{ctx.User.Mention} tried to use the ban command on {users} for reason {reason} but was denied\n(►˛◄’!)");
                await ctx.DeleteResponseAsync();

                return;
            }

            var userList = users.Split(' ').Select(ulong.Parse);

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
                $"{userList.Count() - failCount} users have been banned. {(failCount > 0 ? $"Failed to ban {failCount} users" : "")} "));
        }


        [SlashCommand("ban", "Bans user")]
        public async Task Ban(InteractionContext ctx,
            [Option("user", "User to ban")] DiscordUser user, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

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

        [SlashCommand("unban", "Bans user")]
        public async Task UnBan(InteractionContext ctx,
            [Option("user", "User to unban")] DiscordUser user, [Option("reason", "reason for ban")] string reason)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.BanMembers) != 0;


            if (!perms || user.Id == ctx.User.Id)
            {
                await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                    $"{ctx.User.Mention} tried to use the unban command on {user.Mention} for reason {reason} but was denied\n(►˛◄’!)");
                await ctx.DeleteResponseAsync();
                return;
            }

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


    }
}