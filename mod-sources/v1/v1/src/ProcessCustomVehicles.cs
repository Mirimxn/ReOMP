using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static readonly string _vehicleLogPath = Path.Combine(Environment.CurrentDirectory, "omp-custom-vehicles.log");

    
    static bool _vehicleDebugLog = false;

    static void VehicleDebug(string message)
    {
        if (_vehicleDebugLog) VehicleLog(message);
    }

    static bool _vehicleLogStarted = false;

    static void VehicleLog(string message)
    {
        Console.WriteLine($"[custom-vehicles] {message}");
        try
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}";
            if (_vehicleLogStarted) File.AppendAllText(_vehicleLogPath, line);
            else File.WriteAllText(_vehicleLogPath, line); // new log every game session
            _vehicleLogStarted = true;
        }
        catch { }
    }

    enum ValueKind { Unknown, String, Graph, Bool, Int, Float, Enum }

    static unsafe ValueKind GetValueKind(nint valuePtr)
    {
        if (valuePtr == nint.Zero) return ValueKind.Unknown;
        var vtable = *(nint*)valuePtr;
        if (vtable == Collectables.StringVal.Info.NativeVtablePtr) return ValueKind.String;
        if (vtable == Collectables.GraphVal.Info.NativeVtablePtr) return ValueKind.Graph;
        if (vtable == Collectables.BoolVal.Info.NativeVtablePtr) return ValueKind.Bool;
        if (vtable == Collectables.IntVal.Info.NativeVtablePtr) return ValueKind.Int;
        if (vtable == Collectables.FloatVal.Info.NativeVtablePtr) return ValueKind.Float;
        if (vtable == Collectables.EnumVal.Info.NativeVtablePtr) return ValueKind.Enum;
        return ValueKind.Unknown;
    }

    // Column types by column name. The Vehicles table mirrors the Characters table,
    // whose types OMP's character code already relies on.
    static readonly Dictionary<string, ValueKind> _kindsByColumnName = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Collect State"] = ValueKind.Enum,
        ["VehicleClass"] = ValueKind.Enum,
        ["Graph"] = ValueKind.Graph,
        ["Alternative Graph"] = ValueKind.Graph,
        ["Cheap Graph"] = ValueKind.Graph,
        ["IconFileName"] = ValueKind.String,
        ["DisplayStringID"] = ValueKind.String,
        ["DescriptionStringID"] = ValueKind.String,
        ["SilhouetteIconName"] = ValueKind.String,
        ["CheatCode"] = ValueKind.String,
        ["IsNew"] = ValueKind.Bool,
        ["IsPlayable"] = ValueKind.Bool,
        ["Add to percentage"] = ValueKind.Bool,
        ["IsRideable"] = ValueKind.Bool,
        ["HasBeenRiddenByP1"] = ValueKind.Bool,
        ["HasBeenRiddenByP2"] = ValueKind.Bool,
        ["ShowOnCollect"] = ValueKind.Bool,
        ["Price"] = ValueKind.Int,
    };

    static ValueKind[] _columnKinds = [];

    static void ResolveColumnKinds(Collectables.Table.NativeHandle table, Collectables.EntryValues.NativeHandle sampleRow)
    {
        var n = (uint)sampleRow.Values.Length;
        _columnKinds = new ValueKind[n];
        for (uint c = 0; c < n; c++)
        {
            var typeName = GetValueTypeName(sampleRow, c);
            var byType = KindFromTypeName(typeName);
            var ptr = GetValuePtr(sampleRow, c);
            var byVtable = ptr == nint.Zero ? ValueKind.Unknown : GetValueKind(ptr);
            string? colName = null;
            try { colName = table.GetColumnName((int)c); } catch { }
            var byName = colName != null && _kindsByColumnName.TryGetValue(colName, out var k) ? k : ValueKind.Unknown;
            _columnKinds[c] = byType != ValueKind.Unknown ? byType : byVtable != ValueKind.Unknown ? byVtable : byName;
            VehicleDebug($"  col {c} [{colName}]: type '{typeName}' -> byType={byType} byVtable={byVtable} byName={byName} => {_columnKinds[c]}");
        }
    }

    static string? GetValueTypeName(Collectables.EntryValues.NativeHandle entry, uint column)
    {
        try { return entry.Values[column].ValueNativeData.SelfApiClass.AsApiType().GetName(); }
        catch { return null; }
    }

    static ValueKind KindFromTypeName(string? typeName)
    {
        if (typeName == null) return ValueKind.Unknown;
        if (typeName.EndsWith("StringVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.String;
        if (typeName.EndsWith("GraphVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.Graph;
        if (typeName.EndsWith("BoolVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.Bool;
        if (typeName.EndsWith("IntVal", StringComparison.OrdinalIgnoreCase) && !typeName.EndsWith("UIntVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.Int;
        if (typeName.EndsWith("FloatVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.Float;
        if (typeName.EndsWith("EnumVal", StringComparison.OrdinalIgnoreCase)) return ValueKind.Enum;
        return ValueKind.Unknown;
    }

    static ValueKind KindOf(uint column) => column < _columnKinds.Length ? _columnKinds[column] : ValueKind.Unknown;

    static nint GetValuePtr(Collectables.EntryValues.NativeHandle entry, uint column)
    {
        // Each cell is a ClassObject { ApiClass* type; void* object }. Return the object pointer
        // (same path OMP's FetchCharactersInfo uses).
        return entry.Values[column].ValueNativeData.SelfNativeDataRawPtr;
    }

    static string DescribeValue(nint p, uint column)
    {
        if (p == nint.Zero) return "(null)";
        try
        {
            return KindOf(column) switch
            {
                ValueKind.String => $"string \"{((Collectables.StringVal.NativeHandle)p).Val}\"",
                ValueKind.Graph => $"graph {DescribeGraphHandle((Collectables.GraphVal.NativeHandle)p)} {DescribeGraphState((Collectables.GraphVal.NativeHandle)p)}",
                ValueKind.Bool => $"bool {((Collectables.BoolVal.NativeHandle)p).Val}",
                ValueKind.Int => $"int {((Collectables.IntVal.NativeHandle)p).Val}",
                ValueKind.Float => $"float {((Collectables.FloatVal.NativeHandle)p).Val}",
                ValueKind.Enum => $"enum #{((Collectables.EnumVal.NativeHandle)p).Val.Index}",
                _ => "(custom data, not copied)",
            };
        }
        catch (Exception e)
        {
            return $"(error: {e.Message})";
        }
    }

    static string ApplyReplacements(CustomVehicleInfo info, string templatePath, string? value)
    {
        if (value == null) return "";
        if (templatePath.Length > 0 && string.Equals(value, templatePath, StringComparison.OrdinalIgnoreCase)) return MatchPathStyle(info.PrefabResourcePath, templatePath);
        foreach (var (from, to) in info.Replacements)
        {
            if (string.Equals(value, from, StringComparison.OrdinalIgnoreCase)) return to;
        }
        return value;
    }

    // Keep the new prefab path in the same style as the template's path
    // (".prefab" vs ".prefab_baked"), since the table may store either.
    static string MatchPathStyle(string path, string templatePath)
    {
        var t = templatePath.ToLowerInvariant();
        var p = path;
        if (t.EndsWith(".prefab") && p.EndsWith(".prefab_baked", StringComparison.OrdinalIgnoreCase))
            p = p[..^"_baked".Length];
        else if (t.EndsWith(".prefab_baked") && p.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            p += "_baked";
        return p;
    }

    static void CopyValue(CustomVehicleInfo info, string templatePath, nint src, nint dst, uint column)
    {
        var kind = KindOf(column);
        if (kind == ValueKind.Unknown || src == nint.Zero || dst == nint.Zero) return;

        switch (kind)
        {
            case ValueKind.String:
            {
                var d = (Collectables.StringVal.NativeHandle)dst;
                d.Val = ApplyReplacements(info, templatePath, ((Collectables.StringVal.NativeHandle)src).Val);
                break;
            }
            case ValueKind.Graph:
            {
                // Full copy of the template's handle: paths, resource ids and reference types
                var d = ((Collectables.GraphVal.NativeHandle)dst).Val2;
                var s = ((Collectables.GraphVal.NativeHandle)src).Val2;
                var path1 = ApplyReplacements(info, templatePath, s.ResourcePath1);
                var path2 = ApplyReplacements(info, templatePath, s.ResourcePath2);
                d.ResourcePath1 = path1;
                d.ResourcePath2 = path2;
                if (path1 == s.ResourcePath1) d.ResourceGuid1NativeData = s.ResourceGuid1NativeData;
                if (path2 == s.ResourcePath2) d.ResourceGuid2NativeData = s.ResourceGuid2NativeData;
                d.ReferenceType1 = s.ReferenceType1;
                d.ReferenceType2 = s.ReferenceType2;
                break;
            }
            case ValueKind.Bool:
            {
                var d = (Collectables.BoolVal.NativeHandle)dst;
                d.Val = ((Collectables.BoolVal.NativeHandle)src).Val;
                break;
            }
            case ValueKind.Int:
            {
                var d = (Collectables.IntVal.NativeHandle)dst;
                d.Val = ((Collectables.IntVal.NativeHandle)src).Val;
                break;
            }
            case ValueKind.Float:
            {
                var d = (Collectables.FloatVal.NativeHandle)dst;
                d.Val = ((Collectables.FloatVal.NativeHandle)src).Val;
                break;
            }
            case ValueKind.Enum:
            {
                var d = ((Collectables.EnumVal.NativeHandle)dst).Val;
                d.Index = ((Collectables.EnumVal.NativeHandle)src).Val.Index;
                break;
            }
        }
    }

    static void ApplyOverride(nint dst, string raw, uint column)
    {
        if (dst == nint.Zero) { VehicleLog("  override skipped: value is null"); return; }
        switch (KindOf(column))
        {
            case ValueKind.String: { var d = (Collectables.StringVal.NativeHandle)dst; d.Val = raw; break; }
            case ValueKind.Graph: SetGraphPath((Collectables.GraphVal.NativeHandle)dst, raw); break;
            case ValueKind.Bool: { var d = (Collectables.BoolVal.NativeHandle)dst; d.Val = raw.Trim().ToLowerInvariant() is "true" or "1"; break; }
            case ValueKind.Int: { var d = (Collectables.IntVal.NativeHandle)dst; d.Val = int.Parse(raw, CultureInfo.InvariantCulture); break; }
            case ValueKind.Float: { var d = (Collectables.FloatVal.NativeHandle)dst; d.Val = float.Parse(raw, CultureInfo.InvariantCulture); break; }
            case ValueKind.Enum: { var d = ((Collectables.EnumVal.NativeHandle)dst).Val; d.Index = int.Parse(raw, CultureInfo.InvariantCulture); break; }
            default: VehicleLog($"  override skipped: column type not supported"); break;
        }
    }

    // Returns the graph path that matched (preferring a prefab over a cheap/preview scene), or null.
    static string? FindTemplateMatch(Collectables.EntryValues.NativeHandle entry, string match)
    {
        string? firstMatch = null;
        for (uint c = 0; c < (uint)entry.Values.Length; c++)
        {
            var p = GetValuePtr(entry, c);
            if (KindOf(c) != ValueKind.Graph || p == nint.Zero) continue;
            var path = ((Collectables.GraphVal.NativeHandle)p).Val2.ResourcePath1;
            if (path == null || !path.Contains(match, StringComparison.OrdinalIgnoreCase)) continue;
            if (path.Contains(".prefab", StringComparison.OrdinalIgnoreCase)) return path;
            firstMatch ??= path;
        }
        return firstMatch;
    }

    static readonly List<string?> _collectablesTableNames = [];
    // Called from the Collectables.Table constructor hook, BEFORE the new table is added.
    // Only the most recently constructed table is read (once), so we never touch tables
    // the game may already have freed.
    static void ProcessCustomVehiclesIfNeeded()
    {
        if (_customVehiclesInfo.Count == 0) return;

        var count = _cApi1CollectablesTables.Count;
        if (count == 0 || _collectablesTableNames.Count >= count) return;

        while (_collectablesTableNames.Count < count - 1) _collectablesTableNames.Add(null);

        var index = count - 1;
        var table = _cApi1CollectablesTables[index];

        VehicleDebug($"table #{index}: reading name...");
        string? name;
        try { name = table.GetName(); }
        catch (Exception e) { VehicleLog($"table #{index}: GetName failed: {e.Message}"); name = null; }
        _collectablesTableNames.Add(name);
        VehicleDebug($"table #{index}: '{name}'");

        if (name == null || _processedCustomVehicleTables.Contains(name)) return;
        if (!_customVehiclesInfo.Any(v => string.Equals(v.TableName, name, StringComparison.OrdinalIgnoreCase))) return;

        _processedCustomVehicleTables.Add(name);

        try
        {
            ProcessCustomVehicleTable(table, name);
        }
        catch (Exception e)
        {
            VehicleLog($"ERROR while processing table '{name}': {e}");
        }
    }

    static string ColumnLabel(Collectables.Table.NativeHandle table, uint column)
    {
        try { return $"col {column} [{table.GetColumnName((int)column)}]"; }
        catch { return $"col {column}"; }
    }

    static string EntryLabel(Collectables.Table.NativeHandle table, uint row)
    {
        try { return table.GetEntryName((int)row) ?? "?"; }
        catch { return "?"; }
    }

    static uint? FindEntryIndex(Collectables.Table.NativeHandle table, string entryName)
    {
        var rows = (uint)table.Values.Length;
        for (var r = rows; r-- > 0;)
        {
            if (string.Equals(EntryLabel(table, r), entryName, StringComparison.Ordinal)) return r;
        }
        return null;
    }

    static void ProcessCustomVehicleTable(Collectables.Table.NativeHandle table, string tableName)
    {
        VehicleDebug($"'{tableName}': reading row count...");
        var rowCount = (uint)table.Values.Length;
        VehicleLog($"Table '{tableName}' found with {rowCount} rows");
        if (rowCount == 0) return;
        ResolveColumnKinds(table, table.Values[0]);
        VehicleDebug($"Column types: {string.Join(", ", _columnKinds.Select((k, i) => $"{i}={k}"))}");

        foreach (var enumColumn in new[] { "Collect State", "VehicleClass" })
        {
            try
            {
                var n = table.GetNumColumnEnumItems(enumColumn);
                var names = new List<string>();
                for (int i = 0; i < n; i++) names.Add($"{i}={table.GetColumnEnumItemName(enumColumn, i)}");
                VehicleDebug($"Enum '{enumColumn}': {string.Join(", ", names)}");
            }
            catch (Exception e) { VehicleLog($"Enum '{enumColumn}': could not read ({e.Message})"); }
        }

        try
        {
            int displayCol = FindColumn(table, "DisplayStringID"), iconCol = FindColumn(table, "IconFileName"), cheapCol = FindColumn(table, "Cheap Graph");
            if (displayCol >= 0)
            {
                VehicleDebug($"All '{tableName}' entries (entry / name key / icon / preview):");
                for (uint r = 0; r < rowCount; r++)
                {
                    string Str(int col)
                    {
                        if (col < 0) return "";
                        var p = GetValuePtr(table.Values[r], (uint)col);
                        if (p == nint.Zero) return "";
                        return KindOf((uint)col) == ValueKind.Graph ? ((Collectables.GraphVal.NativeHandle)p).Val2.ResourcePath1 ?? "" : ((Collectables.StringVal.NativeHandle)p).Val ?? "";
                    }
                    // (No text lookup here: the string tables may not be loaded yet.)
                    VehicleDebug($"  {r}: {EntryLabel(table, r)} / {Str(displayCol)} / {Str(iconCol)} / {Str(cheapCol)}");
                }
            }
        }
        catch (Exception e) { VehicleLog($"vehicle list dump failed: {e.Message}"); }

        foreach (var info in _customVehiclesInfo.Where(v => string.Equals(v.TableName, tableName, StringComparison.OrdinalIgnoreCase)))
        {
            VehicleDebug($"'{info.Id}': searching template '{info.TemplateMatch}'...");
            uint? templateIndex = null;
            string? templatePath = null;

            // First try the entry name (exact), then a prefab/graph path containing the text.
            for (uint r = 0; r < rowCount && templateIndex == null; r++)
            {
                if (string.Equals(EntryLabel(table, r), info.TemplateMatch, StringComparison.OrdinalIgnoreCase))
                {
                    templateIndex = r;
                    templatePath = FindTemplateMatch(table.Values[r], ".prefab") ?? "";
                }
            }
            for (uint r = 0; r < rowCount && templateIndex == null; r++)
            {
                var m = FindTemplateMatch(table.Values[r], info.TemplateMatch);
                if (m != null) { templateIndex = r; templatePath = m; }
            }

            if (templateIndex == null || templatePath == null)
            {
                VehicleLog($"Template '{info.TemplateMatch}' not found for '{info.Id}'. Entries and graph paths in this table:");
                for (uint r = 0; r < rowCount; r++)
                {
                    var e = table.Values[r];
                    VehicleLog($"  row {r}: '{EntryLabel(table, r)}'");
                    for (uint c = 0; c < (uint)e.Values.Length; c++)
                    {
                        var p = GetValuePtr(e, c);
                        if (KindOf(c) == ValueKind.Graph)
                            VehicleLog($"    {ColumnLabel(table, c)}: {((Collectables.GraphVal.NativeHandle)p).Val2.ResourcePath1}");
                    }
                }
                continue;
            }

            {
                var t = table.Values[templateIndex.Value];
                VehicleDebug($"Template row {templateIndex} '{EntryLabel(table, templateIndex.Value)}' (prefab '{templatePath}'):");
                for (uint c = 0; c < (uint)t.Values.Length; c++)
                    VehicleDebug($"  {ColumnLabel(table, c)}: {DescribeValue(GetValuePtr(t, c), c)}");
            }

            if (info.DryRun)
            {
                VehicleLog($"'{info.Id}': dryRun is on, NOT adding the entry. Set \"dryRun\": false to add it.");
                continue;
            }

            // Never add the same id twice (e.g. two mods using the same id)
            if (FindEntryIndex(table, info.EntryName) is uint existingIndex)
            {
                VehicleLog($"'{info.Id}': another mod already added a vehicle with this id (row {existingIndex}), skipped. Each vehicle needs a unique id.");
                continue;
            }

            VehicleDebug($"'{info.Id}': adding entry...");
            var emptyGuid = new NuGuid.NativeData();
            unsafe
            {
                var emptyGuidNativeHandle = (NuGuid.NativeHandle)(&emptyGuid);
                table.AddEntry(info.EntryName, emptyGuidNativeHandle);
            }

            // Find the new row by its id: the last row is not always the one just added.
            if (FindEntryIndex(table, info.EntryName) is not uint newIndex)
            {
                VehicleLog($"ERROR: '{info.Id}' was not found in '{tableName}' after adding it (rows: {table.Values.Length}).");
                continue;
            }
            // Re-fetch both rows after AddEntry (the row vector may have been reallocated).
            var template = table.Values[templateIndex.Value];
            var created = table.Values[newIndex];

            VehicleDebug($"'{info.Id}': copying template values into row {newIndex}...");
            var columns = Math.Min((uint)template.Values.Length, (uint)created.Values.Length);
            for (uint c = 0; c < columns; c++)
            {
                try { CopyValue(info, templatePath, GetValuePtr(template, c), GetValuePtr(created, c), c); }
                catch (Exception e) { VehicleLog($"  col {c}: copy failed: {e.Message}"); }
            }

            foreach (var (key, raw) in info.ColumnOverrides)
            {
                int column = -1;
                if (!int.TryParse(key, out column))
                {
                    column = -1;
                    for (uint c = 0; c < columns; c++)
                    {
                        string? n = null;
                        try { n = table.GetColumnName((int)c); } catch { }
                        if (string.Equals(n, key, StringComparison.OrdinalIgnoreCase)) { column = (int)c; break; }
                    }
                }
                if (column < 0 || column >= columns) { VehicleLog($"  override '{key}': column not found"); continue; }
                try { ApplyOverride(GetValuePtr(created, (uint)column), raw, (uint)column); VehicleDebug($"  override {ColumnLabel(table, (uint)column)} = '{raw}'"); }
                catch (Exception e) { VehicleLog($"  override '{key}' failed: {e.Message}"); }
            }

            VehicleLog($"Added vehicle '{info.Id}' as '{info.EntryName}' to '{tableName}' (row {newIndex}, copied from '{EntryLabel(table, templateIndex.Value)}')");
            for (uint c = 0; c < columns; c++)
                VehicleDebug($"  {ColumnLabel(table, c)}: {DescribeValue(GetValuePtr(created, c), c)}");
        }

        try { AssignCustomVehiclePreviews(table, tableName); }
        catch (Exception e) { VehicleLog($"Vehicle previews: FAILED: {e}"); }

        try { WritePreviewBaseList(table); }
        catch (Exception e) { VehicleLog($"Vehicle previews: could not write the preview base list: {e.Message}"); }
    }
}
