namespace SamedisExternalSync
{
  /// <summary>
  /// Translates the ownership value a source system exports into the value the API accepts.
  /// </summary>
  /// <remarks>
  /// Exists because sources export ownership as their own label ("Eigen", "Leihe", "Miete")
  /// and the API rejects anything outside its enum with HTTP 400 -- for the whole row, not
  /// just the field (samedis-care-issues#3601). The labels differ per source, so the
  /// translation is configuration rather than code.
  /// </remarks>
  public sealed class OwnershipMapping
  {
    /// <summary>The values of <c>Inventory.ownership</c> in the backend.</summary>
    public static readonly IReadOnlyList<string> ValidTargets =
      new[] { "owned", "rental-leasing", "pay-per-use", "loaner-device", "patient" };

    private readonly IReadOnlyDictionary<string, string> _targets;
    private readonly Dictionary<string, int> _unmapped = new(StringComparer.OrdinalIgnoreCase);

    private OwnershipMapping(IReadOnlyDictionary<string, string> targets)
    {
      _targets = targets;
    }

    /// <summary>
    /// Builds the mapping from <c>inventories.ownership_mapping</c>.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// An entry has no source, names the same source twice, or maps to a target the API does
    /// not accept. Thrown at start-up rather than at first use: a wrong target would fail
    /// every row carrying that label, which is exactly what this mapping exists to prevent.
    /// </exception>
    public static OwnershipMapping Validate(IReadOnlyList<OwnershipMappingEntry>? entries)
    {
      var targets = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
      if (entries == null) return new OwnershipMapping(targets);

      for (var i = 0; i < entries.Count; i++)
      {
        var entry = entries[i];
        var where = $"inventories.ownership_mapping[{i}]";
        var source = entry.Source?.Trim() ?? string.Empty;
        var target = entry.Target?.Trim() ?? string.Empty;

        if (source.Length == 0)
          throw new ArgumentException($"{where} has no source.");

        if (target.Length > 0 && !ValidTargets.Contains(target))
          throw new ArgumentException(
            $"{where} (source '{source}'): '{target}' is not an ownership value the API accepts. " +
            $"Valid: {string.Join(", ", ValidTargets)}, or empty to leave the field out.");

        if (!targets.TryAdd(source, target))
          throw new ArgumentException($"{where}: source '{source}' is mapped more than once.");
      }

      return new OwnershipMapping(targets);
    }

    /// <summary>
    /// The value to send for <paramref name="value"/>, or an empty string to leave the field
    /// out of the payload (the record then keeps what it holds).
    /// </summary>
    /// <remarks>
    /// A configured source wins, then a value that already is an API value passes through
    /// unchanged. Anything else is left out and counted for <see cref="Unmapped"/>: sending
    /// it would fail the row, so one unknown label would block every other field of it.
    /// </remarks>
    public string Resolve(string? value)
    {
      var trimmed = value?.Trim() ?? string.Empty;
      if (trimmed.Length == 0) return string.Empty;

      if (_targets.TryGetValue(trimmed, out var target)) return target;

      var valid = ValidTargets.FirstOrDefault(v => string.Equals(v, trimmed, StringComparison.OrdinalIgnoreCase));
      if (valid != null) return valid;

      _unmapped[trimmed] = _unmapped.GetValueOrDefault(trimmed) + 1;
      return string.Empty;
    }

    /// <summary>
    /// The values <see cref="Resolve"/> could not translate, each with the number of rows that
    /// carried it, so the run can name them once instead of once per row.
    /// </summary>
    public IReadOnlyDictionary<string, int> Unmapped => _unmapped;
  }
}
