using System;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class ProcessingScope : IDisposable
    {
        bool _isDisposed;

        NttUniverseProcessingScope.NativeHandle _nttUniverseProcessingScope;

        ApiWorldProcessingScope.NativeHandle _apiWorldProcessingScope;

        public ProcessingScope()
        {
            _isDisposed = false;

            var currentApiWorld = V1.GetCApi1CurrentApiWorld();

            if (currentApiWorld == nint.Zero)
            {
                _nttUniverseProcessingScope = (NttUniverseProcessingScope.NativeHandle)nint.Zero;
                _apiWorldProcessingScope = (ApiWorldProcessingScope.NativeHandle)nint.Zero;
            }
            else
            {
                _nttUniverseProcessingScope = (NttUniverseProcessingScope.NativeHandle)Marshal.AllocHGlobal(
                    Marshal.SizeOf<NttUniverseProcessingScope.NativeData>()
                );

                _nttUniverseProcessingScope.Constructor(currentApiWorld.GetUniverse(), true);

                _apiWorldProcessingScope = (ApiWorldProcessingScope.NativeHandle)Marshal.AllocHGlobal(
                    Marshal.SizeOf<ApiWorldProcessingScope.NativeData>()
                );

                _apiWorldProcessingScope.Constructor(currentApiWorld, true);
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            if (_nttUniverseProcessingScope != (NttUniverseProcessingScope.NativeHandle)nint.Zero)
            {
                _nttUniverseProcessingScope.Destructor();

                Marshal.FreeHGlobal(_nttUniverseProcessingScope);
            }

            if (_apiWorldProcessingScope != (ApiWorldProcessingScope.NativeHandle)nint.Zero)
            {
                _apiWorldProcessingScope.Destructor();

                Marshal.FreeHGlobal(_apiWorldProcessingScope);
            }

            GC.SuppressFinalize(this);

            _isDisposed = true;
        }
    }
}