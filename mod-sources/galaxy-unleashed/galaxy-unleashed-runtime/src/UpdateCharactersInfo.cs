using System.Linq;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    void UpdateCharactersInfo()
    {
        if (_charactersInfo != null)
        {
            return;
        }

        var v1CharactersInfo = V1.FetchCharactersInfo();

        if (v1CharactersInfo == null)
        {
            return;
        }

        int lastCharacterIdAsNumber = 0;

        _charactersInfo = v1CharactersInfo
            .Select(v1CharacterInfo =>
            {
                var v1CharacterName = V1.TranslateString(v1CharacterInfo.NameStringId);

                return new CharacterInfo
                {
                    Id = lastCharacterIdAsNumber++.ToString(),
                    Name = string.IsNullOrWhiteSpace(v1CharacterName) ? v1CharacterInfo.NameStringId : v1CharacterName,
                    PrefabResourcePath = v1CharacterInfo.PrefabResourcePath,
                    Class = v1CharacterInfo.Class switch
                    {
                        V1.CharacterClass.Jedi => CharacterClass.Jedi,
                        V1.CharacterClass.Sith => CharacterClass.Sith,
                        V1.CharacterClass.RebelResistance => CharacterClass.RebelResistance,
                        V1.CharacterClass.BountyHunter => CharacterClass.BountyHunter,
                        V1.CharacterClass.AstromechDroid => CharacterClass.AstromechDroid,
                        V1.CharacterClass.ProtocolDroid => CharacterClass.ProtocolDroid,
                        V1.CharacterClass.Scoundrel => CharacterClass.Scoundrel,
                        V1.CharacterClass.GalacticEmpire => CharacterClass.GalacticEmpire,
                        V1.CharacterClass.Scavenger => CharacterClass.Scavenger,
                        V1.CharacterClass.Civilian => CharacterClass.Civilian,
                        _ => throw new System.InvalidOperationException(),
                    }
                };
            })
            .ToArray();
    }
}