using System;
using System.Collections.Generic;

namespace NTE_Fishing_Bot;

public static class BotLogger
{
    private const int MaxEntries = 1000;
    private static readonly object _lock = new object();
    private static readonly List<string> _entries = new List<string>(MaxEntries + 10);

    // null entry = clear signal to UI
    public static event EventHandler<string> EntryAdded;
    public static event EventHandler<string> LastEntryUpdated;

    public static void Log(string message)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss.fff}]  {message}";
        lock (_lock)
        {
            if (_entries.Count >= MaxEntries)
                _entries.RemoveRange(0, 100);
            _entries.Add(entry);
        }
        EntryAdded?.Invoke(null, entry);
    }

    public static void UpdateLast(string message)
    {
        string entry = $"[{DateTime.Now:HH:mm:ss.fff}]  {message}";
        lock (_lock)
        {
            if (_entries.Count > 0)
                _entries[_entries.Count - 1] = entry;
            else
                _entries.Add(entry);
        }
        LastEntryUpdated?.Invoke(null, entry);
    }

    public static IReadOnlyList<string> GetAll()
    {
        lock (_lock)
            return new List<string>(_entries).AsReadOnly();
    }

    public static void Clear()
    {
        lock (_lock)
            _entries.Clear();
        EntryAdded?.Invoke(null, null);
    }
}
