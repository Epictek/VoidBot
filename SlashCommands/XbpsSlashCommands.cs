using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using Flurl.Http;
using Flurl.Util;
using LibGit2Sharp;
using LiteDB;

namespace VoidBot.Commands
{
    internal class XbpsSlashCommands : SlashCommandModule
    {
        [SlashCommand("xbps", "Search Void Linux Package Repo")]
        public async Task Xbps(InteractionContext ctx,
            [Option("searchTerm", "Search term to use to find package")]
            string searchTerm,
            [Choice("aarch64", "aarch64")]
            [Choice("aarch64-musl", "aarch64-musl")]
            [Choice("armv6l-musl", "armv6l-musl")]
            [Choice("armv7l", "armv7l")]
            [Choice("armv7l-musl", "armv7l-musl")]
            [Choice("i686", "i686")]
            [Choice("x86_64", "x86_64")]
            [Choice("x86_64-musl", "x86_64-musl")]
            [Option("arch", "arch filter")]
            string arch = "x86_64")
        {
            var interactivity = ctx.Client.GetInteractivity();


            //response may take a while so we need to do this first
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            searchTerm = searchTerm.Replace(' ', '+');

            var xqApiUrl = Config.AppSetting.GetSection("DiscordSettings")["xqApiUrl"];

            PackageListResponse resp;
            try
            {
                resp = await $"{xqApiUrl}query/{arch}?q={searchTerm}".GetJsonAsync<PackageListResponse>();
            }
            catch (FlurlHttpException ex)
            {
                //todo: make helper function for embeds
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Void Repo Search: {searchTerm}")
                    .WithDescription(ex.Message + " report this error to @Epictek#6136")
                    .WithUrl(
                        $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={searchTerm}&s=indexed")
                    .WithColor(new DiscordColor(0x478061))
                    .WithFooter(
                        "Search results xq-api",
                        "https://voidlinux.org/assets/img/void_bg.png"
                    ).Build();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                return;
            }

            if (resp.Data.Length == 0)
            {
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Void Repo Search: {searchTerm}")
                    .WithDescription("No packages found")
                    .WithUrl(
                        $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={searchTerm}&s=indexed")
                    .WithColor(new DiscordColor(0x478061))
                    .WithFooter(
                        "Search results xq-api",
                        "https://voidlinux.org/assets/img/void_bg.png"
                    ).Build();
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                return;
            }

            string links = "";
            IList<Page> pages = new List<Page>();

            Package last = resp.Data.Last();
            foreach (Package package in resp.Data)
            {
                var packageString =
                    $"[{package.Name} {package.Version}](https://github.com/void-linux/void-packages/tree/master/srcpkgs/{package.Name}) - {package.ShortDesc}";

                links += packageString + "\n";

                if (packageString.Length + links.Length > 2000 || package.Equals(last))
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle("Void Repo Search: " + searchTerm)
                        .WithDescription(links)
                        .WithUrl(
                            "https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D=" +
                            searchTerm + "&s=indexed")
                        .WithColor(new DiscordColor(0x478061))
                        .WithFooter(
                            "Search results from xq-api",
                            "https://voidlinux.org/assets/img/void_bg.png"
                        );

                    pages.Add(new Page
                    {
                        Embed = embed
                    });

                    links = "";
                }
            }

            if (pages.Count > 1)
            {
                await ctx.DeleteResponseAsync();
                await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.Interaction.User, pages);
            }
            else
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(pages.First().Embed));
            }
        }

        [SlashCommand("package", "Search Void Linux Package Info")]
        public async Task Package(InteractionContext ctx, [Option("searchTerm", "Search term to use to find package")]
            string searchTerm,
            [Choice("aarch64", "aarch64")]
            [Choice("aarch64-musl", "aarch64-musl")]
            [Choice("armv6l-musl", "armv6l-musl")]
            [Choice("armv7l", "armv7l")]
            [Choice("armv7l-musl", "armv7l-musl")]
            [Choice("i686", "i686")]
            [Choice("x86_64", "x86_64")]
            [Choice("x86_64-musl", "x86_64-musl")]
            [Option("arch", "arch filter")]
            string arch = "x86_64")
        {
            //response may take a while so we need to do this first
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var xqApiUrl = Config.AppSetting.GetSection("DiscordSettings")["xqApiUrl"];

            PackageResponse resp;
            try
            {
                resp = await $"{xqApiUrl}/packages/{arch}/{searchTerm}".GetJsonAsync<PackageResponse>();
            }
            catch (FlurlHttpException ex)
            {
                if (ex.Call.HttpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"Void Repo package: {searchTerm}")
                        .WithDescription("Package not found")
                        .WithUrl(
                            $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={searchTerm}&s=indexed")
                        .WithColor(new DiscordColor(0x478061))
                        .WithFooter(
                            "Search results xq-api",
                            "https://voidlinux.org/assets/img/void_bg.png"
                        ).Build();
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
                else
                {
                    //todo: make helper function for embeds
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"Void Repo Search: {searchTerm}")
                        .WithDescription(ex.Message + " report this error to @Epictek#6136")
                        .WithUrl(
                            $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={searchTerm}&s=indexed")
                        .WithColor(new DiscordColor(0x478061))
                        .WithFooter(
                            "Search results xq-api",
                            "https://voidlinux.org/assets/img/void_bg.png"
                        ).Build();
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
                    return;
                }
            }

            var em = new DiscordEmbedBuilder()
                .WithTitle($"Void Repo Package: {resp.Data.Name}")
                .WithUrl(
                    $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={resp.Data.Name}&s=indexed")
                .WithColor(new DiscordColor(0x478061))
                .WithFooter(
                    "Search results xq-api",
                    "https://voidlinux.org/assets/img/void_bg.png"
                );

            foreach (var val in resp.Data.ToKeyValuePairs())
                if (val.Value is Array)
                {
                    //em.AddField(val.Key, string.Join(", ", (string[]) val.Value), false);
                }
                else if (val.Value is string value)
                {
                    em.AddField(val.Key, value, true);
                }

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(em.Build()));
        }


        [SlashCommand("tldr", "find tldr page")]
        public async Task tldr(InteractionContext ctx, [Option("package", "Search term to use to find tldr pages")]
            string searchTerm)
        {
            //response may take a while so we need to do this first
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            searchTerm = searchTerm.ToLower();
            var tldrUrl = $"https://raw.githubusercontent.com/tldr-pages/tldr/master/pages/common/{searchTerm}.md";
            var resp = "";
            try
            {
                resp = await tldrUrl.GetStringAsync();
            }
            catch (FlurlHttpException ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("No tldr page found"));
                return;
            }

            var lines = resp.Replace("\n\n", "\n").Split("\n");
            var embed = new DiscordEmbedBuilder()
                .WithTitle($"TLDR page: {lines.FirstOrDefault()?.Remove(0, 2)}")
                .WithColor(new DiscordColor(0x478061))
                .WithDescription(string.Join("\n", lines.Skip(1)))
                .Build();
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
        }

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


        [SlashCommandGroup("xlocate", "xlocate commands")]
        public class XlocateGroup
        {
            [SlashCommand("search", "Locate files in all XBPS packages")]
            public async Task Search(InteractionContext ctx, [Option("searchTerm", "locate files in all XBPS packages")]
                string searchTerm)
            {
                //response may take a while so we need to do this first
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var resp = "";
                if (!Directory.Exists("tmp/xlocate"))
                {
                    await ctx.EditResponseAsync(
                        new DiscordWebhookBuilder().WithContent("No database found please update"));
                    return;
                }

                var files = Directory.GetFiles("tmp/xlocate");
                foreach (var file in files)
                {
                    // if (file.Contains(".git"))
                    // {
                    //     continue;
                    // }

                    Console.WriteLine(file);
                    var pkg = Path.GetFileName(file);

                    foreach (var line in File.ReadLines(file))
                    {
                        if (!line.Contains(searchTerm)) continue;

                        if (resp.Length + line.Length + pkg.Length + 1 + Environment.NewLine.Length > 1994) break;

                        resp += $"{pkg}:{line}{Environment.NewLine}";
                    }
                }

                if (!string.IsNullOrEmpty(resp))
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                        $"```{resp}```"));
                    return;
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not find files"));
            }


            [SlashCommand("update", "update xlocate cache")]
            public async Task Update(InteractionContext ctx)
            {
                //response may take a while so we need to do this first
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                if (!Directory.Exists("tmp/xlocate") && Directory.GetFiles("tmp/xlocate").Length > 0)
                {
                    var clone = Repository.Clone("https://alpha.de.repo.voidlinux.org/xlocate/xlocate.git",
                        "tmp/xlocate");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(clone));
                }

                using var repo = new Repository("tmp/xlocate");
                // Credential information to fetch
                PullOptions options = new();
                options.FetchOptions = new FetchOptions();
                var signature = new Signature(
                    new Identity("VoidBot", "kieran@coldron.com"), DateTimeOffset.Now);

                try
                {
                    var pull = LibGit2Sharp.Commands.Pull(repo, signature, options);

                    switch (pull.Status)
                    {
                        case MergeStatus.UpToDate:
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache up to date"));
                            break;
                        case MergeStatus.FastForward:
                            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Cache updated"));
                            break;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Already up to date"));
                }
            }
        }


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
            
            
        }


        [SlashCommandGroup("warning", "warning commands")]
        public class Warnings
        {
            [SlashCommand("list", "List the warnings for a user")]
            public async Task ListWarnings(InteractionContext ctx,
                [Option("user", "The user to warn")] DiscordUser user = null!)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);


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

                var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
                var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.KickMembers) != 0;

                if (!perms || user.Id == ctx.User.Id)
                {
                    await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                        $"{ctx.User.Mention} tried to warn {user.Mention} for reason: \"{reason}\" but was denied\n(►˛◄’!)");
                    await ctx.DeleteResponseAsync();
                    return;
                }

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

            [SlashCommand("remove", "Remove user a warning")]
            public async Task RemoveWarnings(InteractionContext ctx,
                [Option("id", "id of the warning")] long warningId)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

                var usr = await ctx.Guild.GetMemberAsync(ctx.User.Id);
                var perms = (usr.PermissionsIn(ctx.Channel) & Permissions.KickMembers) != 0;

                if (!perms)
                {
                    await ctx.Client.SendMessageAsync(ctx.Guild.GetChannel(607392574235344928),
                        $"{ctx.User.Mention} tried to remove warning {warningId} but was denied\n(►˛◄’!)");
                    await ctx.DeleteResponseAsync();
                    return;
                }

                using var db = new LiteDatabase(@$"{ctx.Guild.Id}.db");
                var col = db.GetCollection<Warning>("warnings");

                col.Delete(warningId);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("warning removed"));
            }
        }
    }
}