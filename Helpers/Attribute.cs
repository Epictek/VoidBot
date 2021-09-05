using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.SlashCommands;

namespace VoidBot.Helpers
{
    public class RequireUserPermissionsAttribute : SlashCheckBaseAttribute
    {
        private readonly Permissions _permissions;
    
        public RequireUserPermissionsAttribute(Permissions permissions)
            => _permissions = permissions;

        public override async Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            var user = await ctx.Guild.GetMemberAsync(ctx.User.Id);
            return (user.PermissionsIn(ctx.Channel) & _permissions) != 0;
        }
    }
}