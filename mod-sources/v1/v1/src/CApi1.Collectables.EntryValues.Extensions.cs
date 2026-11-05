using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static Collectables.EnumVal.NativeHandle GetCharacterCollectStateVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.EnumVal.NativeHandle)@this.Values[0];
    }

    public static Collectables.GraphVal.NativeHandle GetCharacterGraphVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.GraphVal.NativeHandle)@this.Values[1];
    }

    public static Collectables.EnumVal.NativeHandle GetCharacterClassVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.EnumVal.NativeHandle)@this.Values[2];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterIconFileNameVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[3];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterDisplayStringIdVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[4];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterDescriptionStringIdVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[5];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsNewVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[6];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterBaseVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[7];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterCurrentlySelectedOutfitVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[8];
    }

    public static Collectables.IntVal.NativeHandle GetCharacterPriceVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.IntVal.NativeHandle)@this.Values[9];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterBaseNameIdVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[10];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsPlayableVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[11];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterAddToPercentageVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[12];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsRideableVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[15];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterHasBeenRiddenByP1Val(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[16];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterHasBeenRiddenByP2Val(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[17];
    }

    public static Collectables.StringVal.NativeHandle GetCharacterCheatCodeVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.StringVal.NativeHandle)@this.Values[19];
    }

    public static Collectables.IntVal.NativeHandle GetCharacterChronologyVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.IntVal.NativeHandle)@this.Values[20];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterShowOnCollectVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[23];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsPopCharacterVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[25];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsQuestCharacterVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[26];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsCustomOrCostumeCharacterVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[27];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsProtocolDroidSegmentVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[28];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterIsBossCharacterVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[29];
    }

    public static Collectables.GraphVal.NativeHandle GetCharacterCheapGraphVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.GraphVal.NativeHandle)@this.Values[30];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterImmuneToHazardousGasVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[31];
    }

    public static Collectables.BoolVal.NativeHandle GetCharacterShowOnPartyBarVal(this Collectables.EntryValues.NativeHandle @this)
    {
        return (Collectables.BoolVal.NativeHandle)@this.Values[32];
    }
}