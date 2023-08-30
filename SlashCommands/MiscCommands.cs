using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;

using DSharpPlus.SlashCommands;
using LiteDB;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using VoidBot.Helpers;

namespace VoidBot.Commands
{
    

    public class MiscCommands : ApplicationCommandModule
    {
        
        class FaqDatabase : IChoiceProvider
        {
            public async Task<IEnumerable<DiscordApplicationCommandOptionChoice>> Provider()
            {
                using var db = new LiteDatabase(@$"FAQ.db");
                var col = db.GetCollection<FaqModel>("faq");
                var faqList = col.FindAll().ToList();
                if (faqList.Any())
                {
                    return faqList.Select(x => new DiscordApplicationCommandOptionChoice(x.Command, x.Command));
                }

                return new DiscordApplicationCommandOptionChoice[]
                    {new DiscordApplicationCommandOptionChoice("empty", "empty")};

            }
        }
        
        [RequireUserPermissions(Permissions.KickMembers)]
        [SlashCommand("addfaq", "add faq to database")]
        public async Task AddFaq(InteractionContext ctx,
            [Option("command", "command")] string command,
            [Option("title", "title")] string title,
            [Option("content", "content")] string desc

            )
        {
            using var db = new LiteDatabase(@$"FAQ.db");
            var col = db.GetCollection<FaqModel>("faq");
            col.Insert(new FaqModel(command, title, desc));
            col.EnsureIndex(x => x.Id, true);
            db.Commit();
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("FAQ entry added"));
        }

        [SlashCommand("faq", "search faq database")]
        public async Task Faq(InteractionContext ctx,
            [ChoiceProvider(typeof(FaqDatabase))]
            [Option("command", "command")]
            string command,
            [Option("atuser", "user to mention")] DiscordUser user = null!)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var build = new DiscordWebhookBuilder();
            if (user != null)
            {
                build.WithContent($"\n{user.Mention}");
            }
            using var db = new LiteDatabase(@$"FAQ.db");
            var col = db.GetCollection<FaqModel>("faq");
            var faq = col.FindOne(x => x.Command == command);
            if (faq != null)
            {
                var embed = new DiscordEmbedBuilder()
                {
                    Title = Formatter.Underline(Formatter.Bold(faq.Title)),
                    Description = faq.Content.Replace("\\n", Environment.NewLine)
                };
                build.AddEmbed(embed);
            }

