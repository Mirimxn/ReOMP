using System;
using System.Runtime.InteropServices;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class V1
{
    public class PrefabResource : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public string Path { get; private set; }

        NttSceneGraphResourceHandle.NativeHandle _cApi1Resource;

        public PrefabResource(string path)
        {
            IsDisposed = false;

            Path = path;

            _cApi1Resource = (NttSceneGraphResourceHandle.NativeHandle)Marshal.AllocHGlobal(
                Marshal.SizeOf<NttSceneGraphResourceHandle.NativeData>()
            );

            for (int i = 0; i < Marshal.SizeOf<NttSceneGraphResourceHandle.NativeData>(); i++)
            {
                Marshal.WriteByte(_cApi1Resource, i, 0);
            }

            _cApi1Resource.Constructor(2);

            _cApi1Resource.ResourcePath1 = Path;
            _cApi1Resource.AsyncLoad();
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        public bool FetchIsLoaded()
        {
            ThrowIfDisposed();

            return _cApi1Resource.IsLoadedAndFixedUp() && _cApi1Resource.Get() != nint.Zero;
        }

        public ApiEntity.NativeHandle FetchCApi1Prefab()
        {
            ThrowIfDisposed();

            if (!FetchIsLoaded())
            {
                throw new InvalidOperationException();
            }

            return _cApi1Resource.Get().GetRoot();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            // TODO:

            IsDisposed = true;
        }
    }
}