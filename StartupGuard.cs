namespace SamedisExternalSync;

/// <summary>
/// Makes the two ways a run could die without writing a single log line visible in the log.
/// </summary>
/// <remarks>
/// The log is the only thing the customer and samedis-care-log-monitor look at. A run that
/// throws before "Sync started." leaves no line and not even a file, so a broken config.yml
/// read as "the sync is not running" for days (UKT, 2026-11-04 to 2026-11-10) while the real
/// reason sat in the Windows event log.
/// </remarks>
public static class StartupGuard
{
  /// <summary>
  /// Reads config.yml. On any failure returns null and an error that names the file and,
  /// for a YAML problem, the line and column the parser stopped at.
  /// </summary>
  public static AppConfig? TryLoadConfig(string path, out string? error)
  {
    error = null;
    if (!File.Exists(path))
    {
      error = $"The file {path} does not exists. Stopping Import.";
      return null;
    }

    try
    {
      return ConfigStore.Load<AppConfig>(path, ignoreUnmatchedProperties: false);
    }
    catch (Exception ex)
    {
      // The parser message does not always carry the position (a tab in the indentation
      // says only "found invalid tab as indentation"), so it is added from the exception.
      // An unknown key names itself in the message. The inner exception carries the cause
      // of a value that did not convert. End, not Start: for a scanner error Start is where
      // the enclosing block begins (line 1 for a tab anywhere in it), End is where it broke.
      var position = ex is YamlDotNet.Core.YamlException yaml
        ? $" near line {yaml.End.Line}, column {yaml.End.Column}"
        : "";
      var inner = ex.InnerException == null ? "" : $" ({ex.InnerException.Message})";
      error = $"{path} could not be read{position}, nothing was synced: {ex.Message}{inner}{TabHint(path)}";
      return null;
    }
  }

  // A tab in the indentation is the usual result of editing config.yml by hand, and the
  // parser reports it at the start of the enclosing block, often line 1. Naming the lines
  // is what makes the message actionable.
  private static string TabHint(string path)
  {
    try
    {
      var lines = File.ReadAllLines(path);
      var withTabs = lines
        .Select((text, i) => (text, number: i + 1))
        .Where(l => l.text.TakeWhile(char.IsWhiteSpace).Contains('\t'))
        .Select(l => l.number)
        .ToList();
      return withTabs.Count == 0 ? "" : $" Tab in the indentation on line(s) {string.Join(", ", withTabs)}; use spaces.";
    }
    catch (IOException)
    {
      return "";
    }
  }

  /// <summary>
  /// Writes an exception nobody caught to the log before the process goes down.
  /// </summary>
  /// <param name="currentLog">
  /// Asked at the moment of the crash, because the run swaps the bootstrap logger for the
  /// configured one once config.yml is read.
  /// </param>
  public static void LogUnhandledExceptions(Func<ISyncLog> currentLog)
  {
    AppDomain.CurrentDomain.UnhandledException += (_, e) => WriteCrash(currentLog(), e.ExceptionObject);
  }

  /// <summary>One ERROR line with type and message, then the stack trace on a single line.</summary>
  public static void WriteCrash(ISyncLog log, object exceptionObject)
  {
    try
    {
      if (exceptionObject is Exception ex)
      {
        log.Error($"Sync crashed, nothing after this point was synced: {ex.GetType().FullName}: {ex.Message}");
        // Kept on one line: log-monitor reads the file line by line, and a multi-line trace
        // would show up as unrelated fragments.
        var trace = (ex.ToString() ?? "").Replace("\r", "").Replace("\n", " | ");
        log.Error($"Crash details: {trace}");
      }
      else
      {
        log.Error($"Sync crashed, nothing after this point was synced: {exceptionObject}");
      }
    }
    catch
    {
      // The process is going down anyway; a failing logger must not replace the original
      // exception in the event log.
    }
  }
}
