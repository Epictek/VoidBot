using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;

namespace VoidBot.Commands
{
    public class MiscCommands : SlashCommandModule
    {
        [SlashCommand("faq", "faq database")]
        public async Task Faq(InteractionContext ctx,
            [Choice("discordinstall", "discordinstall")] 
            [Option("option", "option")]
            string option,
            [Option("atuser", "user to mention")] DiscordUser user = null!)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            switch (option)
            {
                case "discordinstall":
                    var embed = new DiscordEmbedBuilder()
                    {
                        Title = Formatter.Underline(Formatter.Bold("Please do not run these commands as root")),
                        Description = "How to install discord using xbps-src \n"  +Formatter.BlockCode(@"git clone https://github.com/void-linux/void-packages
cd void-packages
./xbps-src binary-bootstrap
echo XBPS_ALLOW_RESTRICTED=yes > etc/conf
./xbps-src pkg discord
sudo xbps-install --repository=hostdir/binpkgs/nonfree discord", "bash")
                    };

                    var build = new DiscordWebhookBuilder();
                    
                    if (user != null)
                    {
                        embed.Description += $"\n{user.Mention}";
                    }
                    build.AddEmbed(embed);
                    
                    await ctx.EditResponseAsync(build);
                    return;
                    break;
                case "":
                    break;
            }
            await ctx.DeleteResponseAsync();

        }

    }
}