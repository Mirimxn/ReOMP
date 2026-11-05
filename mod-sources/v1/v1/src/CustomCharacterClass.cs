using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OMP.LSWTSS;

public static partial class V1
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CustomCharacterClass
    {
        [EnumMember(Value = "jedi")]
        Jedi,
        [EnumMember(Value = "sith")]
        Sith,
        [EnumMember(Value = "rebel-resistance")]
        RebelResistance,
        [EnumMember(Value = "bounty-hunter")]
        BountyHunter,
        [EnumMember(Value = "astromech-droid")]
        AstromechDroid,
        [EnumMember(Value = "protocol-droid")]
        ProtocolDroid,
        [EnumMember(Value = "scoundrel")]
        Scoundrel,
        [EnumMember(Value = "galactic-empire")]
        GalacticEmpire,
        [EnumMember(Value = "scavenger")]
        Scavenger,
        [EnumMember(Value = "civilian")]
        Civilian,
    }
}