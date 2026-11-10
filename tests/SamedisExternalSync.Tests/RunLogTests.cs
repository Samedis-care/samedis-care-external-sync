using System.Diagnostics;
using System.Text;
using FluentAssertions;
using SamedisCare.Helper.Logging;
using Xunit;

namespace SamedisExternalSync.Tests;

/// <summary>
/// A console that stops taking output (a click into the window on Windows) froze a customer
/// sync at its first log line, and the scheduler then skipped every later run for days.
/// These pin that the file log and the run go on whatever the console does.
/// </summary>
public class RunLogTests : IDisposable
{
  private readonly string _dir = Path.Combine(Path.GetTempPath(), "run-log-" + Guid.NewGuid().ToString("N"));

  public RunLogTests() => Directory.CreateDirectory(_dir);

  public void Dispose() => Directory.Delete(_dir, true);

  private string LogPath => Path.Combine(_dir, "log", "Logfile_2026-11-10.log");

  [Fact]
  public void A_stuck_console_does_not_hold_up_the_run_or_the_file()
  {
    using var console = new StuckWriter();
    try
    {
      var log = RunLog.Create(1, LogMode.Both, LogPath, console);

      var watch = Stopwatch.StartNew();
      for (var i = 0; i < 5000; i++)
        log.Info($"line {i}");
      watch.Stop();

      watch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(10));
      File.ReadAllLines(LogPath).Should().HaveCount(5000);
      console.Entered.Should().BeTrue("the mirror did reach the console and is stuck in it");
    }
    finally
    {
      // Free the shared mirror thread for the other tests.
      console.Release();
      ConsoleMirrorLog.Flush(TimeSpan.FromSeconds(5));
    }
  }

  [Fact]
  public void Both_writes_the_file_and_mirrors_to_the_console()
  {
    var console = new StringWriter();
    var log = RunLog.Create(1, LogMode.Both, LogPath, console);

    log.Info("Sync started.");
    log.Debug("not at level 1");
    ConsoleMirrorLog.Flush(TimeSpan.FromSeconds(5)).Should().BeTrue();

    File.ReadAllText(LogPath).Should().Contain("INFO Sync started.").And.NotContain("not at level 1");
    console.ToString().Should().Contain("Sync started.").And.NotContain("not at level 1");
  }

  [Fact]
  public void File_mode_does_not_touch_the_console()
  {
    var log = RunLog.Create(1, LogMode.File, LogPath, new StringWriter());

    log.Should().NotBeOfType<ConsoleMirrorLog>();
    log.Info("x");
    File.Exists(LogPath).Should().BeTrue();
  }

  [Fact]
  public void Console_mode_writes_no_file()
  {
    var log = RunLog.Create(1, LogMode.Console, LogPath, new StringWriter());

    log.Info("x");
    ConsoleMirrorLog.Flush(TimeSpan.FromSeconds(5));
    File.Exists(LogPath).Should().BeFalse();
  }

  /// <summary>A console writer whose first write blocks until released.</summary>
  private sealed class StuckWriter : TextWriter
  {
    private readonly ManualResetEventSlim _released = new(false);
    private volatile bool _entered;

    public bool Entered => _entered;

    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
      _entered = true;
      _released.Wait();
    }

    public void Release() => _released.Set();

    protected override void Dispose(bool disposing)
    {
      _released.Set();
      base.Dispose(disposing);
    }
  }
}
