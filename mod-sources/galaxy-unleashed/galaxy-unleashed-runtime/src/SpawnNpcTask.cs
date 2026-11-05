using System;
using System.Linq;
using System.Numerics;

namespace OMP.LSWTSS;

public partial class GalaxyUnleashed
{
    class SpawnNpcTask : IDisposable
    {
        public bool IsDisposed { get; private set; }

        readonly NpcPresetConfig _npcPresetConfig;

        readonly bool _isNpcBattleParticipant;

        readonly Vector3 _npcPosition;

        readonly bool _isGlobal;

        V1.PrefabResource? _npcCharacterPrefabResource;

        public Npc? Npc { get; private set; }

        public SpawnNpcTask(NpcPresetConfig npcPresetConfig, bool isNpcBattleParticipant, Vector3 npcPosition, bool isGlobal)
        {
            IsDisposed = false;

            _npcPresetConfig = npcPresetConfig;

            _isNpcBattleParticipant = isNpcBattleParticipant;

            _npcPosition = npcPosition;

            _isGlobal = isGlobal;

            _npcCharacterPrefabResource = null;

            Npc = null;

            _instance!._spawnNpcTasks.Add(this);
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

            if (_instance!._charactersInfo == null)
            {
                return;
            }

            var npcCharacterInfo = _instance!._charactersInfo.FirstOrDefault(characterInfo => characterInfo.Id == _npcPresetConfig.CharacterId);

            if (npcCharacterInfo == null)
            {
                return;
            }

            _npcCharacterPrefabResource ??= new V1.PrefabResource(npcCharacterInfo.PrefabResourcePath);

            if (!_npcCharacterPrefabResource.FetchIsLoaded())
            {
                return;
            }

            var currentApiWorld = V1.GetCApi1CurrentApiWorld();

            if (currentApiWorld == nint.Zero)
            {
                return;
            }

            Npc = new Npc(currentApiWorld, _npcCharacterPrefabResource.FetchCApi1Prefab());

            Npc.SetPosition(_npcPosition);
            Npc.IsBattleParticipant = _isNpcBattleParticipant;

            if (_npcPresetConfig.CharacterOverrideFactionId != null)
            {
                Npc.SetFactionId(_npcPresetConfig.CharacterOverrideFactionId.Value);
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

            _instance!._spawnNpcTasks.Remove(this);

            _npcCharacterPrefabResource?.Dispose();

            IsDisposed = true;
        }
    }
}