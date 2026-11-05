using System;
using System.Numerics;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class BattleFlag : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public Vector3 Position { get; set; }

        public bool ShouldBeDestroyed { get; set; }

        V1.PrefabResource? _hologramPrefabResource;

        ApiEntity.NativeHandle _hologram;

        ApiTransformComponent.NativeHandle _hologramTransformComponent;

        NttRenderTTExportObject.NativeHandle _hologramNttRenderTTExportObject;

        float _hologramRotationY;

        DateTime? _hologramRotationLastUpdateTime;

        public BattleFlag(Vector3 position)
        {
            IsDisposed = false;

            Position = position;

            ShouldBeDestroyed = false;

            _hologramPrefabResource = null;

            _hologram = (ApiEntity.NativeHandle)nint.Zero;

            _hologramTransformComponent = (ApiTransformComponent.NativeHandle)nint.Zero;

            _hologramNttRenderTTExportObject = (NttRenderTTExportObject.NativeHandle)nint.Zero;

            _hologramRotationY = 0.0f;

            _hologramRotationLastUpdateTime = null;
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        void UpdateHologramRotation()
        {
            if (_hologramTransformComponent == nint.Zero)
            {
                return;
            }

            var now = DateTime.Now;

            var timeSinceLastHologramRotationUpdate = _hologramRotationLastUpdateTime == null ? TimeSpan.Zero : now - _hologramRotationLastUpdateTime.Value;

            _hologramRotationY = (_hologramRotationY + 0.1f * (float)timeSinceLastHologramRotationUpdate.TotalMilliseconds) % 360.0f;

            _hologramTransformComponent.SetRotation(0f, _hologramRotationY, 0f);

            _hologramRotationLastUpdateTime = now;
        }

        void UpdateHologram()
        {
            if (_hologram == nint.Zero)
            {
                if (_hologramPrefabResource == null)
                {
                    _hologramPrefabResource = new V1.PrefabResource("chars/items/goon_whiteflag.prefab_baked");
                }

                if (!_hologramPrefabResource.FetchIsLoaded())
                {
                    return;
                }

                var currentApiWorld = V1.GetCApi1CurrentApiWorld();

                if (currentApiWorld == nint.Zero)
                {
                    return;
                }

                _hologram = _hologramPrefabResource.FetchCApi1Prefab().Clone();

                _hologram.SetNoSerialise();
                _hologram.SetParent(currentApiWorld.GetSceneGraphRoot());

                _hologramTransformComponent = (ApiTransformComponent.NativeHandle)_hologram.FindComponentByTypeName(
                    ApiTransformComponent.Info.ApiClassName
                );

                _hologramNttRenderTTExportObject = (NttRenderTTExportObject.NativeHandle)_hologram.FindComponentByTypeName(
                    NttRenderTTExportObject.Info.ApiClassName
                );

                if (_hologramNttRenderTTExportObject != nint.Zero)
                {
                    _hologramNttRenderTTExportObject.RenderAsHologram = true;
                }
            }

            if (_hologram != nint.Zero)
            {
                if (!_hologram.IsActive())
                {
                    Dispose();
                }

                UpdateHologramRotation();

                if (_hologramTransformComponent != nint.Zero)
                {
                    _hologramTransformComponent.SetPosition(Position.X, Position.Y + 0.1f, Position.Z);
                }

                if (_hologramNttRenderTTExportObject != nint.Zero)
                {
                    _hologramNttRenderTTExportObject.HologramOpacity = _instance!._battle.State.IsActive ? 1.0f : 0.1f;
                }
            }
        }

        public void Update()
        {
            ThrowIfDisposed();

            if (ShouldBeDestroyed)
            {
                Dispose();

                return;
            }

            UpdateHologram();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (_hologram != nint.Zero && _hologram.IsActive())
            {
                _hologram.DeferredDelete();
            }

            _hologramPrefabResource?.Dispose();

            IsDisposed = true;
        }
    }
}