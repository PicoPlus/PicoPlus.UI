using System.Text.Json.Serialization;

namespace PicoPlus.Models.Services.Liara;

public class LiaraReleaseResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("currentRelease")]
    public string CurrentRelease { get; set; } = string.Empty;

    [JsonPropertyName("readyReleasesCount")]
    public int ReadyReleasesCount { get; set; }

    [JsonPropertyName("releases")]
    public List<Release> Releases { get; set; } = new();

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;
}

public class Release
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("imageName")]
    public string ImageName { get; set; } = string.Empty;

    [JsonPropertyName("projectType")]
    public string ProjectType { get; set; } = string.Empty;

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("port")]
    public int Port { get; set; }

    [JsonPropertyName("buildLocation")]
    public string BuildLocation { get; set; } = string.Empty;

    [JsonPropertyName("buildTimeout")]
    public int BuildTimeout { get; set; }

    [JsonPropertyName("maxImageLayerSize")]
    public int MaxImageLayerSize { get; set; }

    [JsonPropertyName("gitInfo")]
    public GitInfo GitInfo { get; set; } = new();

    [JsonPropertyName("client")]
    public string Client { get; set; } = string.Empty;

    [JsonPropertyName("disks")]
    public List<Disk> Disks { get; set; } = new();

    [JsonPropertyName("finishedAt")]
    public DateTime FinishedAt { get; set; }

    [JsonPropertyName("platformConfig")]
    public PlatformConfig PlatformConfig { get; set; } = new();

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;

    [JsonPropertyName("sourceAvailable")]
    public bool SourceAvailable { get; set; }
}

public class GitInfo
{
    [JsonPropertyName("branch")]
    public string? Branch { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("commit")]
    public string? Commit { get; set; }

    [JsonPropertyName("committedAt")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? CommittedAt { get; set; } // Changed to object to handle null values

    [JsonPropertyName("remote")]
    public string? Remote { get; set; }

    [JsonPropertyName("author")]
    public Author Author { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public class Author
{
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

public class Disk
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("mountTo")]
    public string MountTo { get; set; } = string.Empty;
}

public class PlatformConfig
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;
}
