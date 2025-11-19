using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace SdmaProjectExplorer.Models
{
    public class Project
    {
        [YamlMember(Alias = "projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [YamlMember(Alias = "projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [YamlMember(Alias = "assetCount")]
        public int AssetCount { get; set; }

        [YamlMember(Alias = "fileCount")]
        public int FileCount { get; set; }

        [YamlMember(Alias = "totalSize")]
        public long TotalSize { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; } = string.Empty;

        [YamlMember(Alias = "createdAt")]
        public long CreatedAtTimestamp { get; set; }

        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtTimestamp).DateTime;

        [YamlMember(Alias = "createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class Asset
    {
        [YamlMember(Alias = "assetId")]
        public string AssetId { get; set; } = string.Empty;

        [YamlMember(Alias = "projectId")]
        public string ProjectId { get; set; } = string.Empty;

        [YamlMember(Alias = "projectName")]
        public string ProjectName { get; set; } = string.Empty;

        [YamlMember(Alias = "assetName")]
        public string AssetName { get; set; } = string.Empty;

        [YamlMember(Alias = "fileCount")]
        public int FileCount { get; set; }

        [YamlMember(Alias = "totalSize")]
        public long TotalSize { get; set; }

        [YamlMember(Alias = "description")]
        public string Description { get; set; } = string.Empty;

        [YamlMember(Alias = "createdAt")]
        public long CreatedAtTimestamp { get; set; }

        public DateTime CreatedAt => DateTimeOffset.FromUnixTimeSeconds(CreatedAtTimestamp).DateTime;

        [YamlMember(Alias = "createdBy")]
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class AssetFileResponse
    {
        [YamlMember(Alias = "files")]
        public List<AssetFile> Files { get; set; } = new List<AssetFile>();
    }

    public class AssetFile
    {
        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "size")]
        public long Size { get; set; }

        [YamlMember(Alias = "state")]
        public string State { get; set; } = string.Empty;

        [YamlMember(Alias = "analysisState")]
        public string AnalysisState { get; set; } = string.Empty;

        [YamlMember(Alias = "hasSuggestedMetadata")]
        public bool HasSuggestedMetadata { get; set; }

        [YamlMember(Alias = "key")]
        public string Key { get; set; } = string.Empty;

        [YamlMember(Alias = "mtime")]
        public long Mtime { get; set; }

        [YamlMember(Alias = "addedAt")]
        public long AddedAtTimestamp { get; set; }

        public DateTime AddedAt => DateTimeOffset.FromUnixTimeSeconds(AddedAtTimestamp).DateTime;

        [YamlMember(Alias = "type")]
        public string Type { get; set; } = string.Empty;
    }

    public class SdmaFileCredentials
    {
        [YamlMember(Alias = "accessKeyId")]
        public string AccessKeyId { get; set; } = string.Empty;

        [YamlMember(Alias = "secretAccessKey")]
        public string SecretAccessKey { get; set; } = string.Empty;

        [YamlMember(Alias = "sessionToken")]
        public string SessionToken { get; set; } = string.Empty;

        [YamlMember(Alias = "hash")]
        public string Hash { get; set; } = string.Empty;

        [YamlMember(Alias = "path")]
        public string Path { get; set; } = string.Empty;

        [YamlMember(Alias = "s3Bucket")]
        public string S3Bucket { get; set; } = string.Empty;

        [YamlMember(Alias = "objectKey")]
        public string ObjectKey { get; set; } = string.Empty;
    }
}