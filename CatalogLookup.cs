using System.Data;

namespace SamedisExternalSync
{
  /// <summary>
  /// The configured mapping from CSV columns onto the keys a device model can be looked up
  /// by, and the reading of one inventory row through it.
  /// </summary>
  /// <remarks>
  /// Exists because the device model an inventory row belongs to is the one field a create
  /// cannot do without: without a resolvable catalog_id the row is skipped entirely, and the
  /// fallback on title and manufacturer misses whenever the source words a model differently
  /// than the catalog does. A source that carries its own key -- an article number it sets
  /// as external_id, or a regulatory code that travels with the device -- can say which model
  /// it means instead of describing it.
  /// </remarks>
  public static class CatalogLookup
  {
    /// <summary>
    /// The plain device-model fields a mapping may name. Only external_id: it is the one
    /// non-regulatory field of a catalog that a tenant sets itself and that is both writable
    /// and filterable.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedFields = new[] { "external_id" };

    /// <summary>
    /// Returns the mappings to use, or an empty list when the lookup is switched off.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The configuration is unusable. Thrown rather than skipped, because every way of being
    /// wrong here fails silently against the server: an unknown regulatory label is sliced
    /// away and the endpoint answers with no records at all, which reads exactly like "this
    /// device model does not exist" -- the one answer that makes a sync create a duplicate.
    /// </exception>
    public static IReadOnlyList<CatalogLookupMapping> Validate(CatalogLookupConfig? config)
    {
      if (config is not { Enabled: true }) return Array.Empty<CatalogLookupMapping>();

      var mappings = config.Mappings ?? new List<CatalogLookupMapping>();
      var validated = new List<CatalogLookupMapping>();

      for (var i = 0; i < mappings.Count; i++)
      {
        var mapping = mappings[i];
        var where = $"inventories.catalog_lookup.mappings[{i}]";

        if (string.IsNullOrWhiteSpace(mapping.Column))
          throw new ArgumentException($"{where} has no column.");

        var hasField = !string.IsNullOrWhiteSpace(mapping.Field);
        var hasRegulatory = !string.IsNullOrWhiteSpace(mapping.Regulatory);

        if (hasField == hasRegulatory)
          throw new ArgumentException(
            $"{where} (column '{mapping.Column}') must name exactly one of field or regulatory, not " +
            (hasField ? "both." : "neither."));

        if (hasField && !AllowedFields.Contains(mapping.Field!.Trim()))
          throw new ArgumentException(
            $"{where}: '{mapping.Field}' is not a device model field that can be looked up. " +
            $"Valid: {string.Join(", ", AllowedFields)}.");

        if (hasRegulatory)
        {
          try
          {
            Regulatory.RequireLookupKey(mapping.Regulatory!.Trim());
          }
          catch (ArgumentException ex)
          {
            throw new ArgumentException($"{where}: {ex.Message}", ex);
          }
        }

        validated.Add(mapping);
      }

      return validated;
    }

    /// <summary>
    /// The columns a configuration reads, so the caller can say once that the CSV does not
    /// carry one of them. A missing column reads as an empty cell everywhere else, which
    /// would leave the whole lookup silently doing nothing.
    /// </summary>
    public static IReadOnlyList<string> Columns(IReadOnlyList<CatalogLookupMapping> mappings)
      => mappings.Select(m => m.Column!.Trim())
                 .Where(c => c.Length > 0)
                 .Distinct(StringComparer.OrdinalIgnoreCase)
                 .ToList();

    /// <summary>
    /// What one inventory row offers as a device-model key, in the shape
    /// <see cref="Cascades.DeviceModel"/> takes it.
    /// </summary>
    /// <param name="ExternalId">
    /// The first non-empty external_id column, in configuration order. The cascade takes one.
    /// </param>
    /// <param name="Regulatory">
    /// The regulatory identifiers the row carries, in configuration order -- which is the
    /// order they are tried in.
    /// </param>
    public sealed record Keys(string? ExternalId,
                              IReadOnlyList<(string Label, string? Value)> Regulatory)
    {
      public static readonly Keys None = new(null, Array.Empty<(string, string?)>());

      public bool Any => !string.IsNullOrWhiteSpace(ExternalId) || Regulatory.Count > 0;

      /// <summary>The keys as a log line names them, e.g. <c>emtec_code='EC-9'</c>.</summary>
      public string Describe()
      {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ExternalId)) parts.Add($"external_id='{ExternalId}'");
        parts.AddRange(Regulatory.Select(r => $"{r.Label}='{r.Value}'"));
        return parts.Count == 0 ? "none" : string.Join(", ", parts);
      }
    }

    /// <summary>
    /// Reads one row through the mappings. A blank cell and a column the CSV does not have
    /// are the same thing here -- both drop out, so a partially filled export still resolves
    /// the rows that do carry a key.
    /// </summary>
    public static Keys ValuesFrom(DataRow row, IReadOnlyList<CatalogLookupMapping> mappings)
    {
      if (mappings.Count == 0) return Keys.None;

      string? externalId = null;
      var regulatory = new List<(string Label, string? Value)>();

      foreach (var mapping in mappings)
      {
        var value = Rows.Value(row, mapping.Column!.Trim());
        if (string.IsNullOrWhiteSpace(value)) continue;
        if (mapping.Upcase) value = value.ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(mapping.Regulatory))
          regulatory.Add((mapping.Regulatory.Trim(), value));
        else if (string.IsNullOrWhiteSpace(externalId))
          externalId = value;
      }

      return externalId == null && regulatory.Count == 0
        ? Keys.None
        : new Keys(externalId, regulatory);
    }
  }
}
