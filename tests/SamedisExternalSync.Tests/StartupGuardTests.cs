using FluentAssertions;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// A run that dies before its first log line looks like a run that never started: a customer
/// sync stood still for six days on that, with only the Windows event log
/// knowing why. These pin that a broken config.yml and an uncaught exception are logged.
/// </summary>
public class StartupGuardTests : IDisposable
{
  private readonly string _dir = Path.Combine(Path.GetTempPath(), "startup-guard-" + Guid.NewGuid().ToString("N"));

  public StartupGuardTests() => Directory.CreateDirectory(_dir);

  public void Dispose() => Directory.Delete(_dir, true);

  private string WriteConfig(string yaml)
  {
    var path = Path.Combine(_dir, "config.yml");
    File.WriteAllText(path, yaml);
    return path;
  }

  [Fact]
  public void Missing_file_is_reported_not_thrown()
  {
    var config = StartupGuard.TryLoadConfig(Path.Combine(_dir, "nope.yml"), out var error);

    config.Should().BeNull();
    error.Should().Contain("does not exists");
  }

  [Fact]
  public void Broken_yaml_is_reported_with_its_position()
  {
    // A tab in the indentation: what a hand edit in Notepad typically produces.
    var path = WriteConfig("inventories:\n\townership_mapping:\n  - source: \"Privat\"\n    target: \"owned\"\n");

    var config = StartupGuard.TryLoadConfig(path, out var error);

    config.Should().BeNull();
    error.Should().Contain("could not be read").And.Contain("Tab in the indentation on line(s) 2;");
  }

  [Fact]
  public void Unknown_key_is_reported_by_name()
  {
    var path = WriteConfig("inventories:\n  ownership_mapings:\n    - source: \"Privat\"\n      target: \"owned\"\n");

    var config = StartupGuard.TryLoadConfig(path, out var error);

    config.Should().BeNull();
    error.Should().Contain("ownership_mapings");
  }

  [Fact]
  public void Valid_ownership_mapping_still_loads()
  {
    var path = WriteConfig("inventories:\n  ownership_mapping:\n    - source: \"Privat\"\n      target: \"owned\"\n");

    var config = StartupGuard.TryLoadConfig(path, out var error);

    error.Should().BeNull();
    config!.Inventories.OwnershipMapping.Should().ContainSingle(e => e.Source == "Privat" && e.Target == "owned");
  }

  [Fact]
  public void Crash_is_written_as_error_lines_on_one_line_each()
  {
    var log = new RecordingLog();
    Exception thrown;
    try { throw new InvalidOperationException("boom"); }
    catch (Exception ex) { thrown = ex; }

    StartupGuard.WriteCrash(log, thrown);

    log.Errors.Should().HaveCount(2);
    log.Errors[0].Should().Contain("Sync crashed").And.Contain("InvalidOperationException").And.Contain("boom");
    log.Errors.Should().OnlyContain(l => !l.Contains('\n'));
  }

  private sealed class RecordingLog : ISyncLog
  {
    public List<string> Errors { get; } = new();
    public int Level => 2;
    public void Info(string message) { }
    public void Warn(string message) { }
    public void Debug(string message) { }
    public void Error(string message, Exception? ex = null) => Errors.Add(message);
  }
}
