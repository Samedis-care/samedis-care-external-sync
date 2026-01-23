using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SamedisExternalSync
{

  public class AppConfig
  {
    public AuthConfig Auth { get; set; } = new AuthConfig();
    public SamedisConfig Samedis { get; set; } = new SamedisConfig();
    public LoggingConfig Logging { get; set; } = new LoggingConfig();
    public HttpConfig Http { get; set; } = new HttpConfig();
    public SyncSettings Sync { get; set; } = new SyncSettings();

    public static AppConfig LoadFromYaml(string filePath)
    {
      using var input = File.OpenText(filePath);
      var deserializerBuilder = new DeserializerBuilder().WithNamingConvention(UnderscoredNamingConvention.Instance);
      var deserializer = deserializerBuilder.Build();
      var result = deserializer.Deserialize<AppConfig>(input);
      return result;
    }
  }

  public class AuthConfig
  {
    public string? Uri { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
  }

  public class SamedisConfig
  {
    public string? Uri { get; set; }
    public string? ApiVersion { get; set; }
    public string? TenantId { get; set; }
  }



  public class LoggingConfig
  {
    public int Level { get; set; }
    public int Mode { get; set; }
  }

  public class HttpConfig
  {
    public bool ValidCertificate { get; set; }
    public string? Proxy { get; set; }
    public string? ProxyUsername { get; set; }
    public string? ProxyPassword { get; set; }
  }

  public class SyncSettings
  {
    public bool DeviceTypes { get; set; } = false;
    public bool DeviceModels { get; set; } = true;
    public bool Contacts { get; set; } = false;

    public bool InventoriesDownload { get; set; } = false;
    public bool InventoriesUpload { get; set; } = false;
    public bool TasksDownload { get; set; } = false;
    public bool TasksUpload { get; set; } = false;
    public bool TicketsDownload { get; set; } = false;
    public bool TicketsUpload { get; set; } = false;
    public bool Trainings { get; set; } = false;
  }
}