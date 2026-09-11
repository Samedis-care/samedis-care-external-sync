using Newtonsoft.Json;
using SamedisCare.Api.Common;
using SamedisCare.Api.Http;
using SamedisCare.Api.Lookup;
using SamedisCare.Helper.Logging;

namespace SamedisExternalSync
{
  /// <summary>
  /// Resolving one node of the facility's location tree -- a property's building, a
  /// building's floor, a floor's room -- and creating it when the source names one that
  /// does not exist yet.
  /// <para>
  /// The three used to have a resolver each, 312 lines that differed in the parent field,
  /// a handful of payload keys and the word in the log message. A diff of buildings against
  /// floors with the name and the parent field normalised away left nothing but those. What
  /// they share is the order of questions, and getting that order wrong is expensive, so it
  /// is written down once.
  /// </para>
  /// <para>
  /// Deliberately not in SamedisCare.Api: the enterprise API answers buildings, floors and
  /// properties read-only, and rooms are out by decision. This is external-sync's own tree
  /// and there is no second caller to serve.
  /// </para>
  /// </summary>
  internal static class Hierarchy
  {
    /// <summary>
    /// Builds the attributes to send.
    /// </summary>
    /// <param name="clearing">
    /// True when the payload goes into a PUT on a record that already exists. Fields the
    /// source may have emptied are then sent anyway, so the update clears them instead of
    /// leaving yesterday's value standing -- a building whose street was removed upstream
    /// must lose it here too. On a create there is nothing to clear and they stay out, so a
    /// new record is not written full of empty strings.
    /// </param>
    internal delegate Dictionary<string, object?> Attributes(bool clearing);

    /// <summary>What the source says about one node, and how to write it.</summary>
    /// <param name="Label">What the log calls it: <c>building</c>, <c>floor</c>, <c>room</c>.</param>
    /// <param name="Title">The node's name. Blank means the row cannot name a node.</param>
    /// <param name="ExternalId">The source system's own key, the strongest anchor here.</param>
    /// <param name="Scope">
    /// The parent, as gridfilter conditions: <c>property_id</c> for a building,
    /// <c>building_id</c> for a floor, whichever of the three a room carries. Titles repeat
    /// across parents -- every building has a "1. OG" -- so a title lookup without the parent
    /// finds a stranger.
    /// </param>
    /// <param name="Attributes">The payload builder.</param>
    internal sealed record Node(
      string Label,
      string? Title,
      string? ExternalId,
      (string Field, string? Value)[] Scope,
      Attributes Attributes)
    {
      /// <summary>
      /// The source's own Samedis id, where it carries one. Only rooms do -- the inventory
      /// row names its room by id, while buildings and floors are only ever named.
      /// </summary>
      public string? Id { get; init; }

      /// <summary>
      /// Whether a parent is required before the node can be looked up by title or created
      /// at all. True for buildings and floors, whose title says nothing on its own. A room
      /// may sit directly under a property, so it is false there.
      /// </summary>
      public bool RequiresScope { get; init; } = true;

      /// <summary>Whatever else the log should name, e.g. the inventory row that asked.</summary>
      public string Context { get; init; } = "";

      internal string NormalizedTitle => Title?.Trim() ?? string.Empty;
      internal string NormalizedExternalId => ExternalId?.Trim() ?? string.Empty;

      internal string Where
      {
        get
        {
          var parts = Scope.Where(c => !string.IsNullOrWhiteSpace(c.Value))
                           .Select(c => $"{c.Field}='{c.Value}'").ToList();
          if (!string.IsNullOrWhiteSpace(NormalizedExternalId))
            parts.Add($"external_id='{NormalizedExternalId}'");
          if (!string.IsNullOrWhiteSpace(Context)) parts.Add(Context);
          return string.Join(", ", parts);
        }
      }
    }

