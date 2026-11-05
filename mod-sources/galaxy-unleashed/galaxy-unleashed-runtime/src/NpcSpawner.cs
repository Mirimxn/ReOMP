using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using OMP.LSWTSS.CApi1;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class NpcSpawner : IDisposable
    {
        public bool IsDisposed { get; private set; }

        static int? _lastIdAsInt;

        public NpcSpawnerState State { get; private set; }

        public Vector3 Position { get; set; }

        public bool ShouldBeDestroyed { get; set; }

        public float? DistanceToPlayerEntity { get; private set; }

        public bool IsInPlayerEntityRange { get; private set; }

        readonly List<Npc> _npcs = [];

        readonly List<SpawnNpcTask> spawnNpcPendingTasks = [];

        DateTime? _lastSpawnNpcTaskTime;

        V1.PrefabResource? _hologramPrefabResource;

        ApiEntity.NativeHandle _hologram;

        ApiTransformComponent.NativeHandle _hologramTransformComponent;

        NttRenderTTExportObject.NativeHandle _hologramNttRenderTTExportObject;

        float _hologramRotationY;

        DateTime? _hologramRotationLastUpdateTime;

        public NpcSpawner(NpcSpawnerConfig config, Vector3 position)
        {
            IsDisposed = false;

            var idAsNumber = _lastIdAsInt == null ? 1 : _lastIdAsInt.Value + 1;

            _lastIdAsInt = idAsNumber;

            State = new NpcSpawnerState
            {
                Id = idAsNumber.ToString(),
                Config = config,
                IsEnabled = false
            };

            Position = position;

            ShouldBeDestroyed = false;

            IsInPlayerEntityRange = false;

            _hologramPrefabResource = null;

            _hologram = (ApiEntity.NativeHandle)nint.Zero;

            _hologramTransformComponent = (ApiTransformComponent.NativeHandle)nint.Zero;

            _hologramRotationY = 0.0f;

            _hologramRotationLastUpdateTime = null;

            _instance!._npcSpawners.Add(this);
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
            if (!State.IsEnabled)
            {
                return;
            }

            if (_npcs.Count + spawnNpcPendingTasks.Count >= State.Config.MaxNpcsCount)
            {
                return;
            }

            if (_lastSpawnNpcTaskTime != null && DateTime.Now - _lastSpawnNpcTaskTime < TimeSpan.FromSeconds(State.Config.NpcSpawningIntervalSeconds))
            {
                return;
            }

            var currentApiWorld = V1.GetCApi1CurrentApiWorld();

            if (currentApiWorld == nint.Zero)
            {
                return;
            }

            var spawnNpcTask = new SpawnNpcTask(
                State.Config.NpcPreset,
                State.Config.AreNpcsBattleParticipants,
                Position,
                isGlobal: false
            );

            spawnNpcPendingTasks.Add(spawnNpcTask);

            _lastSpawnNpcTaskTime = DateTime.Now;
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

                _hologramTransformComponent = (ApiTransformComponent.NativeHandle)_hologram.FindComponentByTypeName(
                    ApiTransformComponent.Info.ApiClassName
                );

                _hologramNttRenderTTExportObject = (NttRenderTTExportObject.NativeHandle)_hologram.FindComponentByTypeName(
                    NttRenderTTExportObject.Info.ApiClassName
                );
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
                    _hologramTransformComponent.SetPosition(Position.X, Position.Y, Position.Z);
                }

                if (_hologramNttRenderTTExportObject != nint.Zero)
                {
                    _hologramNttRenderTTExportObject.HologramOpacity = State.IsEnabled ? 1.0f : 0.1f;
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

            var playerEntityPosition = _instance!.FetchPlayerEntityPosition();

            DistanceToPlayerEntity = playerEntityPosition == null ? null : Vector3.Distance(playerEntityPosition.Value, Position);

            IsInPlayerEntityRange = DistanceToPlayerEntity != null && DistanceToPlayerEntity.Value <= 0.5f;

            foreach (var spawnNpcPendingTask in spawnNpcPendingTasks.ToList())
            {
                if (spawnNpcPendingTask.Npc != null)
                {
                    _npcs.Add(spawnNpcPendingTask.Npc);

                    spawnNpcPendingTasks.Remove(spawnNpcPendingTask);

                    spawnNpcPendingTask.Dispose();
                }
            }

            _npcs.RemoveAll((npc) => npc.IsDisposed);

            if (State.IsEnabled)
            {
                TryCreateSpawnNpcTask();
            }
        }

        public void DestroyNpcs()
        {
            foreach (var spawnNpcPendingTask in spawnNpcPendingTasks)
            {
                spawnNpcPendingTask.Dispose();
            }

            spawnNpcPendingTasks.Clear();

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

            _instance!._npcSpawners.Remove(this);

            DestroyNpcs();

            IsDisposed = true;
        }
    }
}