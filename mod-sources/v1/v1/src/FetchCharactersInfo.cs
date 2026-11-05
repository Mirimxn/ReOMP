using System;
using System.Linq;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static CharacterInfo[]? FetchCharactersInfo()
    {
        if (_wasCApi1CollectablesCharactersTableProcessed == false)
        {
            return null;
        }

        var charactersTable = _cApi1CollectablesTables.FirstOrDefault(table => table.GetName() == "Characters");

        if (charactersTable == nint.Zero)
        {
            return null;
        }

        return charactersTable
            .Values
            .Select(tableEntryValues => new CharacterInfo
            {
                NameStringId = ((Collectables.StringVal.NativeHandle)tableEntryValues.Values[4].ValueNativeData.SelfNativeDataRawPtr).Val ?? throw new InvalidOperationException(),
                DescriptionStringId = ((Collectables.StringVal.NativeHandle)tableEntryValues.Values[5].ValueNativeData.SelfNativeDataRawPtr).Val ?? throw new InvalidOperationException(),
                Class = ((Collectables.EnumVal.NativeHandle)tableEntryValues.Values[2].ValueNativeData.SelfNativeDataRawPtr).Val.Index switch
                {
                    0 => CharacterClass.Jedi,
                    1 => CharacterClass.Sith,
                    2 => CharacterClass.RebelResistance,
                    3 => CharacterClass.BountyHunter,
                    4 => CharacterClass.AstromechDroid,
                    5 => CharacterClass.ProtocolDroid,
                    6 => CharacterClass.Scoundrel,
                    7 => CharacterClass.GalacticEmpire,
                    8 => CharacterClass.Scavenger,
                    9 => CharacterClass.Civilian,
                    _ => throw new InvalidOperationException()
                },
                PrefabResourcePath = ((Collectables.GraphVal.NativeHandle)tableEntryValues.Values[1].ValueNativeData.SelfNativeDataRawPtr).Val2.ResourcePath1 ?? throw new InvalidOperationException()
            })
            .ToArray();
    }
}