    /// <summary>
    /// Resolves the node, creating it when asked to. Returns null when it neither exists nor
    /// could be created.
    /// </summary>
    /// <remarks>
    /// The order is external_id, then id, then title within the parent -- strongest key
    /// first, and a hit is final. Falling through after a match would resolve a DIFFERENT
    /// record: the source may rename a room whose external_id still points at the same one,
    /// and the title lookup would then find whatever else carries the new name.
    /// </remarks>
    internal static string? Resolve(IApiClient client, string resource, Node node,
                                    ResourceLookup lookup, ISyncLog log,
                                    bool create, bool updateExisting)
    {
      var title = node.NormalizedTitle;
      var externalId = node.NormalizedExternalId;

      // Sending a blank title in an update would clear the name of a record that has one, so
      // a row that cannot name the node may still resolve it but never writes to it.
      void SyncExisting(string id, string matchedBy)
      {
        if (!updateExisting || string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(title))
          return;

        var response = client.Put(resource, id,
          JsonConvert.SerializeObject(new { data = node.Attributes(clearing: true) }));

        if (JsonApi.IsSuccess(client.StatusCode))
          log.Debug($"{node.Label} synced via PUT (match_by='{matchedBy}', id='{id}', title='{title}', {node.Where}).");
        else
          log.Warn($"Failed to sync {node.Label} via PUT (match_by='{matchedBy}', id='{id}', "
                 + $"title='{title}', {node.Where}, status={client.StatusCode}). Response: {response}");
      }

      // external_id is the stable cross-system anchor, so it is asked first. The lookup
      // remembers hits AND misses, so a value repeated across rows costs one request.
      var byExternalId = lookup.ByUniqueField("external_id", externalId);
      if (!string.IsNullOrWhiteSpace(byExternalId))
      {
        // Seed the title lookup too, so a later row naming the same node by title and parent
        // is answered from memory.
        lookup.RememberFields(TitleConditions(node, title), byExternalId);
        SyncExisting(byExternalId, "external_id");
        return byExternalId;
      }

      // Debug, not a warning: on a first import nothing carries this external_id yet, so a
      // miss is the normal case and the row goes on to be matched by title or created. A
      // lookup that could not be answered is a different thing and throws.
      if (!string.IsNullOrWhiteSpace(externalId))
        log.Debug($"{node.Label} not found by external_id, continuing with title ({node.Where}).");

      if (!string.IsNullOrWhiteSpace(node.Id))
      {
        var byId = lookup.ById(node.Id);
        if (!string.IsNullOrWhiteSpace(byId))
        {
          SyncExisting(byId, "id");
          return byId;
        }
      }

      var hasScope = node.Scope.Any(c => !string.IsNullOrWhiteSpace(c.Value));
      if (node.RequiresScope && (!hasScope || string.IsNullOrWhiteSpace(title)))
        return null;

      var byTitle = lookup.ByFields(TitleConditions(node, title));
      if (!string.IsNullOrWhiteSpace(byTitle))
      {
        SyncExisting(byTitle, "title");
        return byTitle;
      }

      if (!create || string.IsNullOrWhiteSpace(title))
        return null;

      var created = client.Post(resource,
        JsonConvert.SerializeObject(new { data = node.Attributes(clearing: false) }));

      if (!JsonApi.IsSuccess(client.StatusCode))
      {
        log.Error($"Failed to create {node.Label} (title='{title}', {node.Where}, "
                + $"status={client.StatusCode}). Response: {created}");
        return null;
      }

      var newId = JsonApi.ExtractDataId(created);
      if (string.IsNullOrWhiteSpace(newId))
      {
        log.Error($"Failed to create {node.Label} (title='{title}', {node.Where}): "
                + "API returned no id.");
        return null;
      }

      // Seed every key this row was identified by, so a later row naming the same node is
      // answered from memory instead of asking for what this run just wrote.
      if (!string.IsNullOrWhiteSpace(node.Id))
      {
        lookup.RememberId(newId);
        lookup.RememberId(node.Id, newId);
      }
      lookup.RememberFields(TitleConditions(node, title), newId);
      lookup.RememberUniqueField("external_id", externalId, newId);

      log.Debug($"{node.Label} created on the fly: '{title}' ({node.Where}) -> {newId}");
      return newId;
    }

    /// <summary>
    /// Title plus parent, with blank parts dropped. Declared once so the lookup and the cache
    /// seeding cannot drift apart -- a seed built from a different set answers a narrower
    /// question with a broader record, or never hits at all.
    /// </summary>
    private static (string Field, string? Value)[] TitleConditions(Node node, string title)
    {
      var conditions = new List<(string Field, string? Value)> { ("title", title) };
      conditions.AddRange(node.Scope);
      return conditions.Where(c => !string.IsNullOrWhiteSpace(c.Value)).ToArray();
    }
  }
}
