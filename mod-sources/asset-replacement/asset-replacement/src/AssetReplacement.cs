using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace OMP.LSWTSS;

/// <summary>
/// Owns the game's file lookups for mod-supplied game files. See mod.md.
/// </summary>
public static partial class AssetReplacement
{
    static readonly object _lock = new();
    // Canonical game path -> owning mod ID.
    static readonly Dictionary<string, string> _owners = new(StringComparer.Ordinal);
    static readonly List<(string Owner, Action<string> Observer)> _observers = [];
    static readonly List<(string Owner, Func<string, bool> IsHidden)> _hideFilters = [];
    static bool _isInitialized;

    /// <summary>Lower case, forward slashes, no leading slash.</summary>
    public static string CanonPath(string path) => path.Replace('\\', '/').TrimStart('/').ToLowerInvariant();

    public static void InitIfNotInitialized()
    {
        lock (_lock)
        {
            if (_isInitialized)
            {
                return;
            }
            _hostDeviceConstructorHook.Enable();
            _datDeviceConstructorHook.Enable();
            _createFileHook.Enable();
            _getPositionHook.Enable();
            _isFileExistsHook.Enable();
            _isInitialized = true;
        }
        Log("file hooks enabled");
    }

    public static bool Register(string owner, string path, out string? existingOwner)
    {
        var canon = CanonPath(path);
        lock (_lock)
        {
            if (_owners.TryGetValue(canon, out var current) && current != owner)
            {
                existingOwner = current;
                return false;
            }
            _owners[canon] = owner;
        }
        existingOwner = null;
        return true;
    }

    public static bool Unregister(string owner, string path)
    {
        var canon = CanonPath(path);
        lock (_lock)
        {
            return _owners.TryGetValue(canon, out var current) && current == owner && _owners.Remove(canon);
        }
    }

    public static bool IsRegistered(string path)
    {
        var canon = CanonPath(path);
        lock (_lock)
        {
            return _owners.ContainsKey(canon);
        }
    }

    static bool HasRegistrations
    {
        get
        {
            lock (_lock)
            {
                return _owners.Count > 0;
            }
        }
    }

    /// <summary>
    /// Registers a file a mod declares. The Mod Manager places it at the game path when it
    /// installs the mod. Without the manager, the packaged file is copied there when nothing
    /// is at that path yet. An existing file is never deleted or overwritten.
    /// </summary>
    public static bool RegisterModFile(string modId, string modDirPath, string path, string srcPath)
    {
        if (!IsSafeRelative(path) || !IsSafeRelative(srcPath))
        {
            Log($"{modId}: skipped unsafe path \"{path}\" (srcPath \"{srcPath}\")");
            return false;
        }
        try
        {
            var gameFile = Path.Combine(Environment.CurrentDirectory, path.Replace('\\', '/'));
            if (!File.Exists(gameFile))
            {
                var source = Path.Combine(modDirPath, srcPath.Replace('\\', '/'));
                if (!File.Exists(source))
                {
                    Log($"{modId}: {srcPath} isn't in the mod folder, so {path} wasn't registered");
                    return false;
                }
                Directory.CreateDirectory(Path.GetDirectoryName(gameFile) ?? Environment.CurrentDirectory);
                File.Copy(source, gameFile);
                Log($"{modId}: copied {srcPath} to {path}");
            }
        }
        catch (Exception e)
        {
            Log($"{modId}: couldn't prepare {path}: {e.Message}");
            return false;
        }
        if (!Register(modId, path, out var other))
        {
            Log($"{modId}: {path} is already registered by {other}, so {modId}'s file isn't used");
            return false;
        }
        Log($"{modId}: registered {path}");
        return true;
    }

    static bool IsSafeRelative(string path)
    {
        var normal = path.Replace('\\', '/');
        if (normal.Length == 0 || normal.StartsWith('/') || normal.Contains(':'))
        {
            return false;
        }
        foreach (var segment in normal.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
            {
                return false;
            }
        }
        return true;
    }

    sealed class Subscription(Action remove) : IDisposable
    {
        Action? _remove = remove;

        public void Dispose() => Interlocked.Exchange(ref _remove, null)?.Invoke();
    }

    public static IDisposable AddRequestObserver(string owner, Action<string> observer)
    {
        var entry = (owner, observer);
        lock (_lock)
        {
            _observers.Add(entry);
        }
        return new Subscription(() =>
        {
            lock (_lock)
            {
                _observers.Remove(entry);
            }
        });
    }

    public static IDisposable AddHideFilter(string owner, Func<string, bool> isHidden)
    {
        var entry = (owner, isHidden);
        lock (_lock)
        {
            _hideFilters.Add(entry);
        }
        return new Subscription(() =>
        {
            lock (_lock)
            {
                _hideFilters.Remove(entry);
            }
        });
    }

    static void NotifyRequest(string path)
    {
        (string Owner, Action<string> Observer)[] observers;
        lock (_lock)
        {
            if (_observers.Count == 0)
            {
                return;
            }
            observers = [.. _observers];
        }
        foreach (var (owner, observer) in observers)
        {
            try
            {
                observer(path);
            }
            catch (Exception e)
            {
                Log($"request observer from {owner} failed: {e.Message}");
            }
        }
    }

    static bool IsHidden(string canonPath)
    {
        (string Owner, Func<string, bool> IsHidden)[] filters;
        lock (_lock)
        {
            if (_hideFilters.Count == 0)
            {
                return false;
            }
            filters = [.. _hideFilters];
        }
        foreach (var (owner, isHidden) in filters)
        {
            try
            {
                if (isHidden(canonPath))
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                Log($"hide filter from {owner} failed: {e.Message}");
            }
        }
        return false;
    }

    static void Log(string message) => Console.WriteLine($"[asset-replacement] {message}");
}
