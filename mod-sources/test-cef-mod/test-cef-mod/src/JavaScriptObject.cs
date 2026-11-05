using System;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace OMP.LSWTSS;

public partial class TestCefMod
{
    class JavaScriptObject()
    {
        #pragma warning disable CS0649
        readonly static JsonSerializerSettings _jsonSerializerSettings = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public string FetchCharactersInfo(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_charactersInfo, _jsonSerializerSettings);
        }

        public string FetchQuickMenuState(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_quickMenuState, _jsonSerializerSettings);
        }

        public string FetchSpawnerMenuState(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_spawnerMenuState, _jsonSerializerSettings);
        }

        struct SetSpawnerMenuStateArgs
        {
            public SpawnerMenuState? SpawnerMenuState;
        }

        public void SetSpawnerMenuState(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<SetSpawnerMenuStateArgs>(argsAsJson, _jsonSerializerSettings);

            _spawnerMenuState = args.SpawnerMenuState;
        }

        public string FetchPlayerEntityLastPosition(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_playerEntityLastPosition, _jsonSerializerSettings);
        }

        struct CreateSpawnNpcGlobalTaskArgs
        {
            public NpcPresetConfig NpcPresetConfig;

            public Vector3 NpcPosition;
        }

        public void CreateSpawnNpcGlobalTask(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<CreateSpawnNpcGlobalTaskArgs>(argsAsJson, _jsonSerializerSettings);

            _ = new SpawnNpcTask(args.NpcPresetConfig, args.NpcPosition, isGlobal: true);
        }

        struct CreateSpawnerArgs
        {
            public SpawnerConfig SpawnerConfig;

            public Vector3 SpawnerPosition;
        }

        public string CreateSpawner(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<CreateSpawnerArgs>(argsAsJson, _jsonSerializerSettings);

            var result = new Spawner(args.SpawnerConfig, args.SpawnerPosition).Id;

            return JsonConvert.SerializeObject(result, _jsonSerializerSettings);
        }

        struct FetchIsSpawnerActiveArgs
        {
            public int SpawnerId;
        }

        public string FetchIsSpawnerActive(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<FetchIsSpawnerActiveArgs>(argsAsJson, _jsonSerializerSettings);

            var spawner = _spawners.FirstOrDefault(spawner => spawner.Id == args.SpawnerId);

            if (spawner == null)
            {
                throw new InvalidOperationException();
            }

            var result = spawner.IsActive;

            return JsonConvert.SerializeObject(result, _jsonSerializerSettings);
        }

        struct SetIsSpawnerActiveArgs
        {
            public int SpawnerId;

            public bool IsSpawnerActive;
        }

        public void SetIsSpawnerActive(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<SetIsSpawnerActiveArgs>(argsAsJson, _jsonSerializerSettings);

            var spawner = _spawners.FirstOrDefault(spawner => spawner.Id == args.SpawnerId);

            if (spawner == null)
            {
                throw new InvalidOperationException();
            }

            spawner.IsActive = args.IsSpawnerActive;
        }

        struct SetShouldSpawnerBeDestroyedArgs
        {
            public int SpawnerId;

            public bool ShouldSpawnerBeDestroyed;
        }

        public void SetShouldSpawnerBeDestroyed(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<SetShouldSpawnerBeDestroyedArgs>(argsAsJson, _jsonSerializerSettings);

            var spawner = _spawners.FirstOrDefault(spawner => spawner.Id == args.SpawnerId);

            if (spawner == null)
            {
                throw new InvalidOperationException();
            }

            spawner.ShouldBeDestroyed = args.ShouldSpawnerBeDestroyed;
        }

        struct FetchSpawnerConfigArgs
        {
            public int SpawnerId;
        }

        public string FetchSpawnerConfig(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<FetchSpawnerConfigArgs>(argsAsJson, _jsonSerializerSettings);

            var spawner = _spawners.FirstOrDefault(spawner => spawner.Id == args.SpawnerId);

            if (spawner == null)
            {
                throw new InvalidOperationException();
            }

            var result = spawner.Config;

            return JsonConvert.SerializeObject(result, _jsonSerializerSettings);
        }

        struct SetSpawnerConfigArgs
        {
            public int SpawnerId;

            public SpawnerConfig SpawnerConfig;
        }

        public void SetSpawnerConfig(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<SetSpawnerConfigArgs>(argsAsJson, _jsonSerializerSettings);

            var spawner = _spawners.FirstOrDefault(spawner => spawner.Id == args.SpawnerId);

            if (spawner == null)
            {
                throw new InvalidOperationException();
            }

            spawner.Config = args.SpawnerConfig;
        }

        public string FetchSpawnerInPlayerEntityRangeId(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_spawnerInPlayerEntityRange?.Id, _jsonSerializerSettings);
        }

        public void EnableAllSpawners(string argsAsJson)
        {
            foreach (var spawner in _spawners)
            {
                spawner.IsActive = true;
            }
        }

        public void DisableAllSpawners(string argsAsJson)
        {
            foreach (var spawner in _spawners)
            {
                spawner.IsActive = false;
            }
        }

        public string FetchBattleConfig(string argsAsJson)
        {
            return JsonConvert.SerializeObject(_battleConfig, _jsonSerializerSettings);
        }

        struct SetBattleConfigArgs
        {
            public BattleConfig BattleConfig;
        }

        public void SetBattleConfig(string argsAsJson)
        {
            var args = JsonConvert.DeserializeObject<SetBattleConfigArgs>(argsAsJson, _jsonSerializerSettings);

            _battleConfig = args.BattleConfig;
        }
        #pragma warning restore CS0649
    }
}