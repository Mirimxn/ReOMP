using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class Spawner : IDisposable
    {
        public bool IsDisposed { get; private set; }

        static int? _lastId;

        public int Id { get; private set; }

        public SpawnerConfig Config { get; set; }

        public Vector3 Position { get; set; }

        public bool IsActive { get; set; }

        public bool ShouldBeDestroyed { get; set; }

        public bool IsInPlayerEntityRange { get; private set; }

        readonly List<Npc> _npcs = [];

        readonly List<SpawnNpcTask> _pendingSpawnNpcTasks = [];

        DateTime? _lastSpawnNpcTaskTime;

        V1.PrefabResource? _hologramPrefabResource;

        ApiEntity.NativeHandle _hologram;

        ApiTransformComponent.NativeHandle _hologramTransformComponent;

        float _hologramRotationY;

        DateTime? _hologramRotationLastUpdateTime;

        public Spawner(SpawnerConfig config, Vector3 position)
        {
            IsDisposed = false;

            Id = _lastId == null ? 0 : _lastId.Value + 1;

            _lastId = Id;

            Config = config;

            Position = position;

            IsActive = false;

            ShouldBeDestroyed = false;

            IsInPlayerEntityRange = false;

            _hologramPrefabResource = null;

            _hologram = (ApiEntity.NativeHandle)nint.Zero;

            _hologramTransformComponent = (ApiTransformComponent.NativeHandle)nint.Zero;

            _hologramRotationY = 0.0f;

            _hologramRotationLastUpdateTime = null;

            _spawners.Add(this);
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        void TryCreateSpawnNpcTask()
        {
            if (!IsActive)
            {
                return;
            }

            if (_npcs.Count + _pendingSpawnNpcTasks.Count >= Config.NpcsMaxCount)
            {
                return;
            }

            if (_lastSpawnNpcTaskTime != null && DateTime.Now - _lastSpawnNpcTaskTime < TimeSpan.FromSeconds(Config.SpawnNpcTasksIntervalAsSeconds))
            {
                return;
            }

            var currentApiWorld = V1.GetCApi1CurrentApiWorld();

            if (currentApiWorld == nint.Zero)
            {
                return;
            }

            var spawnNpcTask = new SpawnNpcTask(
                Config.NpcPreset,
                Position,
                isGlobal: false
            );

            _pendingSpawnNpcTasks.Add(spawnNpcTask);

            _lastSpawnNpcTaskTime = DateTime.Now;
        }

        void UpdateHologramRotation()
        {
            var now = DateTime.Now;

            var timeSinceLastHologramRotationUpdate = _hologramRotationLastUpdateTime == null ? TimeSpan.Zero : now - _hologramRotationLastUpdateTime.Value;

            _hologramRotationY = (_hologramRotationY + 0.1f * (float)timeSinceLastHologramRotationUpdate.TotalMilliseconds) % 360.0f;

            _hologramTransformComponent.SetRotation(0f, _hologramRotationY, 0f);

            _hologramRotationLastUpdateTime = now;

            var hologramNttRenderComponent = (NttRenderTTExportObject.NativeHandle)_hologram.FindComponentByTypeName(NttRenderTTExportObject.Info.ApiClassName);

            if (hologramNttRenderComponent != nint.Zero)
            {
                hologramNttRenderComponent.Opacity = IsActive ? 1.0f : 0.1f;
            }
        }

        void UpdateHologram()
        {
            if (_hologram != nint.Zero)
            {
                if (!_hologram.IsActive())
                {
                    Dispose();
                }

                UpdateHologramRotation();

                return;
            }

            if (_hologramPrefabResource == null)
            {
                _hologramPrefabResource = new V1.PrefabResource("Chars/Minifig/Hologram/Hologram.prefab_baked");
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

            _hologramTransformComponent = (ApiTransformComponent.NativeHandle)_hologram.FindComponentByTypeName(ApiTransformComponent.Info.ApiClassName);

            _hologramTransformComponent.SetPosition(Position.X, Position.Y, Position.Z);

            UpdateHologramRotation();
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

            var playerEntityPosition = GetPlayerEntityPosition();

            IsInPlayerEntityRange = playerEntityPosition != null && Vector3.Distance(playerEntityPosition.Value, Position) <= 0.5f;

            foreach (var pendingSpawnNpcTask in _pendingSpawnNpcTasks.ToList())
            {
                if (pendingSpawnNpcTask.Npc != null)
                {
                    _npcs.Add(pendingSpawnNpcTask.Npc);

                    _pendingSpawnNpcTasks.Remove(pendingSpawnNpcTask);

                    pendingSpawnNpcTask.Dispose();
                }
            }

            _npcs.RemoveAll((npc) => npc.IsDisposed);

            if (!IsActive)
            {
                return;
            }

            TryCreateSpawnNpcTask();
        }

        public void DestroyNpcs()
        {
            foreach (var pendingSpawnNpcTasks in _pendingSpawnNpcTasks)
            {
                pendingSpawnNpcTasks.Dispose();
            }

            _pendingSpawnNpcTasks.Clear();

            foreach (var npc in _npcs)
            {
                npc.Dispose();
            }

            _npcs.Clear();
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

            _spawners.Remove(this);

            DestroyNpcs();

            IsDisposed = true;
        }
    }
}