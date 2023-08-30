using System;
using DSharpPlus.Entities;
namespace VoidBot
{
    public class Warning
    {
        public long Id { get; set; }

        public ulong UserId { get; set; }
        public string Reason { get; set; }
        public DateTime Date { get; set; }
        public ulong Enforcer { get; set; }
    }
}