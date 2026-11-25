using System;
using System.IO;
using Newtonsoft.Json;

namespace OMP.LSWTSS;

public static partial class V1
{
    public static class ModRegisterCustomResourceAction
    {
        public static void Execute(string modId, string modDirPath, string modActionPayloadAsJson)
        {
            var modRegisterCustomResourceActionPayload = JsonConvert.DeserializeObject<ModRegisterCustomResourceActionPayload>(
                modActionPayloadAsJson
            ) ?? throw new InvalidOperationException();

            var customResourceInfo = new CustomResourceInfo
            {
                Path = modRegisterCustomResourceActionPayload.Path,
                SrcPath = Path.Combine(
                    modDirPath,
                    modRegisterCustomResourceActionPayload.SrcPath
                ),
            };

            _customResourcesInfo.Add(customResourceInfo);

            // asset-replacement routes the lookup to the file the Mod Manager placed at the
            // game path; it no longer deletes and hard-links files at startup.
            AssetReplacement.RegisterModFile(
                modId,
                modDirPath,
                modRegisterCustomResourceActionPayload.Path,
                modRegisterCustomResourceActionPayload.SrcPath
            );
        }
    }
}