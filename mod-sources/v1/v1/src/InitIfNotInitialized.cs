namespace OMP.LSWTSS;

public static partial class V1
{
    public static void InitIfNotInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        // The game's file hooks belong to asset-replacement; v1 plugs its vehicle icon and
        // preview handling into them.
        AssetReplacement.InitIfNotInitialized();
        AssetReplacement.AddRequestObserver("v1", EnsureVehicleIconSheet);
        AssetReplacement.AddHideFilter("v1", canonPath =>
        {
            if (!IsHiddenPreviewSidecar(canonPath))
            {
                return false;
            }
            VehicleDebug($"  hiding {canonPath} (belongs to the game's own preview, not the custom one)");
            return true;
        });

        _cApi1CollectablesTableConstructorMethodHook.Enable();
        _cApi1CollectablesTableAddEntry2MethodHook.Enable();
        _cApi1NuStringTableGetByNameMethodHook.Enable();
        _cApi1NttResourceHandleAsyncLoadMethodHook.Enable();
        _cApi1UIRenderTargetModelLoaderLoadGraphMethodHook.Enable();

        EnablePartyDiagnostics();

        _isInitialized = true;
    }
}