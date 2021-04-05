using System.Collections.Generic;

namespace VoidBot
{
    public class ServerSettings
    {
        public ulong Id { get; set; }
        public int ExpPerMessage { get; set; }
        public List<ulong> ExperienceBlacklistedChannels { get; set; }
        public List<string> BlacklistedWords { get; set; }
        public List<string> BlacklistedUsernames { get; set; }
    }
}