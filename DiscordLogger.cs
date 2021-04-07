using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace VoidBot
{
    public static class DiscordLogger
    {
        public static void Init(DiscordClient discord)
        {
            discord.MessageUpdated += DiscordOnMessageUpdated;
        }

        private static async Task DiscordOnMessageUpdated(DiscordClient ctx, MessageUpdateEventArgs e)
        {
            var embed = new DiscordEmbedBuilder
            {
                Author = new DiscordEmbedBuilder.EmbedAuthor()
                {
                    IconUrl = e.Author.AvatarUrl,
                    Url = e.Message.JumpLink.ToString(),
                    Name = "Message Edited"
                },
                Color = default,
                Description = $"in {Formatter.Mention(e.Channel)}",
                Footer = null,
                ImageUrl = null,
                Thumbnail = null,
                Timestamp = e.Message.EditedTimestamp,
                Url = e.Message.JumpLink.ToString()
            };

            embed.AddField("User", e.Author.Mention, true);
            embed.AddField("Before", e.MessageBefore.Content);
            embed.AddField("After", e.Message.Content);

            await ctx.SendMessageAsync(e.Guild.GetChannel(607392574235344928), embed);
        }
    }
}