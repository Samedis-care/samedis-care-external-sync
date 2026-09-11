using FluentAssertions;
using SamedisCare.Api.Lookup;
using SamedisCare.Api.Query;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// Nach einem Schreibvorgang merkt sich der Lauf die soeben angelegte Id, damit eine zweite
/// Quellzeile zum selben Datensatz ihn findet, statt ihn noch einmal anzulegen.
/// <para>
/// Das funktioniert nur, wenn die Saat unter <b>genau dem Schlüssel</b> sitzt, den die
/// zugehörige Suche bildet. Tut sie das nicht, ist sie nicht wirkungslos, sondern schädlich:
/// <see cref="ResourceLookup"/> merkt sich auch <b>Fehltreffer</b>, und der Fehltreffer der
/// ersten Zeile bleibt dann stehen. Die zweite Zeile legt erneut an und läuft in den
/// Unique-Index.
/// </para>
/// <para>
/// Beide Tests prüfen über <c>Requests</c>, dass die Suche nach der Saat <b>gar nicht mehr
/// fragt</b> — das ist der Beweis, dass der Schlüssel getroffen hat, und nicht bloß, dass
/// irgendwoher die richtige Id kam.
/// </para>
/// </summary>
public class CacheSeedTests
{
    private const string Existing = "507f1f77bcf86cd799439011";

    /// <summary>
    /// Die Gerätenummer wird mit <c>variant=regular</c> nachgeschlagen, und der Query-Teil
    /// steckt im Cache-Schlüssel.
    /// </summary>
    /// <remarks>
    /// Ohne ihn saß die Saat unter <c>fields:Equals::device_number=N</c>, gefragt wurde nach
    /// <c>fields:Equals:variant=regular:device_number=N</c>.
    /// </remarks>
    [Fact]
    public void A_created_inventory_is_found_again_by_its_device_number()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "inventories");

        lookup.RememberField("device_number", "INV-0815", Existing,
                             FilterBuilder.FilterType.Equals, "variant=regular");

        Inventories.ResolveExistingInventoryId(lookup, "", "", "INV-0815", fallbackByDeviceNumber: true)
                   .Should().Be(Existing);
        api.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Aufgaben legt dieser Sync unter <c>external_id</c> an — die Quell-Nummer —, und danach
    /// sucht er sie auch wieder.
    /// </summary>
    /// <remarks>
    /// Vorher wurde <c>issue_number</c> gesät. Das ist die laufende Nummer des <b>Servers</b>,
    /// vergeben beim Anlegen und mit der Quell-Nummer nicht verwandt; hätte der Schlüssel
    /// getroffen, hätte er behauptet, beide seien dasselbe. Er traf ohnehin nie, weil die
    /// Suche danach über <c>ByConditions</c> läuft und damit unter einem anderen Schlüssel
    /// nachschlägt.
    /// </remarks>
    [Fact]
    public void A_created_task_is_found_again_by_the_source_number()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "issues");

        lookup.RememberUniqueField("external_id", "4711", Existing);

        lookup.ByUniqueField("external_id", "4711").Should().Be(Existing);
        api.Requests.Should().BeEmpty();
    }

    /// <summary>
    /// Ohne Saat fragt derselbe Aufruf den Server — sonst bewiese der Test oben nur, dass der
    /// Server nichts liefert.
    /// </summary>
    [Fact]
    public void Without_the_seed_the_lookup_really_does_ask()
    {
        var api = FakeApi.NotFound();
        var lookup = new ResourceLookup(api, "inventories");

        Inventories.ResolveExistingInventoryId(lookup, "", "", "INV-0815", fallbackByDeviceNumber: true)
                   .Should().BeNullOrEmpty();
        api.Requests.Should().NotBeEmpty();
    }
}
