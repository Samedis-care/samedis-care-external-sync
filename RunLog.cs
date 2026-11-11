using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace SamedisExternalSync;

/// <summary>
/// Builds the run's log so that the console can never stop the sync.
/// </summary>
/// <remarks>
/// FileSyncLog writes each line to the console first and to the file second, synchronously.
/// A console that does not take output blocks that write: on Windows a click into the window
/// (QuickEdit selection) or the Pause key freezes the process at its next line. Run from the
/// task scheduler with "do not start a new instance", one frozen run then silently skips every
/// later one -- one installation stood still for days with not even "Sync started." in the log,
/// because that line was the one waiting for the console.
/// Here the file is written on the calling thread and the console only gets a copy through a
/// background queue, which drops lines rather than wait.
/// </remarks>
public static class RunLog
{
  /// <summary>
  /// The log for <paramref name="mode"/>: file lines written directly, console lines mirrored
  /// without blocking.
  /// </summary>
  public static ISyncLog Create(int level, LogMode mode, string path)
    => Create(level, mode, path, Console.Out);

  /// <summary>Same as <see cref="Create(int, LogMode, string)"/> with the console writer given, for tests.</summary>
  public static ISyncLog Create(int level, LogMode mode, string path, TextWriter console)
  {
    var toFile = mode is LogMode.File or LogMode.Both;
    var toConsole = mode is LogMode.Console or LogMode.Both;
    ISyncLog file = new FileSyncLog(level, toFile ? LogMode.File : LogMode.None, path);
    return toConsole ? new ConsoleMirrorLog(file, level, console) : file;
  }

  /// <summary>
  /// Turns off QuickEdit on the console the process runs in, so a click into the window no
  /// longer suspends it. Does nothing off Windows or without a console.
  /// </summary>
  public static void DisableQuickEdit()
  {
    if (!OperatingSystem.IsWindows()) return;
    try
    {
      const int StdInputHandle = -10;
      const uint EnableQuickEditMode = 0x0040;
      const uint EnableExtendedFlags = 0x0080;

      var handle = GetStdHandle(StdInputHandle);
      if (handle == IntPtr.Zero || handle == new IntPtr(-1)) return;
      if (!GetConsoleMode(handle, out var mode)) return;
      SetConsoleMode(handle, (mode & ~EnableQuickEditMode) | EnableExtendedFlags);
    }
    catch
    {
      // Hardening only; the background console mirror already keeps the run going.
    }
  }

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern IntPtr GetStdHandle(int nStdHandle);

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

  [DllImport("kernel32.dll", SetLastError = true)]
  private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
}

/// <summary>
/// Passes every line to an inner log and copies it to a console writer on a background
/// thread. When the console stops taking output the queue fills and further copies are
/// dropped; the inner log and the run are never held up.
/// </summary>
public sealed class ConsoleMirrorLog : ISyncLog
{
  private const int Capacity = 1000;

  // One queue and one writer thread per process: the run swaps its log once config.yml is
  // read, and two threads writing the same console would interleave.
  private static readonly BlockingCollection<string> Queue = new(Capacity);
  private static TextWriter? _writer;
  private static Thread? _thread;
  private static readonly object Gate = new();

  private readonly ISyncLog _inner;

  public int Level { get; }

  public ConsoleMirrorLog(ISyncLog inner, int level, TextWriter console)
  {
    _inner = inner;
    Level = level;
    lock (Gate)
    {
      _writer = console;
      if (_thread == null)
      {
        // Background, so a console that never drains cannot keep the process alive at exit.
        _thread = new Thread(Drain) { IsBackground = true, Name = "console-mirror" };
        _thread.Start();
        // Lets a run started by hand still show its last lines (an Abort exits right after
        // logging), but bounded, so a stuck console cannot hold the exit either.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => Flush(TimeSpan.FromSeconds(2));
      }
    }
  }

  public void Info(string message) { _inner.Info(message); Mirror(message, 1); }

  public void Warn(string message) { _inner.Warn(message); Mirror(message, 1); }

  public void Error(string message, Exception? ex = null)
  {
    _inner.Error(message, ex);
    Mirror(ex == null ? message : $"{message}: {ex.Message}", 1);
  }

  public void Debug(string message) { _inner.Debug(message); Mirror(message, 2); }

  private void Mirror(string message, int requiredLevel)
  {
    if (requiredLevel > Level) return;
    // Same shape as FileSyncLog's console output.
    var text = $"{new string('*', 80)}{Environment.NewLine}{DateTime.Now.ToString(LogFormat.TimeFormat)} {message}";
    Queue.TryAdd(text); // never waits: full means the console is stuck, and the copy is dropped
  }

  private static void Drain()
  {
    foreach (var text in Queue.GetConsumingEnumerable())
    {
      try
      {
        TextWriter? writer;
        lock (Gate) writer = _writer;
        writer?.WriteLine(text);
        writer?.Flush();
      }
      catch
      {
        // A broken console is not a reason to stop mirroring the rest.
      }
    }
  }

  /// <summary>Waits up to <paramref name="timeout"/> for queued lines to reach the console.</summary>
  public static bool Flush(TimeSpan timeout)
  {
    var until = DateTime.UtcNow + timeout;
    while (Queue.Count > 0 && DateTime.UtcNow < until)
      Thread.Sleep(10);
    return Queue.Count == 0;
  }
}
