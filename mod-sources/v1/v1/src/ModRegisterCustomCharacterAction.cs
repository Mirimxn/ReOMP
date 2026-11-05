using System;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static class ModRegisterCustomCharacterAction
    {
        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            var modRegisterCustomCharacterActionPayload = JsonConvert.DeserializeObject<ModRegisterCustomCharacterActionPayload>(
                modActionPayloadAsJson
            ) ?? throw new InvalidOperationException();

            _customCharactersInfo.Add(
                new CustomCharacterInfo
                {
                    Id = modRegisterCustomCharacterActionPayload.Id,
                    NameStringId = modRegisterCustomCharacterActionPayload.NameStringId,
                    DescriptionStringId = modRegisterCustomCharacterActionPayload.DescriptionStringId,
                    Class = modRegisterCustomCharacterActionPayload.Class,
                    PrefabResourcePath = modRegisterCustomCharacterActionPayload.PrefabResourcePath,
                    PreviewPrefabResourcePath = modRegisterCustomCharacterActionPayload.PreviewPrefabResourcePath
                }
            );
        }
    }
}