            await ctx.EditResponseAsync(build);
        }

        
        [SlashCommand("eval", "eval c# code")]
        public async Task Eval(InteractionContext ctx, [Option("command", "code to run")] string code)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            if (ctx.User.Id != 63306150757543936)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            var embed = new DiscordEmbedBuilder
            {
                Title = "Evaluating...",
                Color = new DiscordColor(0xD091B2)
            };
            var msg = await ctx.EditResponseAsync( new DiscordWebhookBuilder().AddEmbed(embed));

            var globals = new EvaluationEnvironment(ctx);
            var sopts = ScriptOptions.Default
                .WithImports("System", "System.Collections.Generic", "System.Diagnostics", "System.Linq", "System.Net.Http", "System.Net.Http.Headers", "System.Reflection", "System.Text", 
                             "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands", "DSharpPlus.Entities", "DSharpPlus.EventArgs", "DSharpPlus.Exceptions")
                .WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));
            
            var sw1 = Stopwatch.StartNew();
            var cs = CSharpScript.Create(code, sopts, typeof(EvaluationEnvironment));
            var csc = cs.Compile();
            sw1.Stop();
            
            if (csc.Any(xd => xd.Severity == DiagnosticSeverity.Error))
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Compilation failed",
                    Description = string.Concat("Compilation failed after ", sw1.ElapsedMilliseconds.ToString("#,##0"), "ms with ", csc.Length.ToString("#,##0"), " errors."),
                    Color = new DiscordColor(0xD091B2)
                };
                foreach (var xd in csc.Take(3))
                {
                    var ls = xd.Location.GetLineSpan();
                    embed.AddField(string.Concat("Error at ", ls.StartLinePosition.Line.ToString("#,##0"), ", ", ls.StartLinePosition.Character.ToString("#,##0")), Formatter.InlineCode(xd.GetMessage()), false);
                }
                if (csc.Length > 3)
                {
                    embed.AddField("Some errors ommited", string.Concat((csc.Length - 3).ToString("#,##0"), " more errors not displayed"), false);
                }
                await msg.ModifyAsync(embed: embed.Build());
                return;
            }

            Exception rex = null;
            ScriptState<object> css = null;
            var sw2 = Stopwatch.StartNew();
            try
            {
                css = await cs.RunAsync(globals);
                rex = css.Exception;
            }
            catch (Exception ex)
            {
                rex = ex;
            }
            sw2.Stop();

            if (rex != null)
            {
                embed = new DiscordEmbedBuilder
                {
                    Title = "Execution failed",
                    Description = string.Concat("Execution failed after ", sw2.ElapsedMilliseconds.ToString("#,##0"), "ms with `", rex.GetType(), ": ", rex.Message, "`."),
                    Color = new DiscordColor(0xD091B2),
                };
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

                return;
            }

            // execution succeeded
            embed = new DiscordEmbedBuilder
            {
                Title = "Evaluation successful",
                Color = new DiscordColor(0xD091B2),
            };
            embed.AddField("Result", css.ReturnValue != null ? css.ReturnValue.ToString() : "No value returned", false)
                .AddField("Compilation time", string.Concat(sw1.ElapsedMilliseconds.ToString("#,##0"), "ms"), true)
                .AddField("Execution time", string.Concat(sw2.ElapsedMilliseconds.ToString("#,##0"), "ms"), true);

            if (css.ReturnValue != null)
                embed.AddField("Return type", css.ReturnValue.GetType().ToString(), true);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

        }
        
        
        public sealed class EvaluationEnvironment
        {
            public InteractionContext Context { get; }

            public DiscordChannel Channel => this.Context.Channel;
            public DiscordGuild Guild => this.Context.Guild;
            public DiscordUser User => this.Context.User;
            public DiscordMember Member => this.Context.Member;
            public DiscordClient Client => this.Context.Client;

            public EvaluationEnvironment(InteractionContext ctx)
            {
                this.Context = ctx;
            }
        }
        
        [SlashCommand("bash", "run bash command")]
        public async Task EvalBash(InteractionContext ctx, [Option("command", "command to run")] string command)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

            if (ctx.User.Id != 63306150757543936)
            {
                await ctx.DeleteResponseAsync();
                return;
            }
            
            var response = await command.ExecuteWithBash();

            if (string.IsNullOrWhiteSpace(response.Result))
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(Formatter.BlockCode($"Exit {response.ExitCode}")));
                return;
            }            
            var formattedResult = Formatter.BlockCode(response.Result);
            if (formattedResult.Length > 2000)
            {
                await ctx.DeleteResponseAsync();
                await using var stringStream = response.Result.ToStream();
                await ctx.Channel.SendMessageAsync(new DiscordMessageBuilder().AddFile("bashoutput.log", stringStream));
                return;
            }
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(formattedResult));
        }
    }

    public class FaqModel
    {
        public FaqModel(string command, string title, string content)
        {
            Command = command;
            Title = title;
            Content = content;
        }
        public ObjectId Id { get; set; }
        public string Command { get; set;} 
        public string Title { get; set;} 
        public string Content { get; set;} 

    }

    public static class ShellHelper

    {
        static private Process _process;
        public static async Task<(string Result, int ExitCode)> ExecuteWithBash(this string cmd)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");
            
            using var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();

            string result = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync(new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token);

            result = new Regex(@"\x1B\[[^@-~]*[@-~]").Replace(result, "");
            
            return (result, process.ExitCode);

        }
        
        
        public static Stream ToStream(this string str)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(str);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}