using System;
using System.Numerics;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class SpawnNpcTask : IDisposable
    {
        public bool IsDisposed { get; private set; }

        readonly NpcPresetConfig _npcConfig;

        readonly Vector3 _npcPosition;

        readonly bool _isGlobal;

        V1.PrefabResource? _npcPrefabResource;

        public Npc? Npc { get; private set; }

        public SpawnNpcTask(NpcPresetConfig npcConfig, Vector3 npcPosition, bool isGlobal)
        {
            IsDisposed = false;

            _npcConfig = npcConfig;

            _npcPosition = npcPosition;

            _isGlobal = isGlobal;

            _npcPrefabResource = null;

            Npc = null;

            _spawnNpcTasks.Add(this);
        }

        void ThrowIfDisposed()
        {
            if (IsDisposed)
            {
                throw new InvalidOperationException();
            }
        }

        public bool FetchIsFinished()
        {
            ThrowIfDisposed();

            return Npc != null;
        }

        public void Update()
        {
            ThrowIfDisposed();

            if (FetchIsFinished())
            {
                return;
            }

            _npcPrefabResource ??= new V1.PrefabResource(_npcConfig.PrefabResourcePath);

            if (!_npcPrefabResource.FetchIsLoaded())
            {
                return;
            }

            var currentApiWorld = V1.GetCApi1CurrentApiWorld();

            if (currentApiWorld == nint.Zero)
            {
                return;
            }

            Npc = new Npc(currentApiWorld, _npcPrefabResource.FetchCApi1Prefab());

            Npc.SetPosition(_npcPosition);
            Npc.IsBattleParticipant = _npcConfig.IsBattleParticipant;
            Console.WriteLine($"NPC {_npcConfig.PrefabResourcePath} faction ID: {Npc.FetchFactionName()}");
            if (_npcConfig.OverrideFactionId != null)
            {
                Npc.SetFactionId(_npcConfig.OverrideFactionId.Value);
            }

            if (_isGlobal)
            {
                Dispose();
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _spawnNpcTasks.Remove(this);

            _npcPrefabResource?.Dispose();

            IsDisposed = true;
        }
    }
}