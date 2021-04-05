using System;
using Newtonsoft.Json;

namespace VoidBot
{
    public class BuilderResponse
    {
        [JsonProperty("basedir")]
        public string Basedir { get; set; }

        [JsonProperty("cachedBuilds")]
        public long[] CachedBuilds { get; set; }

        [JsonProperty("currentBuilds")]
        public long[] CurrentBuilds { get; set; }

        [JsonProperty("pendingBuilds")]
        public long PendingBuilds { get; set; }

        [JsonProperty("schedulers")]
        public string[] Schedulers { get; set; }

        [JsonProperty("slaves")]
        public string[] Slaves { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("tags")]
        public object Tags { get; set; }
    }

    
}