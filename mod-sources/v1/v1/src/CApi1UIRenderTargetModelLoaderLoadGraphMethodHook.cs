using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public static partial class V1
{
    // Roster previews are drawn by UIRenderTargetModelLoader::LoadGraph(path). It only works for preview
    // paths the game itself knows, so for a custom vehicle's preview path the loader is handed the template's
    // preview path instead, and that path is served from the custom vehicle's scene for this load.
    // When the template's own preview is shown again, the game's original file is restored.

    sealed record CustomPreviewRoute(string VehicleId, string TemplatePath, string SceneFile);

    static readonly Dictionary<string, CustomPreviewRoute> _customPreviewRoutes = new(StringComparer.OrdinalIgnoreCase);
    static readonly Dictionary<string, nint> _nativePathStrings = new(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> _overriddenPreviewPaths = new(StringComparer.OrdinalIgnoreCase);

    // The game's own preview scenes can come with a ".res" sidecar file in its archives that lists what the
    // scene loads. While a custom scene is served at that path, the sidecar is hidden so it isn't used.
    static bool IsHiddenPreviewSidecar(string canonPath) =>
        canonPath.EndsWith(".res") && _overriddenPreviewPaths.Contains(canonPath[..^".res".Length]);

    static void LoadPreviewGraph(nint loader, string requested, string effective, nint effectivePtr) =>
        _cApi1UIRenderTargetModelLoaderLoadGraphMethodHook!.Trampoline!(loader, effectivePtr);

    static nint NativePathString(string path)
    {
        if (!_nativePathStrings.TryGetValue(path, out var p))
        {
            p = Marshal.StringToCoTaskMemUTF8(path);
            _nativePathStrings[path] = p;
        }
        return p;
    }

    static void UnlinkCustomResource(string path)
    {
        var canon = GetResourceCanonPath(path);
        _diskResourcesInfo.RemoveAll(r => r.CanonPath == canon);
        try
        {
            var abs = Path.Combine(Environment.CurrentDirectory, path);
            if (File.Exists(abs)) File.Delete(abs);
        }
        catch (Exception e) { VehicleLog($"Vehicle previews: could not remove {path}: {e.Message}"); }
    }

    static readonly CFuncHook1<UIRenderTargetModelLoader.LoadGraphMethod.NativeDelegate> _cApi1UIRenderTargetModelLoaderLoadGraphMethodHook = new(
        UIRenderTargetModelLoader.LoadGraphMethod.Info.NativePtr,
        (nativeDataRawPtr, pathPtr) =>
        {
            try
            {
                var path = pathPtr == nint.Zero ? null : Marshal.PtrToStringUTF8(pathPtr);
                if (path != null && path.Contains("vehicles", StringComparison.OrdinalIgnoreCase)) VehicleDebug($"preview LoadGraph: {path}");

                if (path != null && _customPreviewRoutes.TryGetValue(path, out var route))
                {
                    LinkCustomResource(new CustomResourceInfo { Path = route.TemplatePath, SrcPath = route.SceneFile });
                    _overriddenPreviewPaths.Add(GetResourceCanonPath(route.TemplatePath));
                    VehicleDebug($"  -> '{route.VehicleId}' preview served at {route.TemplatePath}");
                    LoadPreviewGraph(nativeDataRawPtr, path, route.TemplatePath, NativePathString(route.TemplatePath));
                    return;
                }

                if (path != null && _overriddenPreviewPaths.Remove(GetResourceCanonPath(path)))
                {
                    UnlinkCustomResource(path);
                    VehicleDebug("  -> original preview restored");
                }

                if (path != null)
                {
                    LoadPreviewGraph(nativeDataRawPtr, path, path, pathPtr);
                    return;
                }
            }
            catch (Exception e)
            {
                VehicleLog($"preview LoadGraph hook failed: {e.Message}");
            }

            _cApi1UIRenderTargetModelLoaderLoadGraphMethodHook!.Trampoline!(nativeDataRawPtr, pathPtr);
        }
    );
}
