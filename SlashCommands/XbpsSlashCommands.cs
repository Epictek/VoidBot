using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using Flurl.Http;
using Flurl.Util;
using LibGit2Sharp;
using LiteDB;

namespace VoidBot.Commands
{
    class GetArchs : IChoiceProvider
    {
        public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
        {
            var httpClient = new HttpClient();
            var archList = await httpClient.GetFromJsonAsync<ArchResponse>("https://xq-api.voidlinux.org/v1/archs");
            if (archList == null || !archList.Data.Any())
            {
                return Enumerable.Empty<DiscordApplicationCommandOptionChoice>();
            }
            return archList.Data.Select(x => new DiscordApplicationCommandOptionChoice(x, (string)x)).ToArray();
        }
    }
 
    internal class XbpsSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("xbps", "Search Void Linux Package Repo")]
        public async Task Xbps(InteractionContext ctx,
            [Option("query", "Search term to use to find package")]
            string query,
            [ChoiceProvider(typeof(GetArchs))]
            [Option("arch", "arch filter")]
            string arch = "x86_64")
        {
            var interactivity = ctx.Client.GetInteractivity();

            
            //response may take a while so we need to do this first
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            query = query.Replace(' ', '+');

            var xqApiUrl = Config.AppSetting.GetSection("DiscordSettings")["xqApiUrl"];

            PackageListResponse resp;
            try
            {
                resp = await $"{xqApiUrl}query/{arch}?q={query}".GetJsonAsync<PackageListResponse>();
            }
            catch (FlurlHttpException ex)
            {
                //todo: make helper function for embeds
                var embed = new DiscordEmbedBuilder()
                    .WithTitle($"Void Repo Search: {query}")
                    .WithDescription(ex.Message + " report this error to @Epictek#6136")
                    .WithUrl(
                        $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={query}&s=indexed")
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
                    .WithTitle($"Void Repo Search: {query}")
                    .WithDescription("No packages found")
                    .WithUrl(
                        $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={query}&s=indexed")
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
                        .WithTitle("Void Repo Search: " + query)
                        .WithDescription(links)
                        .WithUrl(
                            "https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D=" +
                            query + "&s=indexed")
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
        public async Task Package(InteractionContext ctx, [Option("query", "Search term to use to find package")]
            string query,
            [ChoiceProvider(typeof(GetArchs))]
            [Option("arch", "arch filter")]
            string arch = "x86_64")
        {
            //response may take a while so we need to do this first
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var xqApiUrl = Config.AppSetting.GetSection("DiscordSettings")["xqApiUrl"];

            PackageResponse resp;
            try
            {
                resp = await $"{xqApiUrl}/packages/{arch}/{query}".GetJsonAsync<PackageResponse>();
            }
            catch (FlurlHttpException ex)
            {
                if (ex.Call.HttpResponseMessage.StatusCode == HttpStatusCode.NotFound)
                {
                    var embed = new DiscordEmbedBuilder()
                        .WithTitle($"Void Repo package: {query}")
                        .WithDescription("Package not found")
                        .WithUrl(
                            $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={query}&s=indexed")
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
                        .WithTitle($"Void Repo Search: {query}")
                        .WithDescription(ex.Message + " report this error to @Epictek#6136")
                        .WithUrl(
                            $"https://github.com/void-linux/void-packages/search?q%5B%5D=filename%3Atemplate+path%3A%2Fsrcpkgs&q%5B%5D={query}&s=indexed")
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
        
        
         [SlashCommandGroup("xlocate", "xlocate commands")]
         public class XlocateGroup
         {
             [SlashCommand("search", "Locate files in all XBPS packages")]
             public async Task Search(InteractionContext ctx, [Option("query", "locate files in all XBPS packages")]
                 string query)
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
                         if (!line.Contains(query)) continue;
        
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
    }
}