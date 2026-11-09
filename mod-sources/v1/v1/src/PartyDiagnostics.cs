using System;
using System.Collections.Generic;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

// Temporary diagnostics: custom characters drop out of the party when it is changed.
// Logs the party-select and party script calls, every change the game makes to a custom
// character's row (and to any Characters row while a party change is being confirmed), and
// snapshots of the party-related columns before and after each confirm.
// Writes "[party]" lines to omp-custom-vehicles.log. Each hook is optional: one that cannot be
// created is reported in the log and skipped.
public static partial class V1
{
    static readonly string[] _partyColumns =
        ["Collect State", "IsPlayable", "Base Character", "CurrentlySelectedOutfit", "ShowOnPartyBar", "IsQuestCharacter", "IsCustomOrCostumeCharacter", "CharacterClass", "Character Class"];

    static int _partyConfirmDepth = 0;
    static int _partyChangesLogged = 0;

    static void PartyLog(string message) => VehicleLog($"[party] {message}");

    static Collectables.Table.NativeHandle? FindCharactersTable()
    {
        foreach (var t in _cApi1CollectablesTables)
        {
            try { if (t.GetName() == "Characters") return t; } catch { }
        }
        return null;
    }

    static bool IsCustomEntry(string name) => name.StartsWith("OMP_", StringComparison.Ordinal) ||
        (_customCharactersInfo?.Any(c => c.Id == name) ?? false);

    static string DescribeCell(nint p)
    {
        if (p == nint.Zero) return "(null)";
        try
        {
            return GetValueKind(p) switch
            {
                ValueKind.String => $"\"{((Collectables.StringVal.NativeHandle)p).Val}\"",
                ValueKind.Bool => ((Collectables.BoolVal.NativeHandle)p).Val ? "true" : "false",
                ValueKind.Int => ((Collectables.IntVal.NativeHandle)p).Val.ToString(),
                ValueKind.Float => ((Collectables.FloatVal.NativeHandle)p).Val.ToString(),
                ValueKind.Enum => $"enum#{((Collectables.EnumVal.NativeHandle)p).Val.Index}",
                ValueKind.Graph => $"graph {((Collectables.GraphVal.NativeHandle)p).Val2.ResourcePath1}",
                _ => "(other)",
            };
        }
        catch (Exception e) { return $"(error {e.Message})"; }
    }

    static string ColumnNameOf(Collectables.Table.NativeHandle table, int c)
    {
        try { return table.GetColumnName(c) ?? $"col{c}"; } catch { return $"col{c}"; }
    }

    static void SnapshotPartyRows(string when)
    {
        try
        {
            if (FindCharactersTable() is not Collectables.Table.NativeHandle table) { PartyLog($"{when}: Characters table not found"); return; }
            var rows = (uint)table.Values.Length;
            if (rows == 0) return;
            int numCols = _columnKinds.Length;   // column count, read when the custom rows were added
            var cols = new List<int>();
            for (int c = 0; c < numCols; c++) if (_partyColumns.Contains(ColumnNameOf(table, c), StringComparer.OrdinalIgnoreCase)) cols.Add(c);
            PartyLog($"{when}: {rows} rows");
            for (uint r = 0; r < rows; r++)
            {
                var n = EntryLabel(table, r);
                // custom rows and the template rows they were copied from
                if (!IsCustomEntry(n) && !_customVehiclesInfo.Any(v => string.Equals(v.TemplateMatch, n, StringComparison.Ordinal))) continue;
                var vals = string.Join(", ", cols.Select(c => $"{ColumnNameOf(table, c)}={DescribeCell(GetValuePtr(table.Values[r], (uint)c))}"));
                PartyLog($"   row {r} {n}: {vals}");
            }
        }
        catch (Exception e) { PartyLog($"{when}: snapshot failed: {e.Message}"); }
    }

    static CFuncHook1<PartySelectComponent.SelectCharacterMethod.NativeDelegate>? _partySelectCharacterHook;
    static CFuncHook1<PartySelectComponent.ConfirmPartyMethod.NativeDelegate>? _partyConfirmHook;
    static CFuncHook1<PartySelectComponent.CancelPartyMethod.NativeDelegate>? _partyCancelHook;
    static CFuncHook1<ScriptHelper.AddToPartyMethod.NativeDelegate>? _partyAddHook;
    static CFuncHook1<ScriptHelper.RemoveFromPartyMethod.NativeDelegate>? _partyRemoveHook;
    static CFuncHook1<Collectables.Table.OnValueChangedMethod.NativeDelegate>? _tableValueChangedHook;
    static CFuncHook1<Collectables.Table.OnStructureChangedMethod.NativeDelegate>? _tableStructureChangedHook;

