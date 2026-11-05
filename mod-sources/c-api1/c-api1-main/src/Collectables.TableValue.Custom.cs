using System;
using System.Collections.Generic;

namespace OMP.LSWTSS.CApi1;

public static partial class Collectables
{
    public static unsafe partial class TableValue
    {
        public partial struct NativeHandle
        {
            public static explicit operator CustomDataVal__HubRegionsColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__HubRegionsColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__ObjectExtentsColumn.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__ObjectExtentsColumn.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CharacterSeatInfoColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CharacterSeatInfoColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__VehicleSeatInfoColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__VehicleSeatInfoColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__AssociatedCharacterCustomData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__AssociatedCharacterCustomData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__DialogueBoxColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__DialogueBoxColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__ParentObjectiveColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__ParentObjectiveColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CheatCodeColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CheatCodeColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CustomiserPartPoolFilter.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CustomiserPartPoolFilter.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CollectableSelectorCustomDataVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CollectableSelectorCustomDataVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CollectableReferencesCustomDataVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CollectableReferencesCustomDataVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__CollectableInfoColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__CollectableInfoColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__LinkedCharacterColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__LinkedCharacterColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__GalaxyLocationUnlocks.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__GalaxyLocationUnlocks.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__IsNewColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__IsNewColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__GoldBrickTypeColumnData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__GoldBrickTypeColumnData.NativeData*)@this.NativeDataPtr };
            public static explicit operator CustomDataVal__AbilityUpgradeCustomData.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (CustomDataVal__AbilityUpgradeCustomData.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.UInt64Val.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.UInt64Val.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.BoolVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.BoolVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.Vec3Val.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.Vec3Val.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.ColourVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.ColourVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.GraphVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.GraphVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.StringVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.StringVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.EnumVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.EnumVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.FloatVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.FloatVal.NativeData*)@this.NativeDataPtr };
            public static explicit operator Collectables.IntVal.NativeHandle(NativeHandle @this) => new() { NativeDataPtr = (Collectables.IntVal.NativeData*)@this.NativeDataPtr };
        }
    }
}