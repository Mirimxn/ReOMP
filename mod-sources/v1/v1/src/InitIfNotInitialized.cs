namespace OMP.LSWTSS;

public static partial class V1
{
    public static void InitIfNotInitialized()
    {
        if (_isInitialized)
        {
            return;
        }

        _cApi1NttFileFileDevicePCConstructorMethodHook.Enable();
        _cApi1NuFileDeviceDatConstructorMethodHook.Enable();

        _cApi1NuFileDeviceDatCreateFileMethodHook.Enable();
        _cApi1NuFileDeviceDatFileGetPositionMethodHook.Enable();

        _cApi1CollectablesTableConstructorMethodHook.Enable();
        _cApi1CollectablesTableAddEntry2MethodHook.Enable();
        _cApi1NuStringTableGetByNameMethodHook.Enable();
        _cApi1NttResourceHandleIsFileExistsMethodHook.Enable();
        _cApi1NttResourceHandleAsyncLoadMethodHook.Enable();
        _cApi1UIRenderTargetModelLoaderLoadGraphMethodHook.Enable();

        _isInitialized = true;
    }
}