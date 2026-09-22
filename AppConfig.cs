namespace SamedisExternalSync
{

  public class AppConfig
  {
    public AuthConfig Auth { get; set; } = new AuthConfig();
    public SamedisConfig Samedis { get; set; } = new SamedisConfig();
    public PathsConfig Paths { get; set; } = new PathsConfig();
    public FormattingConfig Formatting { get; set; } = new FormattingConfig();
    public LoggingConfig Logging { get; set; } = new LoggingConfig();
    public HttpConfig Http { get; set; } = new HttpConfig();
    public SyncSettings Sync { get; set; } = new SyncSettings();
    public InventoriesConfig Inventories { get; set; } = new InventoriesConfig();
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
    public string? WebUri { get; set; } = "https://app.samedis.care";
    public string? ApiVersion { get; set; }
    public string? TenantId { get; set; }
  }

  public class PathsConfig
  {
    public string? FromSamedis { get; set; } = "data/from_samedis";
    public string? ToSamedis { get; set; } = "data/to_samedis";
  }



  public class FormattingConfig
  {
    public string? DecimalSeparator { get; set; } = ",";
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

    public bool DepartmentsDownload { get; set; } = false;
    public bool DepartmentsUpload { get; set; } = false;
    public bool LocationsDownload { get; set; } = false;
    public bool LocationsUpload { get; set; } = false;

    public bool InventoriesDownload { get; set; } = false;
    public bool InventoriesUpload { get; set; } = false;
    public bool InventoriesUploadFallbackByDeviceNumber { get; set; } = false;
    public bool CreateLocalDeviceModelsOnInventoryLookup { get; set; } = false;
    public bool InventoriesUploadCreateDepartmentsOnTheFly { get; set; } = false;
    public bool InventoriesUploadCreateLocationsOnTheFly { get; set; } = false;
    public bool InventoriesUploadResolveServicePartnerCompany { get; set; } = false;
    public string? LocationsRoomPlaceholder { get; set; } = "Keine Raumzuordnung";
    public string? LocationsFloorPlaceholder { get; set; } = "Keine Ebenenzuordnung";
    public bool TasksDownload { get; set; } = false;
    public bool TasksUpload { get; set; } = false;
    public bool TasksUploadSetInventoryOperationStatusOnFailedMaintenance { get; set; } = false;
    public string? TaskDownloadTypes { get; set; } = "maintenance";
    public bool TaskArchiveFilter { get; set; } = true;
    public string? TaskDownloadStatus { get; set; } = "done";
    public bool RequestsDownload { get; set; } = false;
    public bool RequestsUpload { get; set; } = false;
    public bool Trainings { get; set; } = false;
    public bool ArchiveToSamedisCsvFiles { get; set; } = true;
  }

  public class InventoriesConfig
  {
    public CatalogLookupConfig CatalogLookup { get; set; } = new CatalogLookupConfig();
  }

  /// <summary>
  /// Resolving an inventory row's device model from a key the source system carries in a
  /// column of its own, instead of from the title and manufacturer.
  /// </summary>
  public class CatalogLookupConfig
  {
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// The columns to try, in order. Off by default: a facility that does not carry such a
    /// key in its export must not pay a request per row for it.
    /// </summary>
    public List<CatalogLookupMapping> Mappings { get; set; } = new List<CatalogLookupMapping>();
  }

  /// <summary>
  /// One CSV column and the field of the device model it is matched against. Exactly one of
  /// <see cref="Field"/> and <see cref="Regulatory"/> is set; <see cref="CatalogLookup"/>
  /// rejects the configuration otherwise rather than guessing.
  /// </summary>
  public class CatalogLookupMapping
  {
    public string? Column { get; set; }

    /// <summary>A plain field of the device model. Only <c>external_id</c> is supported.</summary>
    public string? Field { get; set; }

    /// <summary>A key inside the device model's <c>regulatory</c> hash, e.g. <c>emtec_code</c>.</summary>
    public string? Regulatory { get; set; }

    /// <summary>
    /// Whether the CSV value is upper-cased before it is sent. The server matches exactly and
    /// case-sensitively, and emtec codes are stored upper-cased, so a source that exports them
    /// in lower case finds nothing without this.
    /// </summary>
    public bool Upcase { get; set; } = false;
  }
}
