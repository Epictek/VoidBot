using System;
using Newtonsoft.Json;

namespace VoidBot
{
    public class PackageListResponse
    {
        public Package[] Data { get; set; }
    }

    public class Package
    {
        [JsonProperty("name")] public string Name { get; set; }

        [JsonProperty("version")] public string Version { get; set; }

        [JsonProperty("revision")] public long Revision { get; set; }

        [JsonProperty("filename_size")] public long FilenameSize { get; set; }

        [JsonProperty("repository")] public string Repository { get; set; }
        [JsonProperty("short_desc")] public string ShortDesc { get; set; }
    }
    
    public class ArchResponse
    {
        [JsonProperty("data")]
        public string[] Data { get; set; }
    }

    
    public class PackageResponse
    {
        [JsonProperty("data")]
        public PackageDetailed Data { get; set; }
    }

    public class PackageDetailed : Package
    {

        [JsonProperty("architecture")]
        public string Architecture { get; set; }

        [JsonProperty("build_date")]
        public DateTime BuildDate { get; set; }

        [JsonProperty("build_options")]
        public string BuildOptions { get; set; }

        [JsonProperty("filename_sha256")]
        public string FilenameSha256 { get; set; }
        
        [JsonProperty("homepage")]
        public Uri Homepage { get; set; }

        [JsonProperty("installed_size")]
        public long InstalledSize { get; set; }

        [JsonProperty("license")]
        public string License { get; set; }

        [JsonProperty("maintainer")]
        public string Maintainer { get; set; }
        
        [JsonProperty("source_revisions")]
        public string SourceRevisions { get; set; }

        [JsonProperty("run_depends")]
        public string[] RunDepends { get; set; }

        [JsonProperty("shlib_requires")]
        public string[] ShlibRequires { get; set; }

        [JsonProperty("conflicts")]
        public string[] Conflicts { get; set; }
    }

    
}