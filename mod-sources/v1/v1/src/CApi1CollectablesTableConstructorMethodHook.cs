using System;
using System.Collections.Generic;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    static readonly List<Collectables.Table.NativeHandle> _cApi1CollectablesTables = [];

    static bool _wasCApi1CollectablesCharactersTableProcessed = false;

    static readonly unsafe CFuncHook1<Collectables.Table.ConstructorMethod.NativeDelegate> _cApi1CollectablesTableConstructorMethodHook = new(
        Collectables.Table.ConstructorMethod.Info.NativePtr,
        (
            nativeDataRawPtr,
            arg0,
            arg1,
            arg2
        ) =>
        {
            var @this = (Collectables.Table.NativeHandle)nativeDataRawPtr;

            if (!_wasCApi1CollectablesCharactersTableProcessed)
            {
                foreach (var table in _cApi1CollectablesTables)
                {
                    var tableName = table.GetName();

                    if (tableName == "Characters")
                    {
                        var emptyGuid = new NuGuid.NativeData();

                        var emptyGuidNativeHandle = (NuGuid.NativeHandle)(&emptyGuid);

                        foreach (var customCharacterInfo in _customCharactersInfo!)
                        {
                            var tableNewEntryIndex = (uint)table.Values.Length;

                            table.AddEntry(
                                customCharacterInfo.Id,
                                emptyGuidNativeHandle
                            );

                            var tableNewEntryValues = table.Values[tableNewEntryIndex];

                            var tableNewEntryCollectStateVal = tableNewEntryValues.GetCharacterCollectStateVal();
                            var tableNewEntryCollectStateValVal = tableNewEntryCollectStateVal.Val;
                            tableNewEntryCollectStateValVal.Index = 2;

                            var tableNewEntryGraphVal = tableNewEntryValues.GetCharacterGraphVal();
                            var tableNewEntryGraphValVal2 = tableNewEntryGraphVal.Val2;
                            tableNewEntryGraphValVal2.ResourcePath1 = customCharacterInfo.PrefabResourcePath;

                            var tableNewEntryCharacterClassVal = tableNewEntryValues.GetCharacterClassVal();
                            var tableNewEntryCharacterClassValVal = tableNewEntryCharacterClassVal.Val;
                            tableNewEntryCharacterClassValVal.Index = customCharacterInfo.Class switch
                            {
                                CustomCharacterClass.Jedi => 0,
                                CustomCharacterClass.Sith => 1,
                                CustomCharacterClass.RebelResistance => 2,
                                CustomCharacterClass.BountyHunter => 3,
                                CustomCharacterClass.AstromechDroid => 4,
                                CustomCharacterClass.ProtocolDroid => 5,
                                CustomCharacterClass.Scoundrel => 6,
                                CustomCharacterClass.GalacticEmpire => 7,
                                CustomCharacterClass.Scavenger => 8,
                                CustomCharacterClass.Civilian => 9,
                                _ => throw new InvalidOperationException(),
                            };

                            var tableNewEntryIconFileNameVal = tableNewEntryValues.GetCharacterIconFileNameVal();
                            tableNewEntryIconFileNameVal.Val = "Silhouette_White.png";

                            var tableNewEntryDisplayStringIdVal = tableNewEntryValues.GetCharacterDisplayStringIdVal();
                            tableNewEntryDisplayStringIdVal.Val = customCharacterInfo.NameStringId;

                            var tableNewEntryDescriptionStringIdVal = tableNewEntryValues.GetCharacterDescriptionStringIdVal();
                            tableNewEntryDescriptionStringIdVal.Val = customCharacterInfo.DescriptionStringId;

                            var tableNewEntryIsNewVal = tableNewEntryValues.GetCharacterIsNewVal();
                            tableNewEntryIsNewVal.Val = false;

                            var tableNewEntryBaseCharacterVal = tableNewEntryValues.GetCharacterBaseVal();
                            tableNewEntryBaseCharacterVal.Val = customCharacterInfo.Id;

                            var tableNewEntryCurrentlySelectedOutfitVal = tableNewEntryValues.GetCharacterCurrentlySelectedOutfitVal();
                            tableNewEntryCurrentlySelectedOutfitVal.Val = true;

                            var tableNewEntryPriceVal = tableNewEntryValues.GetCharacterPriceVal();
                            tableNewEntryPriceVal.Val = 0;

                            var tableNewEntryCharacterBaseNameIdVal = tableNewEntryValues.GetCharacterBaseNameIdVal();
                            tableNewEntryCharacterBaseNameIdVal.Val = customCharacterInfo.NameStringId;

                            var tableNewEntryIsPlayableVal = tableNewEntryValues.GetCharacterIsPlayableVal();
                            tableNewEntryIsPlayableVal.Val = true;

                            var tableNewEntryAddToPercentageVal = tableNewEntryValues.GetCharacterAddToPercentageVal();
                            tableNewEntryAddToPercentageVal.Val = false;

                            var tableNewEntryIsRideableVal = tableNewEntryValues.GetCharacterIsRideableVal();
                            tableNewEntryIsRideableVal.Val = false;

                            var tableNewEntryHasBeenRiddenByP1Val = tableNewEntryValues.GetCharacterHasBeenRiddenByP1Val();
                            tableNewEntryHasBeenRiddenByP1Val.Val = false;

                            var tableNewEntryHasBeenRiddenByP2Val = tableNewEntryValues.GetCharacterHasBeenRiddenByP2Val();
                            tableNewEntryHasBeenRiddenByP2Val.Val = false;

                            var tableNewEntryCheatCodeVal = tableNewEntryValues.GetCharacterCheatCodeVal();
                            tableNewEntryCheatCodeVal.Val = "";

                            var tableNewEntryChronologyVal = tableNewEntryValues.GetCharacterChronologyVal();
                            tableNewEntryChronologyVal.Val = 0;

                            var tableNewEntryShowOnCollectVal = tableNewEntryValues.GetCharacterShowOnCollectVal();
                            tableNewEntryShowOnCollectVal.Val = false;

                            var tableNewEntryIsPopCharacterVal = tableNewEntryValues.GetCharacterIsPopCharacterVal();
                            tableNewEntryIsPopCharacterVal.Val = false;

                            var tableNewEntryIsQuestCharacterVal = tableNewEntryValues.GetCharacterIsQuestCharacterVal();
                            tableNewEntryIsQuestCharacterVal.Val = false;

                            var tableNewEntryIsCustomOrCostumeCharacterVal = tableNewEntryValues.GetCharacterIsCustomOrCostumeCharacterVal();
                            tableNewEntryIsCustomOrCostumeCharacterVal.Val = false;

                            var tableNewEntryIsProtocolDroidSegmentVal = tableNewEntryValues.GetCharacterIsProtocolDroidSegmentVal();
                            tableNewEntryIsProtocolDroidSegmentVal.Val = false;

                            var tableNewEntryIsBossCharacterVal = tableNewEntryValues.GetCharacterIsBossCharacterVal();
                            tableNewEntryIsBossCharacterVal.Val = false;

                            var tableNewEntryCheapGraphVal = tableNewEntryValues.GetCharacterCheapGraphVal();
                            var tableNewEntryCheapGraphValVal2 = tableNewEntryCheapGraphVal.Val2;
                            tableNewEntryCheapGraphValVal2.ResourcePath1 = customCharacterInfo.PreviewPrefabResourcePath;

                            var tableNewEntryImmuneToHazardousGasVal = tableNewEntryValues.GetCharacterImmuneToHazardousGasVal();
                            tableNewEntryImmuneToHazardousGasVal.Val = false;

                            var tableNewEntryShowOnPartyBarVal = tableNewEntryValues.GetCharacterShowOnPartyBarVal();
                            tableNewEntryShowOnPartyBarVal.Val = false;
                        }

                        _wasCApi1CollectablesCharactersTableProcessed = true;
                    }
                }
            }

            // Only tables that finished constructing are in the list at this point.
            try { ProcessCustomVehiclesIfNeeded(); }
            catch (System.Exception e) { VehicleLog($"EXCEPTION in table constructor hook: {e}"); }

            _cApi1CollectablesTables.Add(@this);

            return _cApi1CollectablesTableConstructorMethodHook!.Trampoline!(nativeDataRawPtr, arg0, arg1, arg2);
        }
    );
}