    static void PartySelectCharacter(nint self, int index)
    {
        PartyLog($"PartySelect.SelectCharacter({index})");
        _partySelectCharacterHook!.Trampoline!(self, index);
    }

    static void PartyConfirm(nint self)
    {
        PartyLog("PartySelect.ConfirmParty: start");
        SnapshotPartyRows("before confirm");
        _partyConfirmDepth++; _partyChangesLogged = 0;
        try { _partyConfirmHook!.Trampoline!(self); }
        finally { _partyConfirmDepth--; }
        SnapshotPartyRows("after confirm");
        PartyLog("PartySelect.ConfirmParty: end");
    }

    static void PartyCancel(nint self)
    {
        PartyLog("PartySelect.CancelParty");
        _partyCancelHook!.Trampoline!(self);
    }

    static void PartyAdd(nint self, nint arg)
    {
        PartyLog($"ScriptHelper.AddToParty(0x{arg:X})");
        _partyAddHook!.Trampoline!(self, arg);
    }

    static void PartyRemove(nint self, nint arg)
    {
        PartyLog($"ScriptHelper.RemoveFromParty(0x{arg:X})");
        _partyRemoveHook!.Trampoline!(self, arg);
    }

    static void TableValueChanged(nint self, int row, int column)
    {
        try
        {
            var table = (Collectables.Table.NativeHandle)self;
            if (row >= 0 && row < table.Values.Length && table.GetName() == "Characters")
            {
                var name = EntryLabel(table, (uint)row);
                if (IsCustomEntry(name) || (_partyConfirmDepth > 0 && _partyChangesLogged++ < 200))
                {
                    _tableValueChangedHook!.Trampoline!(self, row, column);
                    var now = column >= 0 ? DescribeCell(GetValuePtr(table.Values[(uint)row], (uint)column)) : "?";
                    PartyLog($"value changed: row {row} {name} {ColumnNameOf(table, column)} = {now}{(_partyConfirmDepth > 0 ? " [during confirm]" : "")}");
                    return;
                }
            }
        }
        catch (Exception e) { PartyLog($"value-changed hook: {e.Message}"); }
        _tableValueChangedHook!.Trampoline!(self, row, column);
    }

    static void TableStructureChanged(nint self)
    {
        try
        {
            var table = (Collectables.Table.NativeHandle)self;
            if (table.GetName() == "Characters") PartyLog($"Characters table structure changed (rows now {table.Values.Length})");
        }
        catch { }
        _tableStructureChangedHook!.Trampoline!(self);
    }

    static void EnablePartyDiagnostics()
    {
        static void Try(string what, Action enable)
        {
            try { enable(); PartyLog($"hooked {what}"); }
            catch (Exception e) { PartyLog($"could not hook {what}: {e.Message}"); }
        }
        Try("PartySelect.SelectCharacter", () => { _partySelectCharacterHook = new(PartySelectComponent.SelectCharacterMethod.Info.NativePtr, PartySelectCharacter); _partySelectCharacterHook.Enable(); });
        Try("PartySelect.ConfirmParty", () => { _partyConfirmHook = new(PartySelectComponent.ConfirmPartyMethod.Info.NativePtr, PartyConfirm); _partyConfirmHook.Enable(); });
        Try("PartySelect.CancelParty", () => { _partyCancelHook = new(PartySelectComponent.CancelPartyMethod.Info.NativePtr, PartyCancel); _partyCancelHook.Enable(); });
        Try("ScriptHelper.AddToParty", () => { _partyAddHook = new(ScriptHelper.AddToPartyMethod.Info.NativePtr, PartyAdd); _partyAddHook.Enable(); });
        Try("ScriptHelper.RemoveFromParty", () => { _partyRemoveHook = new(ScriptHelper.RemoveFromPartyMethod.Info.NativePtr, PartyRemove); _partyRemoveHook.Enable(); });
        Try("Table.OnValueChanged", () => { _tableValueChangedHook = new(Collectables.Table.OnValueChangedMethod.Info.NativePtr, TableValueChanged); _tableValueChangedHook.Enable(); });
        Try("Table.OnStructureChanged", () => { _tableStructureChangedHook = new(Collectables.Table.OnStructureChangedMethod.Info.NativePtr, TableStructureChanged); _tableStructureChangedHook.Enable(); });
    }
}
